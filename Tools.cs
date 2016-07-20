using System;
using System.Collections.Generic;
using System.Threading;
using OEC.FIX.Sample.FoxScript;
using QuickFix;

namespace OEC.FIX.Sample
{
	internal static class Tools
	{
		public static readonly HashSet<int> TrailerFields = new HashSet<int>
		{
			SignatureLength.FIELD,
			Signature.FIELD,
			CheckSum.FIELD,
		};

		public static readonly HashSet<int> HeaderFields = new HashSet<int>
		{
			BeginString.FIELD,
			BodyLength.FIELD,
			MsgType.FIELD,
			SenderCompID.FIELD,
			TargetCompID.FIELD,
			OnBehalfOfCompID.FIELD,
			DeliverToCompID.FIELD,
			SecureDataLen.FIELD,
			SecureData.FIELD,
			MsgSeqNum.FIELD,
			SenderSubID.FIELD,
			SenderLocationID.FIELD,
			TargetSubID.FIELD,
			TargetLocationID.FIELD,
			OnBehalfOfSubID.FIELD,
			OnBehalfOfLocationID.FIELD,
			DeliverToSubID.FIELD,
			DeliverToLocationID.FIELD,
			PossDupFlag.FIELD,
			PossResend.FIELD,
			SendingTime.FIELD,
			OrigSendingTime.FIELD,
			XmlDataLen.FIELD,
			XmlData.FIELD,
			MessageEncoding.FIELD,
			LastMsgSeqNumProcessed.FIELD,
			NoHops.FIELD,
			HopCompID.FIELD,
			HopSendingTime.FIELD,
			HopRefID.FIELD
		};

		public static readonly string[] Months =
		{
			"JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV",
			"DEC"
		};

		public static readonly string ContractMonths = "FGHJKMNQUVXZ";

		public static DateTime CreateMonthYear(int month, int year)
		{
			return new DateTime(year, month, 1);
		}

		public static string FormatMonthYear(DateTime date)
		{
			return date.ToString("yyyyMM");
		}

		public static string FormatLocalMktDate(DateTime date)
		{
			return date.ToString("yyyyMMdd");
		}

		public static string GenerateUniqueID()
		{
			Thread.Sleep(100);
			return DateTime.UtcNow.Ticks.ToString();
		}

		public static string GenerateUniqueTimestamp()
		{
			Thread.Sleep(100);
			return DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff");
		}

		public static TimeSpan LocalToUtc(TimeSpan local)
		{
			return DateTime.Now.Date.Add(local).ToUniversalTime().TimeOfDay;
		}

		public static string FormatProp(Prop prop)
		{
			return string.Format("	{0} : {1} = {2}", prop.Name, prop.Type.Name, FormatPropValue(prop));
		}

		public static string FormatPropValue(Prop prop)
		{
			try
			{
				object value = prop.Value;
				if (value == null)
				{
					return "NULL";
				}

				if (prop.Type == typeof (string))
				{
					return "'" + value + "'";
				}
				return value.ToString();
			}
			catch (Exception e)
			{
				return "[" + (e.Message) + "]";
			}
		}

		public static IEnumerable<T> Empty<T>()
		{
			yield break;
		}

		public static IEnumerable<T> Single<T>(T item)
		{
			yield return item;
		}
	}
}