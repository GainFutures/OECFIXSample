using System.ComponentModel;

namespace OEC.FIX.Sample.FoxScript
{
    internal enum ContractKind
    {
        UNKNOWN,
        FUTURE = 1,
        OPTION = 3,
        FOREX = 5,
        CONTINUOUS = 11,
        GENERIC_COMPOUND = 256,
        FUTURE_COMPOUND = 257,
        OPTIONS_COMPOUND = 258,
        CUSTOM_COMPOUND = 259
    }

    internal enum ContractType
    {
        ELECTRONIC,
        PIT
    }

    internal enum OptionType
    {
        ALL,
        PUT,
        CALL
    }

    internal enum CompoundType
    {
        [Description("Unknown")]
        UNKNOWN = 0,
        [Description("Generic")]
        GENERIC = 1,
        [Description("PerformanceIndexBasket")]
        PERFORMANCE_INDEX_BASKET = 2,
        [Description("NonPerformanceIndexBasket")]
        NON_PERFORMANCE_INDEX_BASKET = 3,
        [Description("Straddle")]
        STRADDLE = 4,
        [Description("Strangle")]
        STRANGLE = 5,
        [Description("FutureTimeSpread")]
        FUTURE_TIME_SPREAD = 6,
        [Description("OptionTimeSpread")]
        OPTION_TIME_SPREAD = 7,
        [Description("PriceSpread")]
        PRICE_SPREAD = 8,
        [Description("SyntheticUnderlying")]
        SYNTHETIC_UNDERLYING = 9,
        [Description("StraddleTimeSpread")]
        STRADDLE_TIME_SPREAD = 10,
        [Description("RatioSpread")]
        RATIO_SPREAD = 11,
        [Description("RatioFutureTimeSpread")]
        RATIO_FUTURE_TIME_SPREAD = 12,
        [Description("RatioOptionTimeSpread")]
        RATIO_OPTION_TIME_SPREAD = 13,
        [Description("PutCallSpread")]
        PUT_CALL_SPREAD = 14,
        [Description("RatioPutCallSpread")]
        RATIO_PUT_CALL_SPREAD = 15,
        [Description("Ladder")]
        LADDER = 16,
        [Description("Box")]
        BOX = 17,
        [Description("Butterfly")]
        BUTTERFLY = 18,
        [Description("Condor")]
        CONDOR = 19,
        [Description("IronButterfly")]
        IRON_BUTTERFLY = 20,
        [Description("DiagonalSpread")]
        DIAGONAL_SPREAD = 21,
        [Description("RatioDiagonalSpread")]
        RATIO_DIAGONAL_SPREAD = 22,
        [Description("StraddleDiagonalSpread")]
        STRADDLE_DIAGONAL_SPREAD = 23,
        [Description("ConversionReversal")]
        CONVERSION_REVERSAL = 24,
        [Description("CoveredOption")]
        COVERED_OPTION = 25,
        [Description("reserved1")]
        RESERVED1 = 26,
        [Description("reserved2")]
        RESERVED2 = 27,
        [Description("CurrencyFutureSpread")]
        CURRENCY_FUTURE_SPREAD = 28,
        [Description("RateFutureSpread")]
        RATE_FUTURE_SPREAD = 29,
        [Description("IndexFutureSpread")]
        INDEX_FUTURE_SPREAD = 30,
        [Description("FutureButterfly")]
        FUTURE_BUTTERFLY = 31,
        [Description("FutureCondor")]
        FUTURE_CONDOR = 32,
        [Description("Strip")]
        STRIP = 33,
        [Description("Pack")]
        PACK = 34,
        [Description("Bundle")]
        BUNDLE = 35,
        [Description("BondDeliverableBasket")]
        BOND_DELIVERABLE_BASKET = 36,
        [Description("StockBasket")]
        STOCK_BASKET = 37,
        [Description("PriceSpreadVsOption")]
        PRICE_SPREAD_VS_OPTION = 38,
        [Description("StraddleVsOption")]
        STRADDLE_VS_OPTION = 39,
        [Description("BondSpread")]
        BOND_SPREAD = 40,
        [Description("ExchangeSpread")]
        EXCHANGE_SPREAD = 41,
        [Description("FuturePackSpread")]
        FUTURE_PACK_SPREAD = 42,
        [Description("FuturePackButterfly")]
        FUTURE_PACK_BUTTERFLY = 43,
        [Description("WholeSale")]
        WHOLE_SALE = 44,
        [Description("CommoditySpread")]
        COMMODITY_SPREAD = 45,
        [Description("JellyRoll")]
        JELLY_ROLL = 46,
        [Description("IronCondor")]
        IRON_CONDOR = 47,
        [Description("OptionsStrip")]
        OPTIONS_STRIP = 48,
        [Description("ContingentOrders")]
        CONTINGENT_ORDERS = 49,
        [Description("InterproductSpread")]
        INTERPRODUCT_SPREAD = 50,
        [Description("PseudoStraddle")]
        PSEUDO_STRADDLE = 51,
        [Description("TailorMade")]
        TAILOR_MADE = 52,
        [Description("FuturesGeneric")]
        FUTURES_GENERIC = 53,
        [Description("OptionsGeneric")]
        OPTIONS_GENERIC = 54,
        [Description("BasisTrade")]
        BASIS_TRADE = 55,
        [Description("FutureTimeSpreadReducedTickSize")]
        FUTURETIME_SPREAD_REDUCED_TICK_SIZE = 56,
        [Description("GenericVolaStrategyVS")]
        GENERIC_VOLA_STRATEGY_VS = 10001,
        [Description("StraddleVolaStrategyVS")]
        STRADDLE_VOLA_STRATEGY_VS = 10004,
        [Description("StrangleVS")]
        STRANGLE_VS = 10005,
        [Description("OptionTimeSpreadVS")]
        OPTION_TIME_SPREAD_VS = 10007,
        [Description("PriceSpreadVS")]
        PRICE_SPREAD_VS = 10008,
        [Description("RatioSpreadVS")]
        RATIO_SPREAD_VS = 10011,
        [Description("PutCallSpreadVS")]
        PUT_CALL_SPREADVS = 10014,
        [Description("LadderVS")]
        LADDER_VS = 10016,
        [Description("PriceSpreadVsOptionVS")]
        PRICE_SPREAD_VS_OPTION_VS = 10038,
        [Description("Collar")]
        COLLAR = 65536,
        [Description("Combo")]
        COMBO = 65537,
        [Description("ProtectivePut")]
        PROTECTIVE_PUT = 65538,
        [Description("Spread")]
        SPREAD = 65539
    }

    internal enum AllocationRule
    {
        /// <summary>
        ///     lowest price- lowest acc
        /// </summary>
        LowAcctLowPrice = 2,

        /// <summary>
        ///     lowest price-highest acc
        /// </summary>
        LowAcctHighPrice,

        /// <summary>
        ///     highest price- lowest acc
        /// </summary>
        HighAcctLowPrice,

        /// <summary>
        ///     highest price-highest acc
        /// </summary>
        HighAcctHighPrice,

        /// <summary>
        ///     Average price system
        /// </summary>
        APS,
        PostAllocation = 1000,
        PostAllocationAPS
    }
}