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
        public static string ArrayToText(Array value, string separator, string prefix = null, string postfix = null)
        {
            if (value == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                buf.Append(prefix);
            }
            if (0 < value.Length)
            {
                buf.Append(ToString(value.GetValue(0)));
                for (int i = 1; i < value.Length; i++)
                {
                    buf.Append(separator);
                    buf.Append(ToString(value.GetValue(i)));
                }
            }
            if (!string.IsNullOrEmpty(postfix))
            {
                buf.Append(postfix);
            }
            return buf.ToString();
        }
        public static string ArrayToText(Array value, string format, string separator, string prefix = null, string postfix = null)
        {
            if (value == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                buf.Append(prefix);
            }
            if (0 < value.Length)
            {
                buf.Append(ToString(value.GetValue(0), format));
                for (int i = 1; i < value.Length; i++)
                {
                    buf.Append(separator);
                    buf.Append(ToString(value.GetValue(i), format));
                }
            }
            if (!string.IsNullOrEmpty(postfix))
            {
                buf.Append(postfix);
            }
            return buf.ToString();
        }
    }
}
