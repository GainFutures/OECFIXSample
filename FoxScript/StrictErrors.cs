namespace OEC.FIX.Sample.FoxScript
{
    internal class StrictErrors : Errors
    {
        public override void SynErr(int line, int col, int n)
        {
            base.SynErr(line, col, n);
            throw new SyntaxErrorException("Syntax error.");
        }

        public override void SemErr(int line, int col, string s)
        {
            base.SemErr(line, col, s);
            throw new ExecutionException(s);
        }

        public override void SemErr(string s)
        {
            base.SemErr(s);
            throw new ExecutionException(s);
        }
    }
}