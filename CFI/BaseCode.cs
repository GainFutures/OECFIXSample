using System;

namespace OEC.FIX.Sample.CFI
{
	public class BaseCode
	{
		protected BaseCode(char category, char group, char scheme, char underlyingAsset, char delivery, char termLevel)
		{
			Category = category;
			Group = group;
			Scheme = scheme;
			UnderlyingAsset = underlyingAsset;
			Delivery = delivery;
			TermLevel = termLevel;
		}

		/// <summary>
		///     Creates an instance from string like "OPXFXS"
		/// </summary>
		protected BaseCode(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}

			value = value.ToUpperInvariant();
			if (value.Length == 6)
			{
				Category = value[0];
				Group = value[1];
				Scheme = value[2];
				UnderlyingAsset = value[3];
				Delivery = value[4];
				TermLevel = value[5];
			}
			else if (value.Length == 5)
			{
				Category = value[0];
				if (Category != CFI.Category.Futures)
					throw new ArgumentException("Invalid CFI format for Futures.");

				Group = value[1];
				Scheme = Futures.Scheme.Unknown;
				UnderlyingAsset = value[2];
				Delivery = value[3];
				TermLevel = value[4];
			}
			else
			{
				throw new ArgumentException("Invalid CFI format.");
			}
		}

		public char Category { get; private set; }
		public char Group { get; private set; }
		public char Scheme { get; private set; }
		public char UnderlyingAsset { get; private set; }
		public char Delivery { get; private set; }
		public char TermLevel { get; private set; }

		public bool IsMultileg
		{
			get { return IsFuturesMultileg || IsFutureOptionsMultileg; }
		}

		public bool IsFuturesMultileg
		{
			get
			{
				return
					Category == CFI.Category.Futures &&
					Group == Futures.Group.Others &&
					TermLevel == Futures.TermLevel.NonStandard;
			}
		}

		public bool IsFutureOptionsMultileg
		{
			get
			{
				return
					Category == CFI.Category.Options &&
					Group == Options.Group.Other &&
					UnderlyingAsset == Options.UnderlyingAsset.Futures &&
					TermLevel == Options.TermLevel.NonStandard;
			}
		}

		public bool IsFutures
		{
			get
			{
				return Category == CFI.Category.Futures &&
				       (TermLevel == Futures.TermLevel.Standard || TermLevel == Futures.TermLevel.Unknown);
			}
		}

		public bool IsGeneralOptions
		{
			get
			{
				return IsSimpleGeneralOptions
				       && (Group == Options.Group.Call || Group == Options.Group.Put);
			}
		}

		public bool IsFutureOptions
		{
			get
			{
				return IsGeneralOptions &&
				       (UnderlyingAsset == Options.UnderlyingAsset.Futures || UnderlyingAsset == Options.UnderlyingAsset.Unknown);
			}
		}

		public bool IsSimpleFutureOptions
		{
			get { return IsSimpleGeneralOptions && UnderlyingAsset == Options.UnderlyingAsset.Futures; }
		}

		public bool IsSimpleGeneralOptions
		{
			get
			{
				return
					Category == CFI.Category.Options &&
					(TermLevel == Options.TermLevel.Standard || TermLevel == Options.TermLevel.Unknown);
			}
		}

		public bool IsGeneralEquity
		{
			get { return Category == CFI.Category.Equities; }
		}

		public bool IsEquities
		{
			get
			{
				return
					Category == CFI.Category.Equities &&
					Group == Equities.Group.Unknown &&
					TermLevel == Equities.TermLevel.Standard;
			}
		}

		public bool IsForex
		{
			get
			{
				return
					Category == CFI.Category.Equities &&
					Group == Equities.Group.Forex &&
					Delivery == Equities.Delivery.Unknown &&
					TermLevel == Equities.TermLevel.NonStandard;
			}
		}

		public bool? IsOptionPut
		{
			get
			{
				if (Category == CFI.Category.Options)
				{
					return Group == Options.Group.Put;
				}
				return null;
			}
		}

		/// <summary>
		///     Returns "FXXXXS" code.
		/// </summary>
		public static BaseCode CreateFutures()
		{
			return new BaseCode(
				CFI.Category.Futures,
				Futures.Group.Unknown,
				Futures.Scheme.Unknown,
				Futures.UnderlyingAsset.Unknown,
				Futures.Delivery.Unknown,
				Futures.TermLevel.Standard);
		}


		/// <summary>
		///     Returns "ERXXXN" code.
		/// </summary>
		public static BaseCode CreateForex()
		{
			//	TODO:	Is Forex related to Equities category? 
			return new BaseCode(
				CFI.Category.Equities,
				Equities.Group.Forex,
				Equities.Scheme.Unknown,
				Equities.UnderlyingAsset.Unknown,
				Equities.Delivery.Unknown,
				Equities.TermLevel.NonStandard);
		}

		/// <summary>
		///     Returns "OPXFXS" or "OCXFXS" codes.
		/// </summary>
		public static BaseCode CreateFutureOptions(bool put, char termLevel)
		{
			return new BaseCode(
				CFI.Category.Options,
				put ? Options.Group.Put : Options.Group.Call,
				Options.Scheme.Unknown,
				Options.UnderlyingAsset.Futures,
				Options.Delivery.Unknown,
				termLevel);
		}

		/// <summary>
		///     Returns "FMXXXN" code.
		/// </summary>
		public static BaseCode CreateFuturesMultileg()
		{
			return new BaseCode(
				CFI.Category.Futures,
				Futures.Group.Others,
				Futures.Scheme.Unknown,
				Futures.UnderlyingAsset.Unknown,
				Futures.Delivery.Unknown,
				Futures.TermLevel.NonStandard);
		}

		/// <summary>
		///     Returns "OMXFXN" code.
		/// </summary>
		public static BaseCode CreateFutureOptionsMultileg()
		{
			return new BaseCode(
				CFI.Category.Options,
				Options.Group.Other,
				Options.Scheme.Unknown,
				Options.UnderlyingAsset.Futures,
				Options.Delivery.Unknown,
				Options.TermLevel.NonStandard);
		}

		public string ToFix()
		{
			return ToString();
		}

		public override string ToString()
		{
			return string.Format("{0}{1}{2}{3}{4}{5}", Category, Group, Scheme, UnderlyingAsset, Delivery, TermLevel);
		}
	}
}