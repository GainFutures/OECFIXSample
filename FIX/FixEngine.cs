using System;
using System.Collections.Generic;
using System.Threading;
using OEC.FIX.Sample;
using QuickFix;
using QuickFix44;
using Message = QuickFix.Message;

namespace OEC.FIX.Sample.FIX
{
	internal class FixEngine
	{
		private readonly object _connectionLock = new object();
		private readonly ManualResetEvent _messageEvent = new ManualResetEvent(false);
		private readonly List<Message> _messages = new List<Message>();
		private Connection _connection;
		private ManualResetEvent _logonEvent;
		private ManualResetEvent _logoutEvent;

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

			WriteLine("Ping: {0} SN={1}", testReqID.getValue(), msg.getHeader().getInt(MsgSeqNum.FIELD));
		}

		public void Connect(string password, string uuid)
			{
			if (string.IsNullOrEmpty(password)) {
				throw new ExecutionException("Password not specified");
			}

			lock (_connectionLock)
			{
				if (_connection != null)
				{
					WriteLine("Already connected to FIX server.");
					return;
				}

				_connection = new Connection();
				_connection.Logon += Connection_Logon;
				_connection.Logout += Connection_Logout;
				_connection.FromAdmin += Connection_FromAdmin;
				_connection.FromApp += Connection_FromApp;

				_connection.Create((int) Program.Props[Prop.SenderSeqNum].Value, (int) Program.Props[Prop.TargetSeqNum].Value);

				MessageLog.SessionEvent += MessageLog_SessionEvent;
				MessageLog.OnIncomingMessage += MessageLog_OnIncomingMessage;
				MessageLog.OnOutgoingMessage += MessageLog_OnOutgoingMessage;
				MessageStore.SenderSeqNumChanged += MessageStore_SenderSeqNumChanged;
				MessageStore.TargetSeqNumChanged += MessageStore_TargetSeqNumChanged;

				bool connected;
				using (_logonEvent = new ManualResetEvent(false))
				{
					_connection.Open(null, password);
					connected = _logonEvent.WaitOne((TimeSpan) Program.Props[Prop.ConnectTimeout].Value);
				}
				_logonEvent = null;

				if (!connected)
				{
					Disconnect();
					throw new ExecutionException("Connecting to FIX server timed out.");
				}
			}
		}

		private void MessageLog_OnOutgoingMessage(SessionID sessionID, string msg)
		{
			Console.WriteLine("<out>" + msg);
		}

		private void MessageLog_OnIncomingMessage(SessionID sessionID, string msg)
		{
			Console.WriteLine("<inc>" + msg);
		}


		public void Disconnect()
		{
			lock (_connectionLock)
			{
				if (_connection != null)
				{
					if (_connection.Session.isLoggedOn())
					{
						_logoutEvent = new ManualResetEvent(false);
					}
					else
					{
						DestroyConnection();
					}
				}
				else
				{
					WriteLine("Already disconnected from FIX server.");
				}
			}

			if (_logoutEvent != null)
			{
				using (_logoutEvent)
				{
					_connection.Close();
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
				if (msgType == (msg.getHeader().isSetField(MsgType.FIELD) ? msg.getHeader().getString(MsgType.FIELD) : null))
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
			MessageStore.SenderSeqNumChanged -= MessageStore_SenderSeqNumChanged;
			MessageStore.TargetSeqNumChanged -= MessageStore_TargetSeqNumChanged;

			lock (_messages)
			{
				_messages.Clear();
			}

			_connection.Destroy();

			_connection.Logon -= Connection_Logon;
			_connection.Logout -= Connection_Logout;
			_connection.FromAdmin -= Connection_FromAdmin;
			_connection.FromApp -= Connection_FromApp;
			_connection = null;

			WriteLine("Disconnected.");
		}

		private void MessageStore_TargetSeqNumChanged(SessionID sessionID, int seqnum)
		{
			Program.Props[Prop.TargetSeqNum].Value = seqnum;
		}

		private void MessageStore_SenderSeqNumChanged(SessionID sessionID, int seqnum)
		{
			Program.Props[Prop.SenderSeqNum].Value = seqnum;
		}

		private void MessageLog_SessionEvent(SessionID sessionID, string text)
		{
			WriteLine(text);
		}

		private void Connection_FromApp(SessionID sessionID, Message msg)
		{
			lock (_messages)
			{
				_messages.Add(msg);
				_messageEvent.Set();
			}
		}

		private void Connection_FromAdmin(SessionID sessionID, Message msg)
		{
			string msgType = msg.MsgType();
			if (msgType == MsgType.Logout || msgType == MsgType.Logon)
			{
				string text = msg.Get<Text>(null);
				if (!string.IsNullOrEmpty(text))
				{
					WriteLine("From server: {0}", text);
				}
			}
			if (msgType == MsgType.UserResponse || msgType == MsgType.Logon)
			{
				Program.Props[Prop.FastHashCode].Value = msg.getString(12004);
			}
			if (msgType == MsgType.Heartbeat)
			{
				if (msg.isSetField(TestReqID.FIELD))
				{
					WriteLine("Ping response: {0} {1} SN={2}", msg.Get<TestReqID>(null), msg.Get<Text>(null),
						msg.getHeader().getInt(MsgSeqNum.FIELD));
				}
			}
		}

		private void Connection_Logout(SessionID sessionID)
		{
			WriteLine("Logged out: {0}", sessionID);

			if (_logoutEvent != null)
			{
				_logoutEvent.Set();
			}
/*
			else {
				lock (connectionLock) {
					if (connection != null) {
						DestroyConnection();
					}
				}
			}
*/
		}

		private void Connection_Logon(SessionID sessionID)
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