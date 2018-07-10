using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    internal class OSOGroupingMethod : IntField
    {
        public const int FIELD = 12076;

        public const int ByFirstPrice = 0;
        public const int ByPrice = 1;
        public const int ByFill = 2;

        public OSOGroupingMethod(int value)
             : base(FIELD, value)
        {
        }

        public OSOGroupingMethod()
             : this(0)
        {
        }
    }
}
