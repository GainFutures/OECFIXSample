using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class UUIDField : StringField
    {
        public UUIDField(string value)
            : base(Tags.UUIDField, value)
        {
        }

        public UUIDField()
            : this(string.Empty)
        {
        }
    }
}