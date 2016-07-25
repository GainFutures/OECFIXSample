namespace OEC.FIX.Sample.FoxScript
{
    internal class TestStatistics
    {
        public int Total => Succeeded + Failed;

        public int Failed { get; private set; }
        public int Succeeded { get; private set; }

        public void TestSucceeded()
        {
            Succeeded++;
        }

        public void TestFailed()
        {
            Failed++;
        }

        public void Reset()
        {
            Failed = Succeeded = 0;
        }

        public override string ToString()
        {
            return $"Total: {Total}, Succeeded: {Succeeded}, Failed: {Failed}";
        }
    }
}