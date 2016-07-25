using System;
using OEC.FIX.Sample.FoxScript;

namespace OEC.FIX.Sample
{
    internal enum ExpirationType
    {
        Standard = 0,
        Week1,
        Week2,
        Week3,
        Week4,
        Week5,
        WednesdayMidMonth,
        Quarterly
    }

    internal class MaturityMonthYear
    {
        /// <summary>
        ///     Creates an instance from string like "201009"
        /// </summary>
        public MaturityMonthYear(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            try
            {
                ExpirationType = ExpirationType.Standard;
                char specialFlag = char.ToUpper(value[value.Length - 2]);
                if (specialFlag == 'W')
                {
                    char lastDig = value[value.Length - 1];
                    int n = lastDig - '1';
                    if (0 <= n && n <= 4)
                    {
                        ExpirationType = (ExpirationType)((int)ExpirationType.Week1 + n);
                        value = value.Substring(0, 6);
                    }
                    else
                        throw new ArgumentException("Week should be 1..5");
                }
                specialFlag = char.ToUpper(value[value.Length - 1]);
                if (specialFlag == 'Q')
                {
                    ExpirationType = ExpirationType.Quarterly;
                    value = value.Substring(0, 6);
                }
                Year = int.Parse(value.Substring(0, 4));
                Month = int.Parse(value.Substring(4, 2));
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid string format, must be YYYYMM or YYYYMMw1 or YYYYMMq");
            }
            Verify();
        }

        public MaturityMonthYear(DateTime value)
            : this(value.Month, value.Year)
        {
        }

        public MaturityMonthYear(int month, int year)
        {
            Month = month;
            Year = year;
            Verify();
        }

        /// <summary>
        ///     Creates an instance from OEC ExpirationMonth value like 1009
        /// </summary>
        public MaturityMonthYear(int expirationMonth)
            : this(expirationMonth % 100, 2000 + expirationMonth / 100)
        {
        }

        /// <summary>
        ///     Gets maturity month [1..12].
        /// </summary>
        public int Month { get; }

        /// <summary>
        ///     Gets maturity year, including century like 2009.
        /// </summary>
        public int Year { get; }

        public ExpirationType ExpirationType { get; }

        public static MaturityMonthYear CreateOrNull(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return new MaturityMonthYear(value);
        }

        public static MaturityMonthYear CreateOrNull(ContractKind kind, int expirationMonth)
        {
            switch (kind)
            {
                case ContractKind.FUTURE:
                case ContractKind.OPTION:
                case ContractKind.GENERIC_COMPOUND:
                case ContractKind.FUTURE_COMPOUND:
                case ContractKind.OPTIONS_COMPOUND:
                    return new MaturityMonthYear(expirationMonth);
                default:
                    return null;
            }
        }

        public string ToFix()
        {
            return ToString();
        }

        public override string ToString()
        {
            string s = $"{Year:D4}{Month:D2}";
            if (ExpirationType == ExpirationType.Quarterly)
                s += "q";
            else if (ExpirationType.Week1 <= ExpirationType && ExpirationType <= ExpirationType.Week5)
                s += "w" + (ExpirationType - ExpirationType.Week1 + 1);
            return s;
        }

        public int ToExpirationMonth()
        {
            return (Year % 100) * 100 + Month;
        }

        private void Verify()
        {
            bool valid = (1 <= Month) && (Month <= 12) && (2000 <= Year) && (Year <= 9999);
            if (!valid)
            {
                throw new ArgumentException("Invalid month/year value.");
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MaturityMonthYear);
        }

        public override int GetHashCode()
        {
            return Month + Year * 100;
        }

        #region IEquatable<MaturityMonthYear> Members

        public bool Equals(MaturityMonthYear other)
        {
            return other != null && (Month == other.Month && Year == other.Year);
        }

        #endregion
    }
}