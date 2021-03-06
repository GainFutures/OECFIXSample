﻿using QuickFix;

namespace OEC.FIX.Sample.FIX
{
    public sealed class MessageLog : ILog
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

        public void Clear()
        {
        }

        public void OnEvent(string text)
        {
            SessionEvent?.Invoke(SessionID, text);
        }

        public void OnIncoming(string msg)
        {
            OnIncomingMessage?.Invoke(SessionID, msg);
        }

        public void OnOutgoing(string msg)
        {
            OnOutgoingMessage?.Invoke(SessionID, msg);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}