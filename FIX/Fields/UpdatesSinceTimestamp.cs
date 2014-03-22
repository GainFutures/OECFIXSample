using System;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class UpdatesSinceTimestamp : QuickFix.UtcTimeStampField
    {
        public const int FIELD = 12072;

        public UpdatesSinceTimestamp(DateTime data)
            : base(FIELD, data)
        {
        }

        public UpdatesSinceTimestamp()
            : base(FIELD)
        {
        }
    }
}
