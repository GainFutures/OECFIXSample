namespace OEC.FIX.Sample.CFI
{
    public static class Equities
    {
        public static class Delivery
        {
            public static readonly char Unknown = 'X';
            public static readonly char Forward = 'F';
        }

        public static class Group
        {
            public static readonly char Forex = 'R';
            public static readonly char Unknown = 'X';
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
        }
    }
}