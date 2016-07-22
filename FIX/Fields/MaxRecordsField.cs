using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class MaxRecordsField : IntField
	{
		public MaxRecordsField(int value)
			: base(Tags.MaxRecordsField, value)
		{
		}

		public MaxRecordsField()
			: this(0)
		{
		}
	}
}