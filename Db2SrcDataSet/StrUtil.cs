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

        public static string QuotedString(string value, char quoteChar, Dictionary<char, bool> escapedChars)
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

        /// <summary>
        /// ExcelのTSV/CSVテキストのエスケープ仕様に合わせたエスケープを解除処理
        /// 1. "で始まって"で終わらない文字列はそのまま返す(文字列中に"があっても無視)
        /// 2. ""でくくられた文字列中にデリミタ(TABもしくはカンマ)、改行、""が入っている場合は解除処理を行う
        /// 3. ""で括られている場合でも上記に該当しない場合はそのまま返す
        /// </summary>
        /// <param name="value"></param>
        /// <param name="quoteChar"></param>
        /// <param name="escapeChars"></param>
        /// <returns></returns>
        private static string DequoteEscaped(string value, char quoteChar, Dictionary<char, bool> escapeChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value.Length < 2)
            {
                return value;
            }
            if (value[0] != quoteChar || value[value.Length - 1] != quoteChar)
            {
                return value;
            }
            bool escaped = false;
            StringBuilder buf = new StringBuilder(value.Length - 2);
            for (int i = 1, n = value.Length - 1; i < n; i++)
            {
                char c = value[i];
                buf.Append(c);
                if (escapeChars.ContainsKey(c))
                {
                    escaped = true;
                }
                if (c == quoteChar)
                {
                    escaped = true;
                    i++;
                    if (n <= i)
                    {
                        break;
                    }
                    c = value[i];
                    if (c != quoteChar)
                    {
                        buf.Append(c);
                    }
                }
            }
            return escaped ? buf.ToString() : value;
        }

        private static string[][] Get2DArrayFromDelimitedText(string text, char quoteChar, char delimiter, Dictionary<char, bool> escapeChars)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StrUtil.EmptyString2DArray;
            }
            List<List<string>> lRet = new List<List<string>>();
            StringBuilder buf = new StringBuilder();
            List<string> cols = new List<string>();
            int p0 = 0;
            int p = 0;
            for (int n = text.Length; p < n; p++)
            {
                char c = text[p];
                if (c == quoteChar)
                {
                    for (p++; p < n && text[p] != '"'; p++) ;
                }
                else if (c == delimiter)
                {
                    cols.Add(DequoteEscaped(text.Substring(p0, p - p0), quoteChar, escapeChars));
                    p0 = p + 1;
                }
                else if (c == '\r')
                {
                    cols.Add(DequoteEscaped(text.Substring(p0, p - p0), quoteChar, escapeChars));
                    lRet.Add(cols);
                    cols = new List<string>();
                    if (p + 1 < n && text[p + 1] == '\n')
                    {
                        p++;
                    }
                    p0 = p + 1;
                }
                else if (c == '\n')
                {
                    cols.Add(DequoteEscaped(text.Substring(p0, p - p0), quoteChar, escapeChars));
                    lRet.Add(cols);
                    cols = new List<string>();
                    p0 = p + 1;
                }
            }
            if (p0 < p)
            {
                cols.Add(DequoteEscaped(text.Substring(p0, p - p0), quoteChar, escapeChars));
            }
            if (0 < cols.Count)
            {
                lRet.Add(cols);
            }
            foreach (List<string> ls in lRet)
            {
                for (int i = ls.Count - 1; 0 <= i && string.IsNullOrEmpty(ls[i]); i--)
                {
                    ls.RemoveAt(i);
                }
            }
            for (int i = lRet.Count - 1; 0 <= i && lRet[i].Count == 0; i--)
            {
                lRet.RemoveAt(i);
            }
            int nCol = 0;
            foreach (List<string> l in lRet)
            {
                nCol = Math.Max(nCol, l.Count);
            }

            string[][] ret = new string[lRet.Count][];
            for (int i = 0; i < lRet.Count; i++)
            {
                // 要素数を揃えた二次元配列にする
                ret[i] = new string[nCol];
                lRet[i].CopyTo(ret[i]);
            }
            return ret;
        }


        private static readonly Dictionary<char, bool> TabTextEscapeChars = new Dictionary<char, bool>()
        {
            {'\t', true }, {'\r', true }, {'\n', true }
        };

        public static string[][] Get2DArrayFromTabText(string text)
        {
            return Get2DArrayFromDelimitedText(text, '"', '\t', TabTextEscapeChars);
        }

        private static readonly Dictionary<char, bool> CSVEscapeChars = new Dictionary<char, bool>()
        {
            {',', true }, {'\r', true }, {'\n', true }
        };

        public static string[][] Get2DArrayFromCSV(string text)
        {
            return Get2DArrayFromDelimitedText(text, '"', ',', CSVEscapeChars);
        }
    }
}
