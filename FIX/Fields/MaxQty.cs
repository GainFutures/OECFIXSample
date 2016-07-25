using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class MaxQty : IntField
    {
        public MaxQty() : base(Tags.MaxQty)
        {
        }

        public MaxQty(int value) : base(Tags.MaxQty, value)
        {
        }
    }
}