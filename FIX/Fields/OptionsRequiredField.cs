using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class OptionsRequiredField : BooleanField
    {
        public const int FIELD = 12057;

        public OptionsRequiredField(bool value)
            : base(FIELD, value)
        {
        }

        public OptionsRequiredField()
            : this(false)
        {
        }
    }
}
