using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class TrailingAmountInPercents : BooleanField
	{
		public const int FIELD = 12001;

		public TrailingAmountInPercents()
			: this(false)
		{
		}

		public TrailingAmountInPercents(bool value)
			: base(FIELD, value)
		{
		}
	}
}