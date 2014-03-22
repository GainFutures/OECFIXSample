using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class TrailingTriggerType : CharField
	{
		public const int FIELD = 12002;

		public const char ASK = 'A';
		public const char BID = 'B';
		public const char LAST = 'L';

		public TrailingTriggerType()
			: base(FIELD)
		{
		}

		public TrailingTriggerType(char value)
			: base(FIELD, value)
		{
		}
	}
}