namespace OEC.FIX.Sample.FoxScript
{
    internal class MsgVar
    {
        public static readonly MsgVar Null = new MsgVar("NULL");

        public readonly string Name;
        public MessageWrapper Value;

        public MsgVar(string name)
        {
            Name = name;
        }

        public void EnsureValueFix()
        {
            if (Value?.QFMessage == null)
                throw new ExecutionException("Message var '{0}' is NULL.", Name);
        }

        public void EnsureValue()
        {
            if (Value == null)
                throw new ExecutionException("Message var '{0}' is NULL.", Name);
        }

        public void EnsureValueFAST()
        {
            if (Value?.OFMessage == null)
                throw new ExecutionException("Message var '{0}' is NULL.", Name);
        }

        public override string ToString()
        {
            string s = Name + ": ";

            if (Value == null)
                return s + "NULL";

            //return s + QFReflector.FormatMessage(Value);
            return s + MessageWrapper.FormatMessage(Value);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MsgVar;
            if (this == null || other == null)
                throw new ExecutionException("Message var(s) not specified.");

            if (Value == other.Value)
                return true;

            return Value?.Equals(other.Value) ?? other.Value.Equals(Value);
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}