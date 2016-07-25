using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OEC.FIX.Sample.FIX;
using QuickFix.Fields;

namespace OEC.FIX.Sample.FoxScript
{
    partial class ExecEngine
    {
        private bool IsHandleSeqNumbers => !(_properties.Contains(Prop.ResetSeqNumbers) && (bool)_properties[Prop.ResetSeqNumbers].Value);

        public void Exit()
        {
            WriteLine("Exiting...");
            Disconnect();
            DisconnectFast();
            _running = false;
        }

        public void Print(IEnumerable<object> args)
        {
            if (args == null)
                return;

            foreach (object arg in args)
                WriteLine("{0}", GetSyntaxConstructionValue(arg));
        }

        public void Printf(FormatArgs fargs)
        {
            WriteLine(ApplyFormatArgs(fargs));
        }

        public void Ensure(object logicalExpr, FormatArgs fargs)
        {
            Func<bool> predicate = BuildPredicate(logicalExpr);
            if (!predicate())
            {
                throw new ExecutionException(ApplyFormatArgs(fargs));
            }
        }

        public void SetPropValue(string name, Object value)
        {
            _properties[name].Value = GetObjectValue(value, null);
            GetPropsValue(name);
        }

        public void GetPropsValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                foreach (Prop prop in _properties)
                    WriteLine(Tools.FormatProp(prop));
            }
            else
                WriteLine(Tools.FormatProp(_properties[name]));
        }

        public void Ping()
        {
            _fixEngine.Ping();
        }

        public void Auth(string senderCompID)
        {
            StoreSeqNumbers();
            _properties[Prop.SenderCompID].Value = senderCompID;
            LoadSeqNumbers();
        }

        public void Connect(string senderCompID, string password, string uuid)
        {
            if (!string.IsNullOrEmpty(senderCompID))
                Auth(senderCompID);

            WriteLine("Connecting to {0}:{1} as '{2}' ...",
                _properties[Prop.Host].Value,
                _properties[Prop.Port].Value,
                _properties[Prop.SenderCompID].Value);

            _fixEngine.Connect(password, uuid);
        }

        public void ConnectFast(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                userName = _properties[Prop.SenderCompID].Value.ToString();

            string password = _properties[Prop.FastHashCode].Value.ToString();

            if (string.IsNullOrWhiteSpace(password))
                throw new ExecutionException("FastHashCode not set.");

            if (!IsFastAvailable)
                throw new ExecutionException("FASTClient is disabled");

            _fastClient.Connect(userName, password);
        }

        public void FASTHeartbeat()
        {
            if (IsFastAvailable)
                _fastClient.SendHearbeat();
        }


        public void Disconnect()
        {
            _fixEngine.Disconnect();
            StoreSeqNumbers();
        }

        public void DisconnectFast()
        {
            if (IsFastAvailable)
                _fastClient.Disconnect();
        }

        public void StoreSeqNumbers()
        {
            if (!IsHandleSeqNumbers)
                return;

            string senderCompID = _properties[Prop.SenderCompID].Value.ToString();
            string targetCompID = _properties[Prop.TargetCompID].Value.ToString();
            string fileName = MakeFileName(senderCompID, targetCompID);

            using (FileStream file = File.Open(fileName, FileMode.Create))
            {
                using (var stream = new StreamWriter(file))
                    stream.Write("{0} : {1}", _properties[Prop.SenderSeqNum].Value, _properties[Prop.TargetSeqNum].Value);
            }
        }

        public void LoadSeqNumbers()
        {
            if (!IsHandleSeqNumbers)
                return;

            string senderCompID = _properties[Prop.SenderCompID].Value.ToString();
            string targetCompID = _properties[Prop.TargetCompID].Value.ToString();
            string fileName = MakeFileName(senderCompID, targetCompID);

            if (!File.Exists(fileName))
                return;

            using (FileStream file = File.OpenRead(fileName))
            {
                using (var stream = new StreamReader(file))
                {
                    var line = stream.ReadLine();
                    if (line == null)
                        return;

                    string[] items = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    _properties.AddProp(Prop.SenderSeqNum, int.Parse(items[0]));
                    _properties.AddProp(Prop.TargetSeqNum, int.Parse(items[1]));
                }
            }
        }

        private static string MakeFileName(string senderCompID, string targetCompID)
        {
            return $"{senderCompID}_{targetCompID}.hb";
        }

        public void MessageCommand(string msgVarName, MsgCommand command)
        {
            if (command == null)
                throw new ExecutionException("Invalid command.");

            if (command is OutgoingMsgCommand)
                ExecuteOutgoingMsgCommand(msgVarName, (OutgoingMsgCommand)command);
            else if (command is IncomingMsgCommand)
                ExecuteIncomingMsgCommand(msgVarName, (IncomingMsgCommand)command);
        }

        public void Exec(string filename, string scriptName)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ExecutionException("Filename not specified.");
            }

            if (!string.IsNullOrEmpty(scriptName))
            {
                WriteLine("Executing '{0}' in '{1}' ...", scriptName, filename);
            }

            Parser parser = CreateParser(File.ReadAllText(filename), filename);
            parser.Parse();

            if (!string.IsNullOrEmpty(scriptName))
            {
                WriteLine("Executing '{0}' successfully completed.", scriptName);
            }
        }

        public void Test(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ExecutionException("Filename not specified.");
            }

            WriteLine("Starting test '{0}' ...", filename);

            try
            {
                Parser parser = CreateParser(File.ReadAllText(filename), filename);
                parser.Parse();
                WriteLine("Test '{0}' successfully completed.", filename);
                _testStat.TestSucceeded();
            }
            catch (Exception e)
            {
                WriteLine("Test '{0}' failed: {1}", filename, e.Message);
                _testStat.TestFailed();
            }
        }

        public void TestStat(bool reset)
        {
            if (reset)
            {
                _testStat.Reset();
            }
            else
            {
                WriteLine("Test statistics: {0}", _testStat);
            }
        }

        public void ResetSeqnums()
        {
            SetSeqNumbers(1, 1);
        }

        public void SetSeqNumbers(int senderSeqNum, int targetSeqNum)
        {
            if (senderSeqNum != -1)
                _properties[Prop.SenderSeqNum].Value = senderSeqNum;
            if (targetSeqNum != -1)
                _properties[Prop.TargetSeqNum].Value = targetSeqNum;
        }

        public void EnsurePureOrderStatus(string msgVarName, Object ordStatus)
        {
            MsgVar varbl = GetMsgVar(msgVarName);
            varbl.EnsureValueFix();

            object value = GetObjectValue(ordStatus, null);
            value = QFReflector.DenormalizeFieldValue(value, typeof(OrdStatus));

            FixProtocol.EnsurePureOrderStatus(varbl.Value.QFMessage, (char)value);
        }

        public void EnsureOrderStatus(string msgVarName, Object ordStatus)
        {
            MsgVar varbl = GetMsgVar(msgVarName);
            varbl.EnsureValueFix();

            object value = GetObjectValue(ordStatus, null);
            value = QFReflector.DenormalizeFieldValue(value, typeof(OrdStatus));

            FixProtocol.EnsureOrderStatus(varbl.Value.QFMessage, (char)value);
        }

        public void EnsureModifyAccepted(string msgVarName, Object ordStatus)
        {
            MsgVar varbl = GetMsgVar(msgVarName);
            varbl.EnsureValueFix();

            object value = GetObjectValue(ordStatus, null);
            value = QFReflector.DenormalizeFieldValue(value, typeof(OrdStatus));

            FixProtocol.EnsureModifyAccepted(varbl.Value.QFMessage, (char)value);
        }

        public void EnsureTrade(string msgVarName, Object ordStatus, int? qty = null, double? price = null)
        {
            MsgVar varbl = GetMsgVar(msgVarName);
            varbl.EnsureValueFix();

            object value = GetObjectValue(ordStatus, null);
            value = QFReflector.DenormalizeFieldValue(value, typeof(OrdStatus));

            FixProtocol.EnsureTrade(varbl.Value.QFMessage, (char)value, qty, price);
        }

        public void Sleep(TimeSpan timeout)
        {
            Thread.Sleep(timeout);
        }

        public void AnyKey()
        {
            Console.ReadKey(true);
        }
    }
}