using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class TrailingAmountInPercents : BooleanField
    {
        public TrailingAmountInPercents()
            : this(false)
        {
        }

        public TrailingAmountInPercents(bool value)
            : base(Tags.TrailingAmountInPercents, value)
        {
        }
    }
}