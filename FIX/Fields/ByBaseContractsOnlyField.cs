using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class ByBaseContractsOnlyField : BooleanField
	{
		public ByBaseContractsOnlyField(bool value)
			: base(Tags.ByBaseContractsOnlyField, value)
		{
		}

		public ByBaseContractsOnlyField()
			: this(false)
		{
		}
	}
}