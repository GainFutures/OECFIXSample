using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using OEC.FIX.Sample.FAST;
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
            var prefixes = new[] { Future, Equity, Forex, MutualFund };

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

    internal enum BracketType
    {
        OCO,
        OSO
    }

    internal class OrderContract
    {
        public bool? Put;
        public double? Strike;
        public OrderSymbol Symbol;

        public bool Option => Put.HasValue || Strike.HasValue || Symbol.Option;
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

    //VP: WTF
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

    abstract class OrderRequestCommand : OutgoingMsgCommand
    {
        public string Account;
        public OrderSide OrderSide;
        public OrderContract OrderContract;
        public AllocationBlock<PreAllocationBlockItem> AllocationBlock;
    }

    internal abstract class OrderCommand : OrderRequestCommand
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

    internal class BracketCommandItem : NewOrderCommand
    {
        public string MsgVarName;
    }

    internal class BracketOrderCommand : OutgoingMsgCommand
    {
        public BracketType Type;

        public List<BracketCommandItem> BracketCommands;
    }

    internal class OrderRefOutgoingMsgCommand : OutgoingMsgCommand
    {
        public string OrigMsgVarName;
    }

    internal class ModifyOrderCommand : OrderCommand
    {
        public string OrigMsgVarName;
    }

    internal class CancelOrderCommand : OrderRefOutgoingMsgCommand
    {
    }

    internal class OrderStatusCommand : OrderRefOutgoingMsgCommand
    {
    }

    class OrderMassStatusCommand : OrderRequestCommand
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
        public int UserRequestType = QuickFix.Fields.UserRequestType.REQUEST_INDIVIDUAL_USER_STATUS;
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
                Type type = typeof(CompoundType);
                string fieldValue = CompoundType.ToString();
                if (Enum.IsDefined(type, CompoundType))
                {
                    FieldInfo fieldInfo = type.GetField(fieldValue);
                    var atts = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
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
            return $"{Type}: {Token ?? "NULL"}";
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

        public IEnumerable<object> Args => _args;

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
        protected List<MDEntryType> Entries;

        public string BaseSymbol;
        public int? ExpirationMonth;

        public int UpdateType { get; set; }

        public ContractKind ContractKind;

        public FASTStrikeSide StrikeSide { get; set; }

        public bool Option => StrikeSide != null;

        public abstract int MarketDepth { get; }

        public abstract int SubscriptionType { get; }

        public virtual int SubscriptionRequestType => 1;

        public MDEntryType[] MDEntries => Entries.ToArray();

        public abstract DateTime? StartTime { get; }

        public virtual DateTime? EndTime => null;

        public string OutputFileName { get; set; }

        protected MDMessageCommand()
        {
            UpdateType = 1;
            Entries = new List<MDEntryType>();
        }

        public void Add(MDEntryType type)
        {
            Entries.Add(type);
        }

        public void ResetMDEntries()
        {
            Entries.Clear();
        }
    }

    internal class SubscribeQuotesCommand : MDMessageCommand
    {
        public override int MarketDepth => 1;

        public override int SubscriptionType => 0;

        public override DateTime? StartTime => null;

        public SubscribeQuotesCommand()
        {
            Entries = new List<MDEntryType>(
                new[]
                {
                    MDEntryType.BID,
                    MDEntryType.OFFER,
                    MDEntryType.TRADE,
                    MDEntryType.OPENING_PRICE,
                    MDEntryType.SETTLEMENT_PRICE,
                    MDEntryType.TRADE_VOLUME,
                    MDEntryType.OPEN_INTEREST
                }
            );
        }
    }


    internal class SubscribeDOMCommand : MDMessageCommand
    {
        public override int MarketDepth => 0;

        public override int SubscriptionType => 1;

        public override DateTime? StartTime => null;

        public SubscribeDOMCommand()
        {
            Entries = new List<MDEntryType>(new[] { MDEntryType.BID, MDEntryType.OFFER });
        }
    }

    internal class SubscribeHistogramCommand : MDMessageCommand
    {
        public override int MarketDepth => 0;

        public override int SubscriptionType => 4;

        public override DateTime? StartTime => null;

        public SubscribeHistogramCommand()
        {
            Entries = new List<MDEntryType>(new[] { MDEntryType.TRADE });
        }
    }

    internal class SubscribeTicksCommand : MDMessageCommand
    {
        private DateTime? _startTime;
        private TimeSpan? _startTimeSpan;

        public override int MarketDepth => 1;

        public override int SubscriptionType => 2;

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

        public SubscribeTicksCommand()
        {
            Entries = new List<MDEntryType>(new[] { MDEntryType.BID, MDEntryType.OFFER, MDEntryType.TRADE });
        }
    }

    internal class CancelSubscribeCommand : OutgoingMsgCommand
    {
        public string MDMessageVar;
    }

    internal class LoadTicksCommand : SubscribeTicksCommand
    {
        private DateTime? _endTime;
        private TimeSpan? _endTimeSpan;

        public override int SubscriptionRequestType => 0;

        public override DateTime? EndTime
        {
            get
            {
                if (_endTime.HasValue)
                    return _endTime;

                DateTime now = DateTime.UtcNow;
                if (_endTimeSpan.HasValue)
                    return now - _endTimeSpan.Value;
                return now;
            }
        }

        public void SetEndTime(DateTime endTime)
        {
            _endTime = endTime;
        }

        public void SetEndTime(TimeSpan endTimeSpan)
        {
            _endTimeSpan = endTimeSpan;
        }
    }

    internal interface IOutputFile
    {
        string OutputFileName { get; set; }
    }

    #endregion
}