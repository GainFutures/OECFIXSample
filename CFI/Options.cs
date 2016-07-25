namespace OEC.FIX.Sample.CFI
{
    public static class Options
    {
        public static class Delivery
        {
            public static readonly char Unknown = 'X';
        }

        public static class Group
        {
            public static readonly char Unknown = 'X';
            public static readonly char Call = 'C';
            public static readonly char Put = 'P';
            public static readonly char Other = 'M';
        }

        public static class Scheme
        {
            public static readonly char Unknown = 'X';
        }

        public static class TermLevel
        {
            public static readonly char Unknown = 'X';
            public static readonly char Standard = 'S';
            public static readonly char NonStandard = 'N';
        }

        public static class UnderlyingAsset
        {
            public static readonly char Unknown = 'X';
            public static readonly char StockEquities = 'S';
            public static readonly char Commodities = 'T';
            public static readonly char Currencies = 'C';
            public static readonly char Indices = 'I';
            public static readonly char Futures = 'F';
        }
    }
}