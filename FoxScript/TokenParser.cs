using System;
using System.Linq;

namespace OEC.FIX.Sample.FoxScript
{
	internal static class TokenParser
	{
		public static char ParseTrailingTriggerType(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				throw new SyntaxErrorException("TrailingTriggerType not specified.");
			}
			token = token.ToUpperInvariant();
			return token.First();
		}

		public static OrderSymbol ParseOrderSymbol(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				throw new SyntaxErrorException("OrderSymbol not specified.");
			}
			token = token.ToUpperInvariant();
			string assetPrefix = ContractAssetPrefix.ExtractFrom(ref token);

			var result = new OrderSymbol {Asset = ContractAsset.Future, Multileg = false};
			if (token.Length < 3)
			{
				result.Name = token;
			}
			else
			{
				char year = token[token.Length - 1];
				char month = token[token.Length - 2];
				int n = Tools.ContractMonths.IndexOf(month);

				if (token.Contains(',') && token.Contains('+') && token.Contains('-'))
				{
					result.Name = token;
					result.Multileg = true;
					result.Asset = ContractAsset.Future;

					if (n < 0 || !char.IsDigit(year))
					{
						result.Option = true;
					}
				}
				else
				{
					if (n < 0 || !char.IsDigit(year))
					{
						result.Name = token;
					}
					else
					{
						result.Name = token.Substring(0, token.Length - 2);
						result.MonthYear = Tools.CreateMonthYear(n + 1, DateTime.Now.Year/10*10 + int.Parse(year.ToString()));
						result.Asset = ContractAsset.Future;
					}
				}
			}

			if (assetPrefix != null)
			{
				result.Asset = ContractAssetPrefix.ToAsset(assetPrefix);
			}

			return result;
		}
	}
}