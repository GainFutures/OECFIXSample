using System;
using System.Linq;
using OEC.FIX.Sample.FIX.Fields;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;
using Tags = QuickFix.Fields.Tags;

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

        public SessionID SessionID => Session.SessionID;
        public Session Session { get; private set; }

        private int _inititalSenderSeqNum;
        private int _inititalTargetSeqNum;

        public void Create(Props properties)
        {
            _messageStoreFactory = new MessageStoreFactory(properties);
            var messageFactory = new DefaultMessageFactory();

            _inititalSenderSeqNum = (int)properties[Prop.SenderSeqNum].Value;
            _inititalTargetSeqNum = (int)properties[Prop.TargetSeqNum].Value;

            SessionSettings sessionSettings = GetSessionSettings();

            _initiator = new SocketInitiator(this, _messageStoreFactory, sessionSettings, new MessageLogFactory(), messageFactory);
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
            OnFromAdmin?.Invoke(sessionID, msg);
        }

        public void FromApp(Message msg, SessionID sessionID)
        {
            OnFromApp?.Invoke(sessionID, msg);
        }

        public void OnCreate(SessionID sessionID)
        {
            Session = Session.LookupSession(sessionID);
            Session.NextSenderMsgSeqNum = _inititalSenderSeqNum;
            Session.NextTargetMsgSeqNum = _inititalTargetSeqNum;
        }

        public void OnLogon(SessionID sessionID)
        {
            Logon?.Invoke(sessionID);
        }

        public void OnLogout(SessionID sessionID)
        {
            Logout?.Invoke(sessionID);
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

            OnToAdmin?.Invoke(sessionID, msg);
        }

        public void ToApp(Message msg, SessionID sessionID)
        {
            OnToApp?.Invoke(sessionID, msg);
        }

        #endregion
    }
}