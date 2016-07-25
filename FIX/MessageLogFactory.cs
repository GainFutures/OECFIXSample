using QuickFix;

namespace OEC.FIX.Sample.FIX
{
    public sealed class MessageLogFactory : ILogFactory
    {
        #region LogFactory Members
        public ILog Create()
        {
            return new MessageLog(null);
        }

        public ILog Create(SessionID sessionID)
        {
            return new MessageLog(sessionID);
        }
        #endregion
    }
}