using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OEC.FIX.Sample.FIX;
using QuickFix;

namespace OEC.FIX.Sample.FoxScript
{
	partial class ExecEngine
	{
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
			{
				return;
			}
			foreach (object arg in args)
			{
				WriteLine("{0}", GetSyntaxConstructionValue(arg));
			}
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
			Program.Props[name].Value = GetObjectValue(value, null);
			GetPropsValue(name);
		}

		public void GetPropsValue(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				foreach (Prop prop in Program.Props)
				{
					WriteLine(Tools.FormatProp(prop));
				}
			}
			else
			{
				WriteLine(Tools.FormatProp(Program.Props[name]));
			}
		}

		public void Ping()
		{
			_fixEngine.Ping();
		}

		public void Auth(string senderCompID)
		{
			StoreSeqNumbers();
			Program.Props[Prop.SenderCompID].Value = senderCompID;
			LoadSeqNumbers();
		}

		public void Connect(string senderCompID, string password, string uuid)
		{
			if (!string.IsNullOrEmpty(senderCompID))
				Auth(senderCompID);

			WriteLine("Connecting to {0}:{1} as '{2}' ...",
				Program.Props[Prop.Host].Value,
				Program.Props[Prop.Port].Value,
				Program.Props[Prop.SenderCompID].Value);

			_fixEngine.Connect(password, uuid);
		}

		public void ConnectFast(string userName)
		{
			if (String.IsNullOrWhiteSpace(userName))
				userName = Program.Props[Prop.SenderCompID].Value.ToString();

			string password = Program.Props[Prop.FastHashCode].Value.ToString();

			if (String.IsNullOrWhiteSpace(password))
				throw new ExecutionException("FastHashCode not set.");

			_fastClient.Connect(userName, password);
		}

		public void FASTHeartbeat()
		{
			_fastClient.SendHearbeat();
		}


		public void Disconnect()
		{
			_fixEngine.Disconnect();
			StoreSeqNumbers();
		}

		public void DisconnectFast()
		{
			_fastClient.Disconnect();
		}

		public void StoreSeqNumbers()
		{
			string senderCompID = Program.Props[Prop.SenderCompID].Value.ToString();
			string targetCompID = Program.Props[Prop.TargetCompID].Value.ToString();
			string fileName = MakeFileName(senderCompID, targetCompID);

			using (FileStream file = File.Open(fileName, FileMode.Create))
			{
				using (var stream = new StreamWriter(file))
					stream.Write("{0} : {1}", Program.Props[Prop.SenderSeqNum].Value, Program.Props[Prop.TargetSeqNum].Value);
			}
		}

		public void LoadSeqNumbers()
		{
			string senderCompID = Program.Props[Prop.SenderCompID].Value.ToString();
			string targetCompID = Program.Props[Prop.TargetCompID].Value.ToString();
			string fileName = MakeFileName(senderCompID, targetCompID);

			if (!File.Exists(fileName)) return;

			using (FileStream file = File.OpenRead(fileName))
			{
				using (var stream = new StreamReader(file))
				{
					string[] items = stream.ReadLine().Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);

					Program.Props.AddProp(Prop.SenderSeqNum, int.Parse(items[0]));
					Program.Props.AddProp(Prop.TargetSeqNum, int.Parse(items[1]));
				}
			}
		}

		private static string MakeFileName(string senderCompID, string targetCompID)
		{
			return string.Format("{0}_{1}.hb", senderCompID, targetCompID);
		}

		public void MessageCommand(string msgVarName, MsgCommand command)
		{
			if (command == null)
			{
				throw new ExecutionException("Invalid command.");
			}

			if (command is OutgoingMsgCommand)
			{
				ExecuteOutgoingMsgCommand(msgVarName, command as OutgoingMsgCommand);
			}
			else if (command is IncomingMsgCommand)
			{
				ExecuteIncomingMsgCommand(msgVarName, command as IncomingMsgCommand);
			}
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
				Program.Props[Prop.SenderSeqNum].Value = senderSeqNum;
			if (targetSeqNum != -1)
				Program.Props[Prop.TargetSeqNum].Value = targetSeqNum;
		}

		public void EnsurePureOrderStatus(string msgVarName, Object ordStatus)
		{
			MsgVar varbl = GetMsgVar(msgVarName);
			varbl.EnsureValueFix();

			object value = GetObjectValue(ordStatus, null);
			value = QFReflector.DenormalizeFieldValue(value, typeof (OrdStatus));

			FixProtocol.Current.EnsurePureOrderStatus(varbl.Value.QFMessage, (char) value);
		}

		public void EnsureOrderStatus(string msgVarName, Object ordStatus)
		{
			MsgVar varbl = GetMsgVar(msgVarName);
			varbl.EnsureValueFix();

			object value = GetObjectValue(ordStatus, null);
			value = QFReflector.DenormalizeFieldValue(value, typeof (OrdStatus));

			FixProtocol.Current.EnsureOrderStatus(varbl.Value.QFMessage, (char) value);
		}

		public void EnsureModifyAccepted(string msgVarName, Object ordStatus)
		{
			MsgVar varbl = GetMsgVar(msgVarName);
			varbl.EnsureValueFix();

			object value = GetObjectValue(ordStatus, null);
			value = QFReflector.DenormalizeFieldValue(value, typeof (OrdStatus));

			FixProtocol.Current.EnsureModifyAccepted(varbl.Value.QFMessage, (char) value);
		}

		public void EnsureTrade(string msgVarName, Object ordStatus, int? qty = null, double? price = null)
		{
			MsgVar varbl = GetMsgVar(msgVarName);
			varbl.EnsureValueFix();

			object value = GetObjectValue(ordStatus, null);
			value = QFReflector.DenormalizeFieldValue(value, typeof (OrdStatus));

			FixProtocol.Current.EnsureTrade(varbl.Value.QFMessage, (char) value, qty, price);
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