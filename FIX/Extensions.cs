using System;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public static class Extensions
	{
		public static TField CreateIfExists<TField>(this Message msg) where TField : Field, new()
		{
			var field = new TField();
			if (msg.isSetField(field))
				return field;
			return null;
		}

		public static TField GetString<TField>(this Message msg) where TField : StringField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static string Get<TField>(this Message msg, string defaultValue) where TField : StringField, new()
		{
			var field = msg.GetString<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetDouble<TField>(this Message msg) where TField : DoubleField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static TField GetChar<TField>(this Message msg) where TField : CharField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static char Get<TField>(this Message msg, char defaultValue) where TField : CharField, new()
		{
			var field = msg.GetChar<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetInt<TField>(this Message msg) where TField : IntField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static int Get<TField>(this Message msg, int defaultValue) where TField : IntField, new()
		{
			var field = msg.GetInt<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetBool<TField>(this Message msg) where TField : BooleanField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static bool Get<TField>(this Message msg, bool defaultValue) where TField : BooleanField, new()
		{
			var field = msg.GetBool<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetDateTime<TField>(this Message msg) where TField : UtcTimeStampField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.getField(field);
				return field;
			}
			return null;
		}

		public static DateTime Get<TField>(this Message msg, DateTime defaultValue) where TField : UtcTimeStampField, new()
		{
			var field = msg.GetDateTime<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TValue GetValue<TField, TValue>(this Message msg, TValue defaultValue) where TField : Field, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
				return (TValue) field.getObject();
			return defaultValue;
		}

		public static string MsgType(this Message msg)
		{
			var msgType = new MsgType();
			msg.getHeader().getField(msgType);
			return msgType.getValue();
		}
	}
}