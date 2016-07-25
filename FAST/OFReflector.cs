using System;
using System.Reflection;
using OpenFAST;
using OpenFAST.Template;

namespace OEC.FIX.Sample.FAST
{
    internal class OFReflector
    {
        public static readonly MethodInfo GetFieldValueMethodInfo = typeof(OFReflector).GetMethod("GetFieldValue",
            new[] { typeof(Message), typeof(string) });

        public static void SetFieldValue(Message target, string fieldName, object fieldValue)
        {
            string fastTypeName = GetFastTypeName(target, fieldName);
            if (fastTypeName != null)
            {
                switch (fastTypeName)
                {
                    case "uint32":
                        target.SetInteger(fieldName, (int)fieldValue);
                        break;
                    case "ascii":
                        target.SetString(fieldName, (string)fieldValue);
                        break;
                    case "decimal":
                        target.SetDecimal(fieldName, (decimal)fieldValue);
                        break;
                    case "uint64":
                        target.SetLong(fieldName, (long)fieldValue);
                        break;
                }
            }
            else
            {
                throw new ExecutionException("Invalid FAST field '{0}'", fieldName);
            }
        }

        public static string GetFastTypeName(Message target, string fieldName)
        {
            if (target.Template.HasField(fieldName))
            {
                Field field = target.Template.GetField(fieldName);
                return (field as Scalar)?.FASTType.Name.ToLower();
            }
            return null;
        }

        public static object GetFieldValue(Message msg, string fieldName)
        {
            if (msg.IsDefined(fieldName))
            {
                string fastTypeName = GetFastTypeName(msg, fieldName);
                if (fastTypeName != null)
                {
                    switch (fastTypeName)
                    {
                        case "uint32":
                            return msg.GetInt(fieldName);
                        case "ascii":
                            return msg.GetString(fieldName);
                        case "decimal":
                            return msg.GetDouble(fieldName);
                        case "uint64":
                            return msg.GetLong(fieldName);
                    }
                }
            }

            return null;
        }

        public static long ToFastDateTime(DateTime dateTime)
        {
            return long.Parse(dateTime.ToString("yyyyMMddHHmmss"));
        }
    }
}