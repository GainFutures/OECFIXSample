using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public sealed class MessageLog : Log
	{
		public delegate void MessageHandler(SessionID sessionID, string msg);

		public delegate void SessionEventHandler(SessionID sessionID, string text);

		public MessageLog(SessionID sessionID)
		{
			SessionID = sessionID;
		}

		public SessionID SessionID { get; private set; }

		public static event SessionEventHandler SessionEvent;

		public static event MessageHandler OnIncomingMessage;
		public static event MessageHandler OnOutgoingMessage;

		#region Log Members

		public void clear()
		{
		}

		public void backup()
		{
		}

		public void onEvent(string text)
		{
			SessionEventHandler handler = SessionEvent;
			if (handler != null)
			{
				handler(SessionID, text);
			}
		}

		public void onIncoming(string msg)
		{
			MessageHandler handler = OnIncomingMessage;
			if (handler != null)
			{
				handler(SessionID, msg);
			}
		}

		public void onOutgoing(string msg)
		{
			MessageHandler handler = OnOutgoingMessage;
			if (handler != null)
			{
				handler(SessionID, msg);
			}
		}

		#endregion
	}
}