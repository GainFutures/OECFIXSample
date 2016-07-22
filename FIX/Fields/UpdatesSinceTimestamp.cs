using System;
using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class UpdatesSinceTimestamp : DateTimeField
    {
        public UpdatesSinceTimestamp(DateTime data)
            : base(Tags.UpdatesSinceTimestamp, data)
        {
        }

        public UpdatesSinceTimestamp()
            : base(Tags.UpdatesSinceTimestamp)
        {
        }
    }
}
