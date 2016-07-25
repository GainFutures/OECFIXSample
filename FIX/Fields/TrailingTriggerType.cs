using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class TrailingTriggerType : CharField
    {
        public const char ASK = 'A';
        public const char BID = 'B';
        public const char LAST = 'L';

        public TrailingTriggerType()
            : base(Tags.TrailingTriggerType)
        {
        }

        public TrailingTriggerType(char value)
            : base(Tags.TrailingTriggerType, value)
        {
        }
    }
}