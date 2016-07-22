using System;
using System.Collections.Generic;
using System.Threading;
using OEC.FIX.Sample.FoxScript;
using QuickFix;
using QuickFix.Fields;

namespace OEC.FIX.Sample
{
	internal static class Tools
	{
		public static readonly HashSet<int> TrailerFields = new HashSet<int>
		{
			Tags.SignatureLength,
            Tags.Signature,
            Tags.CheckSum,
		};

		public static readonly HashSet<int> HeaderFields = new HashSet<int>
		{
            Tags.BeginString,
            Tags.BodyLength,
            Tags.MsgType,
            Tags.SenderCompID,
            Tags.TargetCompID,
            Tags.OnBehalfOfCompID,
            Tags.DeliverToCompID,
            Tags.SecureDataLen,
            Tags.SecureData,
            Tags.MsgSeqNum,
            Tags.SenderSubID,
            Tags.SenderLocationID,
            Tags.TargetSubID,
            Tags.TargetLocationID,
            Tags.OnBehalfOfSubID,
            Tags.OnBehalfOfLocationID,
            Tags.DeliverToSubID,
            Tags.DeliverToLocationID,
            Tags.PossDupFlag,
            Tags.PossResend,
            Tags.SendingTime,
            Tags.OrigSendingTime,
            Tags.XmlDataLen,
            Tags.XmlData,
            Tags.MessageEncoding,
            Tags.LastMsgSeqNumProcessed,
            Tags.NoHops,
            Tags.HopCompID,
            Tags.HopSendingTime,
            Tags.HopRefID
        };

		public static readonly string[] Months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

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