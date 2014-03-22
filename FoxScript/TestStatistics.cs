namespace OEC.FIX.Sample.FoxScript
{
	internal class TestStatistics
	{
		public int Total
		{
			get { return Succeeded + Failed; }
		}

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
			return string.Format("Total: {0}, Succeeded: {1}, Failed: {2}", Total, Succeeded, Failed);
		}
	}
}