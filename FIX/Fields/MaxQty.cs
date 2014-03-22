using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class MaxQty : IntField
	{
		public const int FIELD = 12066;

		public MaxQty() : base(FIELD)
		{
		}

		public MaxQty(int value) : base(FIELD, value)
		{
		}
	}
}