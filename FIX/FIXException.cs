using System;

namespace OEC.FIX.Sample.FIX
{
    public class FIXException : Exception
    {
        public FIXException()
        {
        }

        public FIXException(string message)
            : base(message)
        {
        }

        public FIXException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public int? OrdRejReason { get; set; }
        public int? CxlRejReason { get; set; }
    }
}