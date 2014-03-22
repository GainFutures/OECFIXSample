using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class MaxRecordsField : IntField
	{
		public const int FIELD = 12051;

		public MaxRecordsField(int value)
			: base(FIELD, value)
		{
		}

		public MaxRecordsField()
			: this(0)
		{
		}
	}
}