using System;
using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class UpdatesSinceTimestamp : DateTimeField
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
