using OEC.FIX.Sample.FIX.Fields;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;

namespace OEC.FIX.Sample.FIX
{
	abstract class ConnectionBase : IApplication
    {
		public delegate void MessageHandler(SessionID sessionID, Message msg);

		public delegate void SessionEventHandler(SessionID sessionID);

		private SocketInitiator _initiator;
		private ILogFactory _logFactory;
		private MessageStoreFactory _messageStoreFactory;
		private string _password;
		private string _uuid = "9e61a8bc-0a31-4542-ad85-33ebab0e4e86";

		public SessionID SessionID { get; private set; }

		public void Create(Props properties)
		{
			_messageStoreFactory = new MessageStoreFactory(properties);
			var messageFactory = new DefaultMessageFactory();

			SessionSettings sessionSettings = GetSessionSettings();
			_logFactory = new MessageLogFactory();
			_initiator = new SocketInitiator(this, _messageStoreFactory, sessionSettings, _logFactory, messageFactory);
		}

		public void Destroy()
		{
			if (_initiator != null)
			{
				_initiator.Stop();
				_initiator.Dispose();
				_initiator = null;
			}

			_messageStoreFactory = null;
			_logFactory = null;
		}

		public virtual void Open(string password, string uuid)
		{
			_password = password;
            _uuid = uuid;

			_initiator.Start();
		}

		public void Close()
		{
			_initiator.Stop();
		}

		public event MessageHandler OnFromAdmin;
		public event MessageHandler OnFromApp;
		public event MessageHandler OnToAdmin;
		public event MessageHandler OnToApp;

		public event SessionEventHandler Logon;
		public event SessionEventHandler Logout;

		public void SendMessage(Message msg)
		{
			Session.SendToTarget(msg, SessionID);
		}

		protected abstract SessionSettings GetSessionSettings();

		#region Application Members

		public void FromAdmin(Message msg, SessionID sessionID)
		{
			MessageHandler handler = OnFromAdmin;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void FromApp(Message msg, SessionID sessionID)
		{
			MessageHandler handler = OnFromApp;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void OnCreate(SessionID sessionID)
		{
			SessionID = sessionID;
		}

		public void OnLogon(SessionID sessionID)
		{
			SessionEventHandler handler = Logon;
			if (handler != null)
			{
				handler(sessionID);
			}
		}

		public void OnLogout(SessionID sessionID)
		{
			SessionEventHandler handler = Logout;
			if (handler != null)
			{
				handler(sessionID);
			}
		}

		public void ToAdmin(Message msg, SessionID sessionID)
		{
            if (msg.Header.GetString(Tags.MsgType) == MsgType.LOGON)
			{
				if (!string.IsNullOrEmpty(_password))
				{
					msg.SetField(new Password(_password));
				}

                if (!string.IsNullOrEmpty(_uuid))
				{
                    msg.SetField(new UUIDField(_uuid));
				}
			}

			MessageHandler handler = OnToAdmin;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void ToApp(Message msg, SessionID sessionID)
		{
			MessageHandler handler = OnToApp;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		#endregion
	}
}