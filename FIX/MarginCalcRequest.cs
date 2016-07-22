using System;
using OEC.FIX;
using OEC.FIX.Sample.FIX.Fields;
using QuickFix;
using QuickFix.Fields;
using Message = QuickFix.FIX44.Message;

namespace OEC.FIX.Sample.FIX
{
	public class MarginCalcRequest : Message
	{
		public const string MsgType = "UR";

		public MarginCalcRequest()
		{
		    Header.SetField(new MsgType(MsgType));
		}

		public MarginCalcRequest(MarginCalcReqID reqID, Account account) : this()
		{
			SetField(reqID);
			SetField(account);
		}

		public class NoPositions : Group
		{
			private static readonly int[] message_order =
			{
				Tags.Symbol, Tags.CFICode, Tags.MaturityMonthYear, Tags.StrikePrice, Tags.MinQty, MaxQty.FIELD, 0
			};

			public NoPositions() : base(Tags.NoPositions, Tags.Symbol, message_order)
			{
			}
		}
	}

	public sealed class MarginCalcReportMessage : Message
	{
		private const string MsgType = "UM";

		public MarginCalcReportMessage(MarginCalcReqID reqID, Account account, MarginCalcReqResult marginCalcReqResult)
		{
		    Header.SetField(new MsgType(MsgType));
			SetField(reqID);
			SetField(account);
			SetField(marginCalcReqResult);
			if (marginCalcReqResult.getValue() != 0)
				SetField(new Text(((MarginCalcReqResult.Enum) marginCalcReqResult.getValue()).ToString()));
		}

		internal void setMarginValue(int field, double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				return;

			SetField(new DecimalField(field, Convert.ToDecimal(value)));
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

	public sealed class InitialMargin : DecimalField
	{
		public const int FIELD = 12067;

		public InitialMargin(double value)
			: base(FIELD, Convert.ToDecimal(value))
		{
		}

		public InitialMargin()
			: this(0)
		{
		}
	}

	public sealed class MaintenanceMargin : DecimalField
	{
		public const int FIELD = 12068;

		public MaintenanceMargin(double value)
			: base(FIELD, Convert.ToDecimal(value))
		{
		}

		public MaintenanceMargin()
			: this(0)
		{
		}
	}

	public sealed class NetOptionValue : DecimalField
	{
		public const int FIELD = 12069;

		public NetOptionValue(double value)
			: base(FIELD, Convert.ToDecimal(value))
		{
		}

		public NetOptionValue()
			: this(0)
		{
		}
	}

	public sealed class RiskValue : DecimalField
	{
		public const int FIELD = 12070;

		public RiskValue(double value)
			: base(FIELD, Convert.ToDecimal(value))
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