using OEC.FIX.Sample.FAST;
using OEC.FIX.Sample.FIX;
using QuickFix;

namespace OEC.FIX.Sample
{
	internal class MessageWrapper
	{
		private MessageWrapper(Message message)
		{
			QFMessage = message;
		}

		private MessageWrapper(OpenFAST.Message message)
		{
			OFMessage = message;
		}

        public readonly Message QFMessage;

        public readonly OpenFAST.Message OFMessage;

		public bool IsQuickFix
		{
			get { return QFMessage != null; }
		}

		public static MessageWrapper Create(Message message)
		{
			if (message == null)
				return null;
			return new MessageWrapper(message);
		}

		public static MessageWrapper Create(OpenFAST.Message message)
		{
			if (message == null)
				return null;
			return new MessageWrapper(message);
		}

		public static void SetFieldValue(MessageWrapper message, string fieldName, object fieldValue)
		{
			if (message != null)
				if (message.QFMessage != null)
				{
					QFReflector.SetFieldValue(message.QFMessage, fieldName, fieldValue);
				}
				else
				{
					OFReflector.SetFieldValue(message.OFMessage, fieldName, fieldValue);
				}
		}

		public static string FormatMessage(MessageWrapper message)
		{
			if (message != null)
				if (message.QFMessage != null)
				{
					return QFReflector.FormatMessage(message.QFMessage);
				}
				else
				{
					return message.OFMessage.ToString();
				}
			return "NULL";
		}

		public static object GetFieldValue(MessageWrapper message, string fieldName)
		{
			if (message != null)
				if (message.QFMessage != null)
				{
					return QFReflector.GetFieldValue(message.QFMessage, fieldName);
				}
				else
				{
					return OFReflector.GetFieldValue(message.OFMessage, fieldName);
				}
			throw new ExecutionException("FIX message not specified.");
		}
	}
}