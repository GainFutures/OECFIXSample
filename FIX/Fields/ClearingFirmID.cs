using QuickFix;

namespace OEC.FIX.Sample.FIX.Fields
{
	internal class ClearingFirmID : StringField
	{
		public const int FIELD = 12058;

		public ClearingFirmID(string value)
			: base(FIELD, value)
		{
		}

		public ClearingFirmID()
			: base(FIELD)
		{
		}
	}
}