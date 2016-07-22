using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class OptionsRequiredField : BooleanField
    {
        public OptionsRequiredField(bool value)
            : base(Tags.OptionsRequiredField, value)
        {
        }

        public OptionsRequiredField()
            : this(false)
        {
        }
    }
}
