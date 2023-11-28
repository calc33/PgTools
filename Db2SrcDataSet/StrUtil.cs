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

        private static string QuotedString(string value, char quoteChar, Dictionary<char, bool> escapedChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            StringBuilder builder = new StringBuilder(value.Length * 2 + 2);
            bool needQuote = false;
            builder.Append(quoteChar);
            foreach (char c in value)
            {
                if (c == quoteChar)
                {
                    builder.Append(c);
                    needQuote = true;
                }
                builder.Append(c);
                if (escapedChars.ContainsKey(c))
                {
                    needQuote = true;
                }
            }
            builder.Append(quoteChar);
            if (needQuote)
            {
                return builder.ToString();
            }
            return value;
        }
        public static string DelimitedText(string[] value, char separator, char quoteChar, string escapeChars)
        {
            if (value == null)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            string delim = string.Empty;
            Dictionary<char, bool> escapeDict = new Dictionary<char, bool>();
            foreach (char c in escapeChars)
            {
                escapeDict[c] = true;
            }
            escapeDict[separator] = true;
            buf.Append(QuotedString(value[0], quoteChar, escapeDict));
            for (int i = 1, n = value.Length; i < n; i++)
            {
                buf.Append(separator);
                buf.Append(QuotedString(value[i], quoteChar, escapeDict));
            }
            return buf.ToString();

        }

        public static string[] SplitDelimitedText(string value, char separator, char quoteChar)
        {
            List<string> list = new List<string>();
            StringBuilder builder = new StringBuilder(value.Length);
            bool wasQuoteChar = false;
            char c = '\0';
            for (int i = 0, n = value.Length; i < n; i++)
            {
                wasQuoteChar = (c == separator);
                c = value[i];
                if (c == separator)
                {
                    list.Add(builder.ToString());
                    builder.Clear();
                    continue;
                }
                if (c == quoteChar)
                {
                    if (wasQuoteChar)
                    {
                        builder.Append(c);
                    }
                    for (i++; i < n && value[i] != separator; i++)
                    {
                        builder.Append(value[i]);
                    }
                    continue;
                }
                builder.Append(c);
            }
            list.Add(builder.ToString());
            return list.ToArray();
        }
    }
}
