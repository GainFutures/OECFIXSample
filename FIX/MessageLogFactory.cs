using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public sealed class MessageLogFactory : LogFactory
	{
		#region LogFactory Members

		public Log create()
		{
			return new MessageLog(null);
		}

		public Log create(SessionID sessionID)
		{
			return new MessageLog(sessionID);
		}

		#endregion
	}
}