using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OEC.FIX.Sample.FoxScript;
using QuickFix;
using QuickFix.Fields;
using Object = OEC.FIX.Sample.FoxScript.Object;

namespace OEC.FIX.Sample
{
	internal static class Extensions
	{
		public static void SetFieldValue(this Message msg, object value, Type fieldType)
		{
			int tag = fieldType.FieldTag();

			if (fieldType.BaseTypeIs<BooleanField>())
			{
			    msg.FieldMapFor(tag).SetField(new BooleanField(tag, (bool)value));
			}
			else if (fieldType.BaseTypeIs<CharField>())
			{
			    msg.FieldMapFor(tag).SetField(new CharField(tag, (char)value));
			}
			else if (fieldType.BaseTypeIs<DecimalField>())
			{
				msg.FieldMapFor(tag).SetField(new DecimalField(tag, (decimal)value));
			}
			else if (fieldType.BaseTypeIs<IntField>())
			{
				msg.FieldMapFor(tag).SetField(new IntField(tag, (int)value));
			}
			else if (fieldType.BaseTypeIs<StringField>())
			{
			    msg.FieldMapFor(tag).SetField(new StringField(tag, (string)value));
			}
            else if (fieldType.BaseTypeIs<DateOnlyField>())
            {
                msg.FieldMapFor(tag).SetField(new DateOnlyField(tag, (DateTime)value));
            }
            else if (fieldType.BaseTypeIs<TimeOnlyField>())
            {
                msg.FieldMapFor(tag).SetField(new TimeOnlyField(tag, (DateTime)value));
            }
            else if (fieldType.BaseTypeIs<DateTimeField>())
			{
			    msg.FieldMapFor(tag).SetField(new DateTimeField(tag, (DateTime)value));
			}
			else
			{
				throw new ExecutionException("Unsupported FIX field type.");
			}
		}

		public static object GetFieldValue(this Message msg, Type fieldType)
		{
			int tag = fieldType.FieldTag();

			if (fieldType.BaseTypeIs<BooleanField>())
			{
				return msg.FieldMapFor(tag).GetBoolean(tag);
			}
			if (fieldType.BaseTypeIs<CharField>())
			{
				return msg.FieldMapFor(tag).GetChar(tag);
			}
			if (fieldType.BaseTypeIs<DecimalField>())
			{
				return msg.FieldMapFor(tag).GetDecimal(tag);
			}
			if (fieldType.BaseTypeIs<IntField>())
			{
				return msg.FieldMapFor(tag).GetInt(tag);
			}
            if (fieldType.BaseTypeIs<StringField>())
            {
                return msg.FieldMapFor(tag).GetString(tag);
            }
            if (fieldType.BaseTypeIs<DateOnlyField>())
            {
                return msg.FieldMapFor(tag).GetDateOnly(tag);
            }
            if (fieldType.BaseTypeIs<TimeOnlyField>())
            {
                return msg.FieldMapFor(tag).GetTimeOnly(tag);
            }
            if (fieldType.BaseTypeIs<DateTimeField>())
			{
				return msg.FieldMapFor(tag).GetDateTime(tag);
			}
			throw new ExecutionException("Unsupported FIX field type.");
		}

		public static int FieldTag(this Type fieldType)
		{
#error "FIELD" does not work any more
            FieldInfo field = fieldType.GetField("FIELD", BindingFlags.Public | BindingFlags.Static);
			if (field == null)
			{
				throw new ExecutionException("FIELD field not found.");
			}
			return (int) field.GetValue(null);
		}

        public static IEnumerable<FieldInfo> GetFieldConsts(this Type fieldType)
		{
            return fieldType.GetFields(BindingFlags.Public | BindingFlags.Static);
        }

        public static bool IsFieldType(this Type type)
        {
            return type.BaseTypeIs<IField>();
		}

		public static IEnumerable<IField> Fields(this Message msg)
		{
		    return msg.Header.Union(msg).Union(msg.Trailer).Select(pair => pair.Value);
		}

		public static bool BaseTypeIs<TBase>(this Type type)
		{
			return BaseTypeIs(type, typeof (TBase));
		}

		public static bool BaseTypeIs(this Type type, Type baseType)
		{
			Type bt = type.BaseType;
			while (bt != null)
			{
				if (bt == baseType)
				{
					return true;
				}
				bt = bt.BaseType;
			}
			return false;
		}

		public static FieldMap FieldMapFor(this Message source, int field)
		{
			if (Tools.HeaderFields.Contains(field))
			{
				return source.Header;
			}

			if (Tools.TrailerFields.Contains(field))
			{
				return source.Trailer;
			}

			return source;
		}


		public static int IndexOf<T>(this T[] array, T value)
		{
			for (int i = 0; i < array.Length; ++i)
			{
				if (value.Equals(array[i]))
				{
					return i;
				}
			}
			return -1;
		}

		public static MethodInfo GetEqualsMethodInfo(this Type type)
		{
			return type.GetMethod(
				"Equals",
				BindingFlags.Public | BindingFlags.Instance,
				null,
				new[] {typeof (object)},
				null);
		}

		public static bool Empty<T>(this Stack<T> stack)
		{
			return stack.Count == 0;
		}

		public static bool IsObject(this object obj)
		{
			return obj is FoxScript.Object;
		}

		public static bool IsLogicalExpr(this object obj)
		{
			return obj is LogicalExpr;
		}
	}
}