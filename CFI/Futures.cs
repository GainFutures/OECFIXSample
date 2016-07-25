namespace OEC.FIX.Sample.CFI
{
    public static class Futures
    {
        public static class Delivery
        {
            public static readonly char Unknown = 'X';
        }

        public static class Group
        {
            public static readonly char Unknown = 'X';
            public static readonly char Financial = 'F';
            public static readonly char Commodity = 'C';
            public static readonly char Others = 'M';
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
            public static readonly char Agriculture = 'A';
            public static readonly char Basket = 'B';
            public static readonly char StocksOrServices = 'S';
            public static readonly char Indices = 'I';
        }
    }
}