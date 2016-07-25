using QuickFix;

namespace OEC.FIX.Sample.FIX
{
    sealed class MessageStoreFactory : IMessageStoreFactory
    {
        private readonly Props _properties;

        public MessageStoreFactory(Props properties)
        {
            _properties = properties;
        }

        #region MessageStoreFactory Members
        public IMessageStore Create(SessionID sessionID)
        {
            var store = new MessageStore(_properties);
            store.Init(sessionID);
            return store;
        }
        #endregion
    }
}