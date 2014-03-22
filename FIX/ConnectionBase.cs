using OEC.FIX.Sample.FIX.Fields;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public abstract class ConnectionBase<TMessageStore> : Application
		where TMessageStore : MessageStoreBase, new()
	{
		public delegate void MessageHandler(SessionID sessionID, Message msg);

		public delegate void SessionEventHandler(SessionID sessionID);

		private SocketInitiator _initiator;
		private LogFactory _logFactory;
		private MessageStoreFactory _messageStoreFactory;
		private string _password;
		private const string UUID = "9e61a8bc-0a31-4542-ad85-33ebab0e4e86";

		public SessionID SessionID { get; private set; }

		public void Create()
		{
			_messageStoreFactory = new MessageStoreFactory<TMessageStore>();
			var messageFactory = new DefaultMessageFactory();

			SessionSettings sessionSettings = GetSessionSettings();
			_logFactory = new MessageLogFactory();
			_initiator = new SocketInitiator(this, _messageStoreFactory, sessionSettings, _logFactory, messageFactory);
		}

		public void Destroy()
		{
			if (_initiator != null)
			{
				_initiator.stop();
				_initiator.Dispose();
				_initiator = null;
			}

			_messageStoreFactory = null;
			_logFactory = null;
		}

		public virtual void Open(string username, string password)
		{
			_password = password;

			_initiator.start();
		}

		public void Close()
		{
			_initiator.stop();
		}

		public event MessageHandler FromAdmin;
		public event MessageHandler FromApp;
		public event MessageHandler ToAdmin;
		public event MessageHandler ToApp;

		public event SessionEventHandler Logon;
		public event SessionEventHandler Logout;

		public void SendMessage(Message msg)
		{
			Session.sendToTarget(msg, SessionID);
		}

		protected abstract SessionSettings GetSessionSettings();

		#region Application Members

		public void fromAdmin(Message msg, SessionID sessionID)
		{
			MessageHandler handler = FromAdmin;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void fromApp(Message msg, SessionID sessionID)
		{
			MessageHandler handler = FromApp;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void onCreate(SessionID sessionID)
		{
			SessionID = sessionID;
		}

		public void onLogon(SessionID sessionID)
		{
			SessionEventHandler handler = Logon;
			if (handler != null)
			{
				handler(sessionID);
			}
		}

		public void onLogout(SessionID sessionID)
		{
			SessionEventHandler handler = Logout;
			if (handler != null)
			{
				handler(sessionID);
			}
		}

		public void toAdmin(Message msg, SessionID sessionID)
		{
			if (msg.getHeader().getString(MsgType.FIELD) == MsgType.Logon)
			{
				if (!string.IsNullOrEmpty(_password))
				{
					msg.setField(new Password(_password));
				}

				if (!string.IsNullOrEmpty(UUID))
				{
					msg.setField(new UUIDField(UUID));
				}
			}

			MessageHandler handler = ToAdmin;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		public void toApp(Message msg, SessionID sessionID)
		{
			MessageHandler handler = ToApp;
			if (handler != null)
			{
				handler(sessionID, msg);
			}
		}

		#endregion
	}
}