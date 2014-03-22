using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class ContractGroupField : StringField
	{
		public const int FIELD = 12054;

		public ContractGroupField(string value)
			: base(FIELD, value)
		{
		}

		public ContractGroupField()
			: this(string.Empty)
		{
		}
	}
}