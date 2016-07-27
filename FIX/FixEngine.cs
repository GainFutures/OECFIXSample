using System;
using System.Collections.Generic;
using System.Threading;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace OEC.FIX.Sample.FIX
{
    internal class FixEngine
    {
        private readonly Props _properties;
        private readonly object _connectionLock = new object();
        private readonly ManualResetEvent _messageEvent = new ManualResetEvent(false);
        private readonly List<Message> _messages = new List<Message>();
        private Connection _connection;
        private ManualResetEvent _logonEvent;
        private ManualResetEvent _logoutEvent;

        public FixEngine(Props properties)
        {
            _properties = properties;
        }

        public void SendMessage(Message msg)
        {
            EnsureConnected();
            _connection.SendMessage(msg);
        }

        public void Ping()
        {
            EnsureConnected();

            var testReqID = new TestReqID(Tools.GenerateUniqueTimestamp());
            var msg = new TestRequest(testReqID);
            _connection.SendMessage(msg);

            WriteLine("Ping: {0} SN={1}", testReqID.getValue(), msg.Header.GetInt(Tags.MsgSeqNum));
        }

        public void Connect(string password, string uuid)
        {
            if (string.IsNullOrWhiteSpace(password) && _properties.Contains(Prop.Password))
                password = _properties[Prop.Password].Value as string;

            if (string.IsNullOrWhiteSpace(password))
                throw new ExecutionException("Password is not specified");

            if (string.IsNullOrWhiteSpace(uuid) && _properties.Contains(Prop.UUID))
                uuid = _properties[Prop.UUID].Value as string;

            lock (_connectionLock)
            {
                if (_connection != null)
                {
                    WriteLine("Already connected to FIX server.");
                    return;
                }

                _connection = new Connection(_properties);
                _connection.Logon += ConnectionOnLogon;
                _connection.Logout += ConnectionLogout;
                _connection.OnFromAdmin += ConnectionOnFromAdmin;
                _connection.OnFromApp += ConnectionOnFromApp;

                _connection.Create();

                MessageLog.SessionEvent += MessageLog_SessionEvent;
                MessageLog.OnIncomingMessage += MessageLog_OnIncomingMessage;
                MessageLog.OnOutgoingMessage += MessageLog_OnOutgoingMessage;
                MessageStoreBase.SenderSeqNumChanged += MessageStore_SenderSeqNumChanged;
                MessageStoreBase.TargetSeqNumChanged += MessageStore_TargetSeqNumChanged;

                bool connected;
                using (_logonEvent = new ManualResetEvent(false))
                {
                    _connection.Open(password, uuid);
                    connected = _logonEvent.WaitOne((TimeSpan)_properties[Prop.ConnectTimeout].Value);
                }
                _logonEvent = null;

                if (!connected)
                {
                    Disconnect();
                    throw new ExecutionException("Connecting to FIX server timed out.");
                }
            }
        }

        private void MessageLog_OnOutgoingMessage(SessionID sessionID, string message)
        {
            Console.WriteLine($"Out> {message.Replace('\x01', '|')}");
        }

        private void MessageLog_OnIncomingMessage(SessionID sessionID, string message)
        {
            Console.WriteLine($" In> {message.Replace('\x01', '|')}");
        }

        public void Disconnect()
        {
            lock (_connectionLock)
            {
                if (_connection?.Session != null)
                {
                    if (_connection.Session.IsLoggedOn)
                        _logoutEvent = new ManualResetEvent(false);
                    else
                        DestroyConnection();
                }
                else
                    WriteLine("Already disconnected from FIX server.");
            }

            if (_logoutEvent != null)
            {
                using (_logoutEvent)
                {
                    _connection?.Close();
                    _logoutEvent.WaitOne();
                }
                _logoutEvent = null;
                DestroyConnection();
            }
        }

        public Message WaitMessage(string msgType, TimeSpan timeout, Predicate<Message> predicate)
        {
            EnsureConnected();

            if (timeout < TimeSpan.Zero)
            {
                throw new ExecutionException("Invalid Timeout.");
            }

            lock (_messages)
            {
                Message msg = RetrieveMessage(msgType, predicate);
                if (msg != null)
                {
                    _messageEvent.Reset();
                    return msg;
                }
            }

            if (timeout == TimeSpan.Zero)
            {
                return null;
            }

            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                if (_messageEvent.WaitOne(timeout))
                {
                    lock (_messages)
                    {
                        Message msg = RetrieveMessage(msgType, predicate);
                        if (msg != null)
                        {
                            _messageEvent.Reset();
                            return msg;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        private Message RetrieveMessage(string msgType, Predicate<Message> predicate)
        {
            if (string.IsNullOrEmpty(msgType))
            {
                throw new ExecutionException("Invalid MsgType.");
            }

            for (int i = 0; i < _messages.Count; ++i)
            {
                Message msg = _messages[i];
                if (msgType == (msg.Header.IsSetField(Tags.MsgType) ? msg.Header.GetString(Tags.MsgType) : null))
                {
                    if (predicate != null)
                    {
                        try
                        {
                            if (predicate(msg))
                            {
                                _messages.RemoveAt(i);
                                return msg;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        _messages.RemoveAt(i);
                        return msg;
                    }
                }
            }
            return null;
        }

        private void DestroyConnection()
        {
            MessageLog.SessionEvent -= MessageLog_SessionEvent;
            MessageLog.OnIncomingMessage -= MessageLog_OnIncomingMessage;
            MessageLog.OnOutgoingMessage -= MessageLog_OnOutgoingMessage;
            MessageStoreBase.SenderSeqNumChanged -= MessageStore_SenderSeqNumChanged;
            MessageStoreBase.TargetSeqNumChanged -= MessageStore_TargetSeqNumChanged;

            lock (_messages)
            {
                _messages.Clear();
            }

            _connection.Destroy();

            _connection.Logon -= ConnectionOnLogon;
            _connection.Logout -= ConnectionLogout;
            _connection.OnFromAdmin -= ConnectionOnFromAdmin;
            _connection.OnFromApp -= ConnectionOnFromApp;
            _connection = null;

            WriteLine("Disconnected.");
        }

        private void MessageStore_TargetSeqNumChanged(SessionID sessionID, int seqnum)
        {
            _properties[Prop.TargetSeqNum].Value = seqnum;
        }

        private void MessageStore_SenderSeqNumChanged(SessionID sessionID, int seqnum)
        {
            _properties[Prop.SenderSeqNum].Value = seqnum;
        }

        private void MessageLog_SessionEvent(SessionID sessionID, string text)
        {
            WriteLine(text);
        }

        private void ConnectionOnFromApp(SessionID sessionID, Message msg)
        {
            lock (_messages)
            {
                _messages.Add(msg);
                _messageEvent.Set();
            }
        }

        private void ConnectionOnFromAdmin(SessionID sessionID, Message msg)
        {
            string msgType = msg.MsgType();
            if (msgType == MsgType.LOGOUT || msgType == MsgType.LOGON)
            {
                string text = msg.Get<Text>(null);
                if (!string.IsNullOrEmpty(text))
                {
                    WriteLine("From server: {0}", text);
                }
                if (msgType == MsgType.LOGON)
                {
                    _properties[Prop.FastHashCode].Value = msg.GetString(12004);
                }
            }
            else if (msgType == MsgType.HEARTBEAT)
            {
                if (msg.IsSetField(Tags.TestReqID))
                {
                    WriteLine("Ping response: {0} {1} SN={2}", msg.Get<TestReqID>(null), msg.Get<Text>(null), msg.Header.GetInt(Tags.MsgSeqNum));
                }
            }
        }

        private void ConnectionLogout(SessionID sessionID)
        {
            WriteLine("Logged out: {0}", sessionID);
            _logoutEvent?.Set();
        }

        private void ConnectionOnLogon(SessionID sessionID)
        {
            WriteLine("Logged on: {0}", sessionID);

            ManualResetEvent ev = _logonEvent;
            if (ev != null)
            {
                try
                {
                    ev.Set();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            Console.WriteLine("FIX: " + format, args);
        }

        private void EnsureConnected()
        {
            if (_connection == null)
            {
                throw new ExecutionException("Not connected to FIX server.");
            }
        }
    }
}