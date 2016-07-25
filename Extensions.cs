using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OEC.FIX.Sample.FoxScript;
using QuickFix;
using QuickFix.Fields;

namespace OEC.FIX.Sample
{
    internal static class Extensions
    {
        public static void SetFieldValue(this Message msg, object value, Type fieldType)
        {
            int tag = fieldType.FieldTag();
            msg.FieldMapFor(tag).SetField(CreateField(fieldType, tag, value));
        }

        private static IField CreateField(Type fieldType, int tag, object value)
        {
            if (fieldType.BaseTypeIs<BooleanField>())
                return new BooleanField(tag, (bool)value);

            if (fieldType.BaseTypeIs<CharField>())
                return new CharField(tag, (char)value);

            if (fieldType.BaseTypeIs<DecimalField>())
                return new DecimalField(tag, (decimal)value);

            if (fieldType.BaseTypeIs<IntField>())
                return new IntField(tag, (int)value);

            if (fieldType.BaseTypeIs<StringField>())
                return new StringField(tag, (string)value);

            if (fieldType.BaseTypeIs<DateOnlyField>())
                return new DateOnlyField(tag, (DateTime)value);

            if (fieldType.BaseTypeIs<TimeOnlyField>())
                return new TimeOnlyField(tag, (DateTime)value);

            if (fieldType.BaseTypeIs<DateTimeField>())
                return new DateTimeField(tag, (DateTime)value);

            throw new ExecutionException("Unsupported FIX field type.");
        }

        public static object GetFieldValue(this Message msg, Type fieldType)
        {
            int tag = fieldType.FieldTag();
            return GetFieldValue(fieldType, msg.FieldMapFor(tag), tag);
        }

        private static object GetFieldValue(Type fieldType, FieldMap map, int tag)
        {
            if (fieldType.BaseTypeIs<BooleanField>())
                return map.GetBoolean(tag);

            if (fieldType.BaseTypeIs<CharField>())
                return map.GetChar(tag);

            if (fieldType.BaseTypeIs<DecimalField>())
                return map.GetDecimal(tag);

            if (fieldType.BaseTypeIs<IntField>())
                return map.GetInt(tag);

            if (fieldType.BaseTypeIs<StringField>())
                return map.GetString(tag);

            if (fieldType.BaseTypeIs<DateOnlyField>())
                return map.GetDateOnly(tag);

            if (fieldType.BaseTypeIs<TimeOnlyField>())
                return map.GetTimeOnly(tag);

            if (fieldType.BaseTypeIs<DateTimeField>())
                return map.GetDateTime(tag);

            throw new ExecutionException("Unsupported FIX field type.");
        }

        public static int FieldTag(this Type fieldType)
        {
            var tagTypes = new[] { typeof(Tags), typeof(FIX.Fields.Tags) };
            var constFields = tagTypes
                .Select(type => type.GetField(fieldType.Name, BindingFlags.Public | BindingFlags.Static))
                .Where(info => info != null && info.IsLiteral && !info.IsInitOnly);
            foreach (var fieldInfo in constFields)
                return (int)fieldInfo.GetValue(null);

            throw new ExecutionException("Unknown tag: {0}", fieldType.Name);
        }

        public static IEnumerable<FieldInfo> GetFieldConsts(this Type fieldType)
        {
            return fieldType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fieldInfo => fieldInfo.IsLiteral && !fieldInfo.IsInitOnly);
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
            return BaseTypeIs(type, typeof(TBase));
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
                new[] { typeof(object) },
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