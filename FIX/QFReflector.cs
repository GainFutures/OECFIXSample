using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using QuickFix;
using QuickFix.Fields;
using Message = QuickFix.Message;

namespace OEC.FIX.Sample.FIX
{
    internal static class QFReflector
    {
        public static readonly MethodInfo GetFieldValueMethodInfo = typeof(QFReflector).GetMethod("GetFieldValue", new[] { typeof(Message), typeof(string) });

        private static readonly Assembly QFAssembly = Assembly.GetAssembly(typeof(Side));
        private static readonly Dictionary<int, Type> FieldTypes = new Dictionary<int, Type>();

        static QFReflector()
        {
            IEnumerable<Type> types = QFAssembly
                .GetTypes()
                .Where(t => t.IsFieldType());

            foreach (Type type in types)
            {
                FieldTypes[type.FieldTag()] = type;
            }
        }

        public static void SetFieldValue(Message target, string fieldName, object fieldValue)
        {
            int tag;
            if (int.TryParse(fieldName, out tag))
            {
                Type fieldType = GetFieldType(tag);
                if (fieldType != null)
                {
                    object value = DenormalizeFieldValue(fieldValue, fieldType);
                    target.SetFieldValue(value, fieldType);
                }
                else
                {
                    //TODO: Same as there FixProtocol.CopyFields
                    target.SetField(new StringField(tag, fieldValue.ToString()));
                }
            }
            else
            {
                Type fieldType = RetrieveFieldType(fieldName);

                object value = DenormalizeFieldValue(fieldValue, fieldType);
                target.SetFieldValue(value, fieldType);
            }
        }

        public static string FormatMessage(Message msg)
        {
            if (msg == null)
            {
                return "NULL";
            }

            var s = new StringBuilder();
            foreach (IField field in msg.Select(pair => pair.Value))
            {
                Type fieldType = GetFieldType(field.Tag);
                if (fieldType != null)
                {
                    s.Append(fieldType.Name);
                }
                else
                {
                    s.Append(field.Tag);
                }

                s.Append("=");

                string constName = null;
                if (fieldType != null)
                {
                    constName = GetConstName(msg, fieldType);
                }

                if (string.IsNullOrEmpty(constName))
                {
                    s.Append(field);
                }
                else
                {
                    s.Append(constName);
                }

                s.Append(" ");
            }

            return s.ToString();
        }

        public static object GetFieldValue(Message msg, string fieldName)
        {
            if (msg == null)
            {
                throw new ExecutionException("FIX message not specified.");
            }
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ExecutionException("FIX field not specified.");
            }

            int tag;
            if (int.TryParse(fieldName, out tag))
            {
                Type fieldType = GetFieldType(tag);
                if (fieldType != null)
                {
                    return NormalizeFieldValue(msg.GetFieldValue(fieldType), fieldType);
                }
                return msg.FieldMapFor(tag).GetString(tag);
            }
            else
            {
                Type fieldType = RetrieveFieldType(fieldName);

                return NormalizeFieldValue(msg.GetFieldValue(fieldType), fieldType);
            }
        }

        public static object GetConstValue(string fieldName, string constName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ExecutionException("FIX field not specified.");
            }

            Type fieldType = RetrieveFieldType(fieldName);

            FieldInfo fld = fieldType
                .GetFieldConsts().FirstOrDefault(f => String.Compare(f.Name, constName, StringComparison.OrdinalIgnoreCase) == 0);

            if (fld == null)
            {
                throw new ExecutionException("Invalid FIX const name '{0}'", constName);
            }

            return NormalizeFieldValue(fld.GetValue(null), fieldType);
        }

        public static string GetMsgTypeValue(string msgTypeName)
        {
            if (string.IsNullOrEmpty(msgTypeName))
            {
                throw new ExecutionException("MsgTypeName not specified.");
            }

            return (string)GetConstValue("MsgType", msgTypeName);
        }

        private static Type RetrieveFieldType(string fieldNameOrTag)
        {
            int tag;
            Type fieldType = int.TryParse(fieldNameOrTag, out tag) ? GetFieldType(tag) : GetFieldType(fieldNameOrTag);
            if (fieldType == null)
            {
                throw new ExecutionException("Invalid FIX field '{0}'", fieldNameOrTag);
            }
            return fieldType;
        }

        public static object DenormalizeFieldValue(object value, Type fieldType)
        {
            if (value == null)
                return null;

            if (fieldType.BaseTypeIs<CharField>())
                return value.ToString().First();

            if (fieldType.BaseTypeIs<TimeOnlyField>())
                return DateTime.Now.Date.Add((TimeSpan)value);

            return value;
        }

        public static object NormalizeFieldValue(object value, Type fieldType)
        {
            if (value == null)
            {
                return null;
            }

            if (fieldType.BaseTypeIs<CharField>())
            {
                return value.ToString();
            }

            if (fieldType.BaseTypeIs<TimeOnlyField>())
            {
                var time = (DateTime)value;
                return time.TimeOfDay;
            }

            return value;
        }

        private static string GetConstName(Message msg, Type fieldType)
        {
            object val = msg.GetFieldValue(fieldType);

            IEnumerable<FieldInfo> consts = fieldType.GetFieldConsts();
            var results = (
                from cnst in consts
                let value = cnst.GetValue(null)
                where value.Equals(val)
                select cnst.Name).ToList();

            string result = results.FirstOrDefault(name => name.Contains("_"));

            return result ?? results.FirstOrDefault();
        }

        private static Type GetFieldType(int fieldTag)
        {
            Type type;
            FieldTypes.TryGetValue(fieldTag, out type);
            return type;
        }

        private static Type GetFieldType(string fieldName)
        {
            var types = new Func<Type>[]
            {
                () => QFAssembly.GetType("QuickFix.Fields." + fieldName, false, true),
                () => Assembly.GetAssembly(typeof (Fields.Tags)).GetType("OEC.FIX.Sample.FIX.Fields" + fieldName, false, true)
            };

            return types.Select(func => func()).FirstOrDefault(t => t != null && t.IsFieldType());
        }
    }
}