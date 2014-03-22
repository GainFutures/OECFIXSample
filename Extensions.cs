using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OEC.FIX.Sample.FoxScript;
using QuickFix;
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
				msg.FieldMapFor(tag).setBoolean(tag, (bool) value);
			}
			else if (fieldType.BaseTypeIs<CharField>())
			{
				msg.FieldMapFor(tag).setChar(tag, (char) value);
			}
			else if (fieldType.BaseTypeIs<DoubleField>())
			{
				msg.FieldMapFor(tag).setDouble(tag, (double) value);
			}
			else if (fieldType.BaseTypeIs<IntField>())
			{
				msg.FieldMapFor(tag).setInt(tag, (int) value);
			}
			else if (fieldType.BaseTypeIs<StringField>())
			{
				msg.FieldMapFor(tag).setString(tag, (string) value);
			}
			else if (fieldType.BaseTypeIs<UtcDateOnlyField>())
			{
				msg.FieldMapFor(tag).setUtcDateOnly(tag, (DateTime) value);
			}
			else if (fieldType.BaseTypeIs<UtcTimeOnlyField>())
			{
				msg.FieldMapFor(tag).setUtcTimeOnly(tag, (DateTime) value);
			}
			else if (fieldType.BaseTypeIs<UtcTimeStampField>())
			{
				msg.FieldMapFor(tag).setUtcTimeStamp(tag, (DateTime) value);
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
				return msg.FieldMapFor(tag).getBoolean(tag);
			}
			if (fieldType.BaseTypeIs<CharField>())
			{
				return msg.FieldMapFor(tag).getChar(tag);
			}
			if (fieldType.BaseTypeIs<DoubleField>())
			{
				return msg.FieldMapFor(tag).getDouble(tag);
			}
			if (fieldType.BaseTypeIs<IntField>())
			{
				return msg.FieldMapFor(tag).getInt(tag);
			}
			if (fieldType.BaseTypeIs<StringField>())
			{
				return msg.FieldMapFor(tag).getString(tag);
			}
			if (fieldType.BaseTypeIs<UtcDateOnlyField>())
			{
				return msg.FieldMapFor(tag).getUtcDateOnly(tag);
			}
			if (fieldType.BaseTypeIs<UtcTimeOnlyField>())
			{
				return msg.FieldMapFor(tag).getUtcTimeOnly(tag);
			}
			if (fieldType.BaseTypeIs<UtcTimeStampField>())
			{
				return msg.FieldMapFor(tag).getUtcTimeStamp(tag);
			}
			throw new ExecutionException("Unsupported FIX field type.");
		}

		public static int FieldTag(this Type fieldType)
		{
			FieldInfo field = fieldType.GetField("FIELD", BindingFlags.Public | BindingFlags.Static);
			if (field == null)
			{
				throw new ExecutionException("FIELD field not found.");
			}
			return (int) field.GetValue(null);
		}

		public static IEnumerable<FieldInfo> GetFieldConsts(this Type fieldType)
		{
			return fieldType
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.Name != "FIELD");
		}

		public static bool IsFieldType(this Type type)
		{
			FieldInfo field = type.GetField("FIELD", BindingFlags.Public | BindingFlags.Static);
			return type.BaseTypeIs<Field>() && field != null;
		}

		public static IEnumerable<Field> Fields(this Message msg)
		{
			foreach (object field in msg.getHeader())
			{
				yield return (Field) field;
			}
			foreach (object field in msg)
			{
				yield return (Field) field;
			}
			foreach (object field in msg.getTrailer())
			{
				yield return (Field) field;
			}
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
				return source.getHeader();
			}

			if (Tools.TrailerFields.Contains(field))
			{
				return source.getTrailer();
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