using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OEC.FIX.Sample.FoxScript
{
	internal static class LiteralParser
	{
		private static readonly Regex NowTimestampRegex = new Regex(@"(NOW|UTCNOW)((\+|\-)(\d\d:\d\d(:\d\d)?))?");

		public static object ParseNull(string literal)
		{
			return null;
		}

		public static bool ParseBool(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Bool not specified.");
			}
			bool value;
			if (bool.TryParse(literal, out value))
			{
				return value;
			}
			throw new SyntaxErrorException("Invalid Bool '{0}'", literal);
		}

		public static double ParseFloat(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Float not specified.");
			}

			var nf = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
			nf.NumberDecimalSeparator = ".";
			double value;
			if (double.TryParse(literal, NumberStyles.Any, nf, out value))
			{
				return value;
			}
			throw new SyntaxErrorException("Invalid Float '{0}'", literal);
		}

		public static int ParseInteger(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Integer not specified.");
			}
			int value;
			if (int.TryParse(literal, out value))
			{
				return value;
			}
			throw new SyntaxErrorException("Invalid Integer '{0}'", literal);
		}

		public static string ParseString(string literal)
		{
			if (literal == null)
			{
				throw new SyntaxErrorException("String not specified.");
			}
			return literal.Replace("'", string.Empty);
		}

		public static DateTime ParseDate(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Date not specified.");
			}
			literal = literal.Replace("[", string.Empty).Replace("]", string.Empty);

			DateTime value;
			if (DateTime.TryParseExact(literal, "yyyyMMdd", null, DateTimeStyles.None, out value))
			{
				return new DateTime(value.Date.Ticks, DateTimeKind.Unspecified);
			}

			throw new SyntaxErrorException("Invalid Date '{0}'", literal);
		}

		public static DateTime ParseTimestamp(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Timestamp not specified.");
			}
			literal = literal
				.Replace("[", string.Empty)
				.Replace("]", string.Empty)
				.ToUpperInvariant();

			Match res = NowTimestampRegex.Match(literal);
			if (res.Success)
			{
				DateTime value = res.Groups[1].Value == "UTCNOW" ? DateTime.UtcNow : DateTime.Now;
				if (res.Groups[4].Value.Length > 0)
				{
					TimeSpan offset = ParseTimespan(res.Groups[4].Value);
					if (res.Groups[3].Value == "+")
					{
						value += offset;
					}
					else
					{
						value -= offset;
					}
				}
				return value;
			}

			bool utc = literal.EndsWith("UTC");
			literal = literal.Replace("UTC", string.Empty);

			string[] formats = {"yyyyMMdd-HH:mm", "yyyyMMdd-HH:mm:ss"};
			foreach (string format in formats)
			{
				DateTime value;
				if (DateTime.TryParseExact(literal, format, null, DateTimeStyles.None, out value))
				{
					return new DateTime(value.Ticks, utc ? DateTimeKind.Utc : DateTimeKind.Local);
				}
			}

			throw new SyntaxErrorException("Invalid Timestamp '{0}'", literal);
		}

		public static TimeSpan ParseTimespan(string literal)
		{
			if (string.IsNullOrEmpty(literal))
			{
				throw new SyntaxErrorException("Timespan not specified.");
			}
			literal = literal.Replace("[", string.Empty).Replace("]", string.Empty);

			TimeSpan value;
			if (TimeSpan.TryParse(literal, out value))
			{
				return value;
			}

			throw new SyntaxErrorException("Invalid Timespan '{0}'", literal);
		}
	}
}