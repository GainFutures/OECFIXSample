using System;
using QuickFix;
using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX
{
	public static class Extensions
	{
		public static TField CreateIfExists<TField>(this Message msg) where TField : IField, new()
		{
			var field = new TField();
			if (msg.IsSetField(field))
				return field;
			return null;
		}

		public static TField GetString<TField>(this Message msg) where TField : StringField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.GetField(field);
				return field;
			}
			return null;
		}

		public static string Get<TField>(this Message msg, string defaultValue) where TField : StringField, new()
		{
			var field = msg.GetString<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetDouble<TField>(this Message msg) where TField : DecimalField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.GetField(field);
				return field;
			}
			return null;
		}

		public static TField GetChar<TField>(this Message msg) where TField : CharField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.GetField(field);
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
				msg.GetField(field);
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
				msg.GetField(field);
				return field;
			}
			return null;
		}

		public static bool Get<TField>(this Message msg, bool defaultValue) where TField : BooleanField, new()
		{
			var field = msg.GetBool<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TField GetDateTime<TField>(this Message msg) where TField : DateTimeField, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
			{
				msg.GetField(field);
				return field;
			}
			return null;
		}

		public static DateTime Get<TField>(this Message msg, DateTime defaultValue) where TField : DateTimeField, new()
		{
			var field = msg.GetDateTime<TField>();
			return field != null ? field.getValue() : defaultValue;
		}

		public static TValue GetValue<TField, TValue>(this Message msg, TValue defaultValue) where TField : FieldBase<TValue>, new()
		{
			var field = CreateIfExists<TField>(msg);
			if (field != null)
				return field.Obj;
			return defaultValue;
		}

		public static string MsgType(this Message msg)
		{
			return msg.Header.GetField(Tags.MsgType);
		}
	}
}