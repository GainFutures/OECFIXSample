using OEC.FIX.Sample.FoxScript;

namespace OEC.FIX.Sample.CFI
{
    internal class Code : BaseCode
    {
        public static readonly string Futures = CreateFutures().ToFix();
        public static readonly string FutureOptionsPut = CreateFutureOptions(true, CFI.Futures.TermLevel.Standard).ToFix();
        public static readonly string FutureOptionsCall = CreateFutureOptions(false, CFI.Futures.TermLevel.Standard).ToFix();
        public static readonly string Forex = CreateForex().ToFix();
        public static readonly string FuturesMultileg = CreateFuturesMultileg().ToFix();
        public static readonly string FutureOptionsMultileg = CreateFutureOptionsMultileg().ToFix();

        public Code(BaseCode code)
            : base(code.Category, code.Group, code.Scheme, code.UnderlyingAsset, code.Delivery, code.TermLevel)
        {
        }

        public Code(string value)
            : base(value)
        {
        }

        protected Code(char category, char group, char scheme, char underlyingAsset, char delivery, char termLevel)
            : base(category, group, scheme, underlyingAsset, delivery, termLevel)
        {
        }

        public static Code Create(ContractKind kind, OptionType optionType)
        {
            BaseCode baseCode = null;
            switch (kind)
            {
                case ContractKind.FUTURE:
                    baseCode = CreateFutures();
                    break;
                case ContractKind.OPTION:
                    baseCode = CreateFutureOptions(optionType);
                    break;
                case ContractKind.FOREX:
                    baseCode = CreateForex();
                    break;
                case ContractKind.FUTURE_COMPOUND:
                    baseCode = CreateFuturesMultileg();
                    break;
                case ContractKind.OPTIONS_COMPOUND:
                    baseCode = CreateFutureOptionsMultileg();
                    break;
            }

            return baseCode == null ? null : new Code(baseCode);
        }

        public static Code CreateFutureOptions(OptionType optionType)
        {
            return new Code(
                CFI.Category.Options,
                OptionTypeToOptionGroup(optionType),
                Options.Scheme.Unknown,
                Options.UnderlyingAsset.Futures,
                Options.Delivery.Unknown,
                Options.TermLevel.Standard);
        }

        public static Code CreateEquityOptions(OptionType optionType)
        {
            return new Code(
                CFI.Category.Options,
                OptionTypeToOptionGroup(optionType),
                Options.Scheme.Unknown,
                Options.UnderlyingAsset.StockEquities,
                Options.Delivery.Unknown,
                Options.TermLevel.Standard);
        }

        private static char OptionTypeToOptionGroup(OptionType optionType)
        {
            switch (optionType)
            {
                case OptionType.PUT:
                    return Options.Group.Put;
                case OptionType.CALL:
                    return Options.Group.Call;
            }
            return Options.Group.Unknown;
        }
    }
}