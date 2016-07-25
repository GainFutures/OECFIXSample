using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class ContractGroupField : StringField
    {
        public ContractGroupField(string value)
            : base(Tags.ContractGroupField, value)
        {
        }

        public ContractGroupField()
            : this(string.Empty)
        {
        }
    }
}