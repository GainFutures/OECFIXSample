using OEC.FIX;
using OEC.FIX.Sample.FIX.Fields;
using QuickFix;
using Message = QuickFix44.Message;

namespace OEC.FIX.Sample.FIX
{
	public class MarginCalcRequest : Message
	{
		public const string MsgType = "UR";

		public MarginCalcRequest()
			: base(new MsgType(MsgType))
		{
		}

		public MarginCalcRequest(MarginCalcReqID reqID, Account account) : this()
		{
			setField(reqID);
			setField(account);
		}

		public class NoPositions : Group
		{
			private static readonly int[] message_order =
			{
				Symbol.FIELD, CFICode.FIELD, QuickFix.MaturityMonthYear.FIELD,
				StrikePrice.FIELD, MinQty.FIELD, MaxQty.FIELD, 0
			};

			public NoPositions() : base(QuickFix.NoPositions.FIELD, Symbol.FIELD, message_order)
			{
			}
		}
	}

	public sealed class MarginCalcReportMessage : Message
	{
		private const string MsgType = "UM";

		public MarginCalcReportMessage(MarginCalcReqID reqID, Account account, MarginCalcReqResult marginCalcReqResult)
			: base(new MsgType(MsgType))
		{
			setField(reqID);
			setField(account);
			setField(marginCalcReqResult);
			if (marginCalcReqResult.getValue() != 0)
				setField(new Text(((MarginCalcReqResult.Enum) marginCalcReqResult.getValue()).ToString()));
		}

		internal void setMarginValue(int field, double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				return;
			setField(field, value.ToString("f2"));
		}
	}

	public sealed class MarginCalcReqID : StringField
	{
		public const int FIELD = 12064;

		public MarginCalcReqID(string value)
			: base(FIELD, value)
		{
		}

		public MarginCalcReqID()
			: this(string.Empty)
		{
		}
	}

	public sealed class InitialMargin : DoubleField
	{
		public const int FIELD = 12067;

		public InitialMargin(double value)
			: base(FIELD, value)
		{
		}

		public InitialMargin()
			: this(0)
		{
		}
	}

	public sealed class MaintenanceMargin : DoubleField
	{
		public const int FIELD = 12068;

		public MaintenanceMargin(double value)
			: base(FIELD, value)
		{
		}

		public MaintenanceMargin()
			: this(0)
		{
		}
	}

	public sealed class NetOptionValue : DoubleField
	{
		public const int FIELD = 12069;

		public NetOptionValue(double value)
			: base(FIELD, value)
		{
		}

		public NetOptionValue()
			: this(0)
		{
		}
	}

	public sealed class RiskValue : DoubleField
	{
		public const int FIELD = 12070;

		public RiskValue(double value)
			: base(FIELD, value)
		{
		}

		public RiskValue()
			: this(0)
		{
		}
	}

	public sealed class MarginCalcReqResult : IntField
	{
		public enum Enum
		{
			Success,
			UnknownSession,
			UnknownAccount,
			EmptyRequest,
			InvalidContract,
			CalculationError,
			RequestExceedsLimit,
			MarginCalculatorDisabled
		}

		public const int FIELD = 12065;

		public MarginCalcReqResult(Enum value)
			: base(FIELD, (int) value)
		{
		}

		public MarginCalcReqResult()
			: this(0)
		{
		}
	}
}