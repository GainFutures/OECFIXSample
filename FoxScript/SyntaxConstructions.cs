using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using OEC.FIX.Sample.FoxScript.AllocationBlocks;

namespace OEC.FIX.Sample.FoxScript
{
	internal class OrderSide
	{
		public bool? Open;
		public char Side;
	}

	internal static class ContractAssetPrefix
	{
		public static readonly string Future = "FUT:";
		public static readonly string Equity = "EQ:";
		public static readonly string Forex = "FX:";
		public static readonly string MutualFund = "MF:";

		public static string ExtractFrom(ref string symbol)
		{
			var prefixes = new[] {Future, Equity, Forex, MutualFund};

			string s = symbol;
			string prefix = prefixes.FirstOrDefault(s.StartsWith);

			if (prefix != null)
			{
				symbol = symbol.Substring(prefix.Length);
				return prefix;
			}
			return null;
		}

		public static ContractAsset ToAsset(string prefix)
		{
			if (prefix == Future)
			{
				return ContractAsset.Future;
			}
			if (prefix == Forex)
			{
				return ContractAsset.Forex;
			}
			throw new ExecutionException("Invalid prefix '{0}'", prefix ?? "NULL");
		}

		public static string FromAsset(ContractAsset asset)
		{
			switch (asset)
			{
				case ContractAsset.Future:
					return Future;

				case ContractAsset.Forex:
					return Forex;

				default:
					throw new ExecutionException("Invalid ContractAsset.");
			}
		}
	}

	internal enum ContractAsset
	{
		Future,
		Forex
	}

	internal class OrderContract
	{
		public bool? Put;
		public double? Strike;
		public OrderSymbol Symbol;

		public bool Option
		{
			get { return Put.HasValue || Strike.HasValue || Symbol.Option; }
		}
	}

	internal class OrderSymbol
	{
		public ContractAsset Asset;
		public DateTime? MonthYear;
		public bool Multileg;
		public string Name;
		public bool Option;
	}

	internal class TrailingStop
	{
		public double? Amount;
		public bool AmountInPercents;
		public char? TriggerType;
	}

	internal class OrderType
	{
		public const char ICEBERG = '!';
		public const char MARKET_ON_OPEN = '@';
		public const char MARKET_ON_CLOSE = '#';

		public double? Limit;

		public int? MaxFloor;
		public double? Stop;
		public TrailingStop TrailingStop;
		public char Type;
	}

	internal class TimeInForce
	{
		public DateTime? Expiration;
		public char Type;
	}

	internal abstract class MsgCommand
	{
	}

	internal class FixFields : List<FixField>
	{
		public void Add(string name, Object value)
		{
			Add(new FixField(name, value));
		}
	}

	internal abstract class OutgoingMsgCommand : MsgCommand
	{
		public readonly FixFields Fields = new FixFields();
	}

	internal abstract class IncomingMsgCommand : MsgCommand
	{
	}

	internal class WaitMessageCommand : IncomingMsgCommand
	{
		public object LogicalExpr;
		public string MsgTypeName;
		public TimeSpan? Timeout;
	}

	internal abstract class OrderCommand : OutgoingMsgCommand
	{
		public string Account;
		public AllocationBlock<PreAllocationBlockItem> AllocationBlock;
		public OrderContract OrderContract;
		public int OrderQty;
		public OrderSide OrderSide;
		public OrderType OrderType;
		public TimeInForce TimeInForce;
		public string TradingSession;
	}

	internal class NewOrderCommand : OrderCommand
	{
	}

	internal class OrderRefOutgoingMsgCommand : OutgoingMsgCommand
	{
		public string OrigMsgVarName;
	}

	internal class ModifyOrderCommand : OrderCommand
	{
		public string OrigMsgVarName;
	}

    class CancelOrderCommand : OrderRefOutgoingMsgCommand
	{
	}

	class OrderStatusCommand : OrderRefOutgoingMsgCommand
	{
	}

    class OrderMassStatusCommand : OrderCommand
	{
	}

	internal class PostAllocationCommand : OrderRefOutgoingMsgCommand
	{
		public AllocationBlock<PostAllocationBlockItem> AllocationBlock;
		public OrderContract Contract;
	}

	internal class BalanceCommand : OutgoingMsgCommand
	{
		public string Account;
	}

	internal class PositionsCommand : OutgoingMsgCommand
	{
		public string Account;
	}

	internal class MarginCalcCommand : OutgoingMsgCommand
	{
		public string Account;
		public List<Position> Positions;

		public class Position
		{
			public OrderContract Contract;
			public int MaxQty;
			public int MinQty;
		}
	}

	internal class UserRequestCommand : OutgoingMsgCommand
	{
		public string Name;
		public string UUID;
		public int UserRequestType = QuickFix.UserRequestType.REQUEST_INDIVIDUAL_USER_STATUS;
	}

	internal enum ContractRequestType
	{
		Request,
		Lookup
	}

	internal class ContractRequestCommand : OutgoingMsgCommand
	{
		public const char DEFAULT_SUBSCRIBTION_TYPE = '-';
		public string Name;
		public char SubscriptionRequestType = DEFAULT_SUBSCRIBTION_TYPE;
		public DateTime? UpdatesSinceTimestamp;
	}

	internal class BaseContractRequestCommand : ContractRequestCommand
	{
		public CompoundType CompoundType = CompoundType.UNKNOWN;
		public string ContractGroup;
		public string Exchange;

		public string CompoundTypeString
		{
			get
			{
				Type type = typeof (CompoundType);
				string fieldValue = CompoundType.ToString();
				if (Enum.IsDefined(type, CompoundType))
				{
					FieldInfo fieldInfo = type.GetField(fieldValue);
					var atts = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof (DescriptionAttribute), false);
					return atts.Length > 0 ? atts[0].Description : fieldValue;
				}
				return fieldValue;
			}
		}
	}

	internal class SymbolLookupCommand : BaseContractRequestCommand
	{
		public string BaseContract;
		public bool? ByBaseContractsOnly;
		public List<ContractKind> ContractKinds = new List<ContractKind>();

		public ContractType? ContractType;
		public int MaxRecords;
		public int Mode;
		public OptionType OptionType = OptionType.ALL;
		public bool? OptionsRequired;

		public Contract ParentContract;
		public string SearchText;
	}

	internal class FixField
	{
		public readonly string Name;
		public readonly Object Value;

		public FixField(string name, Object value)
		{
			Name = name;
			Value = value;
		}
	}

	internal class Object
	{
		public readonly string Token;
		public readonly ObjectType Type;

		public Object(ObjectType type, string token)
		{
			Type = type;
			Token = token;
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", Type, Token ?? "NULL");
		}
	}

	internal enum ObjectType
	{
		Null,
		Integer,
		Float,
		String,
		Bool,
		Timestamp,
		Date,
		Timespan,

		FixMsgVar,
		FixMsgVarField,
		FixField,
		FixConst,
		GlobalProp
	}

	internal class FormatArgs
	{
		public readonly string Format;
		private readonly List<object> _args = new List<object>();

		public FormatArgs(string format)
		{
			Format = format;
		}

		public IEnumerable<object> Args
		{
			get { return _args; }
		}

		public void AddArg(object arg)
		{
			if (arg == null)
			{
				return;
			}
			_args.Add(arg);
		}
	}

	internal enum LogicalOp
	{
		Or,
		And,
		Equal,
		NotEqual,
		Less,
		LessOrEqual,
		Greater,
		GreaterOrEqual
	}

	internal class LogicalExpr
	{
		public object Left;
		public LogicalOp Operation;
		public object Right;
	}

	#region FAST

	internal class FASTStrikeSide
	{
		public bool Put;
		public double Strike;
	}


	internal abstract class MDMessageCommand : OutgoingMsgCommand, IOutputFile
	{
		public string BaseSymbol;
		public int ExpirationMonth;
        public ContractKind ContractKind;

		public int UpdateType = 1;

        public string OutputFileName
        {
            get;
            set;
        }

        public FASTStrikeSide StrikeSide
        {
            get;
            set;
        }

		public bool Option
		{
			get { return StrikeSide != null; }
		}

        public abstract int MarketDepth
        {
            get;
        }

        public abstract int SubscriptionType
        {
            get;
        }

		public virtual int SubscriptionRequestType
		{
			get
			{
				return 1;
			}
		}

        public abstract string[] MDEntries
        {
            get;
        }

        public abstract DateTime? StartTime
        {
            get;
        }

		public virtual DateTime? EndTime
		{
			get
			{
				return null;
			}
		}
	}

	internal class SubscribeQuotesCommand : MDMessageCommand
	{
		public override int MarketDepth
		{
			get { return 1; }
		}


		public override int SubscriptionType
		{
			get { return 0; }
		}

		public override string[] MDEntries
		{
			get { return new[] {"0", "1", "2", "4", "6", "7", "8", "B", "C"}; }
		}

		public override DateTime? StartTime
		{
			get { return null; }
		}
	}


	internal class SubscribeDOMCommand : MDMessageCommand
	{
		public override int MarketDepth
		{
			get { return 0; }
		}

		public override int SubscriptionType
		{
			get { return 1; }
		}

		public override string[] MDEntries
		{
			get { return new[] {"0", "1"}; }
		}

		public override DateTime? StartTime
		{
			get { return null; }
		}
	}

	internal class SubscribeHistogramCommand : MDMessageCommand
	{
		public override int MarketDepth
		{
			get { return 0; }
		}

		public override int SubscriptionType
		{
			get { return 4; }
		}

		public override string[] MDEntries
		{
			get { return new[] {"2"}; }
		}

		public override DateTime? StartTime
		{
			get { return null; }
		}
	}

	internal class SubscribeTicksCommand : MDMessageCommand
	{
		private DateTime? _startTime;
		private TimeSpan? _startTimeSpan;

		public override int MarketDepth
		{
			get { return 1; }
		}

		public override int SubscriptionType
		{
			get { return 2; }
		}

		public override string[] MDEntries
		{
			get { return new[] {"0", "1", "2"}; }
		}

		public override DateTime? StartTime
		{
			get
			{
				if (_startTime.HasValue)
				{
					return _startTime;
				}
				DateTime now = DateTime.UtcNow;
				if (_startTimeSpan.HasValue)
				{
					return now - _startTimeSpan.Value;
				}
				return now;
			}
		}

		public void SetStartTime(DateTime startTime)
		{
			_startTime = startTime;
		}

		public void SetStartTime(TimeSpan startTimeSpan)
		{
			_startTimeSpan = startTimeSpan;
		}
	}

	internal class CancelSubscribeCommand : OutgoingMsgCommand
	{
		public string MDMessageVar;
	}

	class LoadTicksCommand : SubscribeTicksCommand
	{
		private DateTime? _EndTime = null;
		private TimeSpan? _EndTimeSpan = null;

		public override int SubscriptionRequestType
		{
			get { return 0; }
		}

		public override DateTime? EndTime
		{
			get
			{
				if (_EndTime.HasValue)
				{
					return _EndTime;
				}
				else
				{
					DateTime now = DateTime.UtcNow;
					if (_EndTimeSpan.HasValue)
					{
						return now - _EndTimeSpan.Value;
					}
					else
						return now;
				}
			}
		}

		public void SetEndTime(DateTime endTime)
		{
			_EndTime = endTime;
		}

		public void SetEndTime(TimeSpan endTimeSpan)
		{
			_EndTimeSpan = endTimeSpan;
		}
	}

    interface IOutputFile
	{
        string OutputFileName
        {
            get;
            set;
        }
	}

	#endregion
}