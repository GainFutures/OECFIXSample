namespace OEC.FIX.Sample.FAST
{
	public static class FastFieldsNames
	{
		public const string MDREQID = "MDReqID";
		public const string MSGTYPE = "MsgType";
	}

	public static class FastMessageTypes
	{
		public const string MarketDataRequest_Cancel = "V";
	}

    public enum MDEntryType
    {
        BID = '0',
        OFFER = '1',
        TRADE = '2',
        OPENING_PRICE = '4',
        SETTLEMENT_PRICE = '6',
        TRADE_VOLUME = 'B',
        OPEN_INTEREST = 'C',
        WORKUP_TRADE = 'w',
        EMPTY_BOOK = 'J'
    }
}