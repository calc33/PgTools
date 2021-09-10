using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public static class StrUtil
    {
        private static string ToString(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            return obj.ToString();
        }
        private static readonly Type[] FmtArg = new Type[] { typeof(string) };
        private static string ToString(object obj, string format)
        {
            if (obj == null)
            {
                return null;
            }
            MethodInfo m = obj.GetType().GetMethod("ToString", FmtArg);
            if (m == null)
            {
                return obj.ToString();
            }
            return (string)m.Invoke(obj, new object[] { format });
        }

        public static readonly string[] EmptyStringArray = new string[0];
        public static readonly string[][] EmptyString2DArray = new string[0][];
        public static string ArrayToText(Array value, string separator, string prefix = "", string postfix = "")
        {
            if (value == null)
            {
                return null;
            }
            if (value.Length == 0)
            {
                return prefix + postfix;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            buf.Append(ToString(value.GetValue(0)));
            for (int i = 1; i < value.Length; i++)
            {
                buf.Append(separator);
                buf.Append(ToString(value.GetValue(i)));
            }
            buf.Append(postfix);
            return buf.ToString();
        }
        public static string ArrayToText(Array value, string format, string separator, string prefix = "", string postfix = "")
        {
            if (value == null)
            {
                return null;
            }
            if (value.Length == 0)
            {
                return prefix + postfix;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            if (0 < value.Length)
            {
                buf.Append(ToString(value.GetValue(0), format));
                for (int i = 1; i < value.Length; i++)
                {
                    buf.Append(separator);
                    buf.Append(ToString(value.GetValue(i), format));
                }
            }
            buf.Append(postfix);
            return buf.ToString();
        }

        public static string DelimitedText(IEnumerable<string> value, string separator, string prefix = "", string postfix = "")
        {
            if (value == null)
            {
                return prefix + postfix;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            bool needSeparator = false;
            foreach (string s in value)
            {
                if (needSeparator)
                {
                    buf.Append(separator);
                }
                buf.Append(s);
                needSeparator = true;
            }
            buf.Append(postfix);
            return buf.ToString();

        }
    }
}
