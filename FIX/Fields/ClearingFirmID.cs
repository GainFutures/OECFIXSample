using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    internal class ClearingFirmID : StringField
    {
        public ClearingFirmID(string value)
            : base(Tags.ClearingFirmID, value)
        {
        }

        public ClearingFirmID()
            : base(Tags.ClearingFirmID)
        {
        }
    }
}