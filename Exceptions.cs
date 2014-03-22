using System;

namespace OEC.FIX.Sample
{
	public class SyntaxErrorException : Exception
	{
		public SyntaxErrorException()
		{
		}

		public SyntaxErrorException(string format, params object[] args)
			: base(string.Format(format, args))
		{
		}
	}

	public class ExecutionException : Exception
	{
		public ExecutionException()
		{
		}

		public ExecutionException(string format, params object[] args)
			: base(string.Format(format, args))
		{
		}
	}
}