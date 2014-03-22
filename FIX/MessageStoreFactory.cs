using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public sealed class MessageStoreFactory<TMessageStore> : MessageStoreFactory
		where TMessageStore : MessageStoreBase, new()
	{
		#region MessageStoreFactory Members

		public QuickFix.MessageStore create(SessionID sessionID)
		{
			var store = new TMessageStore();
			store.Init(sessionID);
			return store;
		}

		#endregion
	}
}