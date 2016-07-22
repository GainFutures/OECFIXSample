using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
	public class UUIDField : StringField
	{
		public const int FIELD = 12003;

		public UUIDField(string value)
			: base(FIELD, value)
		{
		}

		public UUIDField()
			: this(string.Empty)
		{
		}
	}
}