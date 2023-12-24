using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public static class CsvConverter
    {
        private static readonly char[] QuoteChars = new char[] { '\n', '\r', '"', ',' };
        public static string TryQuote(string value, bool force)
        {
            if (string.IsNullOrEmpty(value) && !force)
            {
                return value;
            }
            if (!force && value.IndexOfAny(QuoteChars) == -1)
            {
                return value;
            }
            StringBuilder buf = new StringBuilder(value.Length * 2 + 2);
            buf.Append('"');
            foreach (char c in value)
            {
                if (c == '"')
                {
                    buf.Append("\"\"");
                }
                else
                {
                    buf.Append(c);
                }
            }
            buf.Append('"');
            return buf.ToString();
        }

        public static void WriteLine(StringBuilder buffer, IList<string> value, bool quoteForce, bool appendNewLine)
        {
            if (value.Count == 0)
            {
                return;
            }
            buffer.Append(TryQuote(value[0], quoteForce));
            for (int i = 1, n = value.Count; i < n; i++)
            {
                buffer.Append(',');
                buffer.Append(TryQuote(value[i], quoteForce));
            }
            if (appendNewLine)
            {
                buffer.AppendLine();
            }
        }
        public static string[] ReadLine(string text, ref int position)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StrUtil.EmptyStringArray;
            }
            if (text.Length <= position)
            {
                return null;
            }
            List<string> l = new List<string>();
            int n = text.Length;
            bool wasQuote = false;
            bool wasCR = false;
            StringBuilder buf = new StringBuilder();
            for (int i = position; i < n; i++)
            {
                char c = text[i];
                if (wasCR)
                {
                    l.Add(buf.ToString());
                    if (c == '\n')
                    {
                        position = i + 1;
                    }
                    else
                    {
                        position = i;
                    }
                    return l.ToArray();
                }
                switch (c)
                {
                    case '\r':
                        break;
                    case '\n':
                        l.Add(buf.ToString());
                        position = i + 1;
                        return l.ToArray();
                    case '"':
                        if (wasQuote)
                        {
                            buf.Append(c);
                        }
                        for (; i < n && text[i] != '"'; i++)
                        {
                            buf.Append(text[i]);
                        }
                        break;
                    case ',':
                        l.Add(buf.ToString());
                        buf = new StringBuilder();
                        break;
                    default:
                        buf.Append(c);
                        if (char.IsSurrogate(c))
                        {
                            i++;
                            if (i < n)
                            {
                                buf.Append(text[i]);
                            }
                        }
                        break;
                }
                wasQuote = (c == '"');
                wasCR = (c == '\r');
            }
            l.Add(buf.ToString());
            position = n;
            return l.ToArray();
        }
        public static string[] ReadLine(TextReader reader)
        {
            int v = reader.Read();
            if (v == -1)
            {
                return null;
            }
            List<string> l = new List<string>();
            bool wasQuote = false;
            StringBuilder buf = new StringBuilder();
            for (; v != -1; v = reader.Read())
            {
                char c = (char)v;
                switch (c)
                {
                    // 改行文字は \rと\nと\r\nがある
                    case '\r':
                        if (reader.Peek() == (int)'\n')
                        {
                            reader.Read();
                        }
                        l.Add(buf.ToString());
                        return l.ToArray();
                    case '\n':
                        l.Add(buf.ToString());
                        return l.ToArray();
                    case '"':
                        if (wasQuote)
                        {
                            buf.Append(c);
                        }
                        for (; v != -1 && (char)v != '"'; v = reader.Read())
                        {
                            buf.Append((char)v);
                        }
                        break;
                    case ',':
                        l.Add(buf.ToString());
                        buf = new StringBuilder();
                        break;
                    default:
                        buf.Append(c);
                        if (char.IsSurrogate(c))
                        {
                            v = reader.Read();
                            if (v != -1)
                            {
                                buf.Append((char)v);
                            }
                        }
                        break;
                }
                wasQuote = (c == '"');
            }
            l.Add(buf.ToString());
            return l.ToArray();
        }
    }
}
