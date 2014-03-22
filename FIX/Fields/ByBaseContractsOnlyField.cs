using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class ByBaseContractsOnlyField : BooleanField
	{
		public const int FIELD = 12056;

		public ByBaseContractsOnlyField(bool value)
			: base(FIELD, value)
		{
		}

		public ByBaseContractsOnlyField()
			: this(false)
		{
		}
	}
}