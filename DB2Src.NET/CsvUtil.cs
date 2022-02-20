using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public static class CsvUtil
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

        public static void AddCsv(StringBuilder buffer, IList<string> value, bool quoteForce)
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
        }
    }
    public class StringList
    {
        private List<string> _list = new List<string>();
        public string this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (_list.Count <= index)
                {
                    return null;
                }
                return _list[index];
            }
            set
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (value == null)
                {
                    if (_list.Count <= index)
                    {
                        return;
                    }
                    _list[index] = value;
                    return;
                }
                while (_list.Count <= index)
                {
                    _list.Add(null);
                }
                _list[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public string[] ToArray()
        {
            return _list.ToArray();
        }

        public StringList(string[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _list.AddRange(value);
        }
        public StringList() { }

        public static StringList FromCsv(string text)
        {
            StringList ret = new StringList();
            if (string.IsNullOrEmpty(text))
            {
                ret[0] = text;
                return ret;
            }
            int n = text.Length;
            bool wasQuote = false;
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '\r':
                    case '\n':
                        ret._list.Add(buf.ToString());
                        return ret;
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
                        ret._list.Add(buf.ToString());
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
            }
            ret._list.Add(buf.ToString());
            return ret;
        }

        public static StringList FromCsv(TextReader stream)
        {
            StringList ret = new StringList();
            int v = stream.Read();
            if (v == -1)
            {
                ret[0] = null;
                return ret;
            }
            bool wasQuote = false;
            StringBuilder buf = new StringBuilder();
            for (; v != -1; v = stream.Read())
            {
                char c = (char)v;
                switch (c)
                {
                    case '\r':
                    case '\n':
                        ret._list.Add(buf.ToString());
                        return ret;
                    case '"':
                        if (wasQuote)
                        {
                            buf.Append(c);
                        }
                        for (v = stream.Read(); v != -1 && (char)v != '"'; v = stream.Read())
                        {
                            c = (char)v;
                            buf.Append(c);
                        }
                        break;
                    case ',':
                        ret._list.Add(buf.ToString());
                        buf = new StringBuilder();
                        break;
                    default:
                        if (char.IsSurrogate(c))
                        {
                            buf.Append(c);
                            v = stream.Read();
                            if (v != -1)
                            {
                                buf.Append((char)v);
                            }
                        }
                        break;
                }
                wasQuote = (c == '"');
            }
            ret._list.Add(buf.ToString());
            return ret;
        }

        public string ToCsv(bool quoteForce)
        {
            if (_list.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            CsvUtil.AddCsv(buf, _list, quoteForce);
            return buf.ToString();
        }
    }
    public class StringTable
    {
        private List<List<string>> _list = new List<List<string>>();
        public string this[int row, int column]
        {
            get
            {
                if (row < 0)
                {
                    throw new ArgumentOutOfRangeException("row");
                }
                if (column < 0)
                {
                    throw new ArgumentOutOfRangeException("column");
                }
                if (_list.Count <= row)
                {
                    return null;
                }
                List<string> l = _list[row];
                if (l.Count <= column)
                {
                    return null;
                }
                return _list[row][column];
            }
            set
            {
                if (row < 0)
                {
                    throw new ArgumentOutOfRangeException("row");
                }
                if (column < 0)
                {
                    throw new ArgumentOutOfRangeException("column");
                }
                if (value == null)
                {
                    if (_list.Count <= row)
                    {
                        return;
                    }
                    if (_list[row].Count <= column)
                    {
                        return;
                    }
                    _list[row][column] = value;
                    return;
                }
                while (_list.Count <= row)
                {
                    _list.Add(null);
                }
                List<string> l = _list[row];
                if (l == null)
                {
                    l = new List<string>();
                    _list[row] = l;
                }
                if (l.Count <= column)
                {
                    InvalidateColumnCount();
                }
                while (l.Count <= column)
                {
                    l.Add(null);
                }
                _list[row][column] = value;
            }
        }
        public int RowCount
        {
            get
            {
                return _list.Count;
            }
        }

        private int? _columnCount = null;
        private void RequireColumnCount()
        {
            if (_columnCount.HasValue)
            {
                return;
            }
            int n = 0;
            foreach (List<string> l in _list)
            {
                n = Math.Max(n, l.Count);
            }
            _columnCount = n;
        }
        private void InvalidateColumnCount()
        {
            _columnCount = null;
        }
        public int ColumnCount
        {
            get
            {
                RequireColumnCount();
                return _columnCount.Value;
            }
        }

        public string[][] ToArray()
        {
            string[][] ret = new string[_list.Count][];
            for (int i = 0; i < _list.Count; i++)
            {
                ret[i] = _list[i].ToArray();
            }
            return ret;
        }

        public StringTable(string[][] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            foreach (string[] v in value)
            {
                List<string> l = new List<string>(v);
                _list.Add(l);
            }
        }
        public StringTable() { }

        private static readonly string[][] Empty = new string[][] { new string[] { string.Empty } };
        public static StringTable FromCsv(string text)
        {
            StringTable ret = new StringTable();
            if (string.IsNullOrEmpty(text))
            {
                ret[0, 0] = text;
                return ret;
            }
            List<List<string>> l = ret._list;
            List<string> l2 = new List<string>();
            int n = text.Length;
            bool wasCr = false;
            bool wasQuote = false;
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                if (wasCr)
                {
                    l2.Add(buf.ToString());
                    l.Add(l2);
                    l2 = new List<string>();
                    buf = new StringBuilder();
                }
                switch (c)
                {
                    case '\r':
                        break;
                    case '\n':
                        if (!wasCr)
                        {
                            l2.Add(buf.ToString());
                            l.Add(l2);
                            l2 = new List<string>();
                            buf = new StringBuilder();
                        }
                        break;
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
                        l2.Add(buf.ToString());
                        buf = new StringBuilder();
                        break;
                    default:
                        if (char.IsSurrogate(c))
                        {
                            buf.Append(c);
                            i++;
                            if (i < n)
                            {
                                buf.Append(text[i]);
                            }
                        }
                        break;
                }
                wasQuote = (c == '"');
                wasCr = (c == '\r');
            }
            if (buf.Length != 0)
            {
                l2.Add(buf.ToString());
            }
            if (l2.Count != 0)
            {
                l.Add(l2);
            }
            ret.InvalidateColumnCount();
            return ret;
        }

        public static StringTable FromCsv(TextReader stream)
        {
            StringTable ret = new StringTable();
            int v = stream.Read();
            if (v == -1)
            {
                ret[0, 0] = null;
                return ret;
            }
            List<List<string>> l = ret._list;
            List<string> l2 = new List<string>();
            bool wasCr = false;
            bool wasQuote = false;
            StringBuilder buf = new StringBuilder();
            for (; v != -1; v = stream.Read())
            {
                char c = (char)v;
                if (wasCr)
                {
                    l2.Add(buf.ToString());
                    l.Add(l2);
                    l2 = new List<string>();
                    buf = new StringBuilder();
                }
                switch (c)
                {
                    case '\r':
                        break;
                    case '\n':
                        if (!wasCr)
                        {
                            l2.Add(buf.ToString());
                            l.Add(l2);
                            l2 = new List<string>();
                            buf = new StringBuilder();
                        }
                        break;
                    case '"':
                        if (wasQuote)
                        {
                            buf.Append(c);
                        }
                        for (v = stream.Read(); v != -1 && (char)v != '"'; v = stream.Read())
                        {
                            c = (char)v;
                            buf.Append(c);
                        }
                        break;
                    case ',':
                        l2.Add(buf.ToString());
                        buf = new StringBuilder();
                        break;
                    default:
                        if (char.IsSurrogate(c))
                        {
                            buf.Append(c);
                            v = stream.Read();
                            if (v != -1)
                            {
                                buf.Append((char)v);
                            }
                        }
                        break;
                }
                wasQuote = (c == '"');
                wasCr = (c == '\r');
            }
            if (buf.Length != 0)
            {
                l2.Add(buf.ToString());
            }
            if (l2.Count != 0)
            {
                l.Add(l2);
            }
            ret.InvalidateColumnCount();
            return ret;
        }

        public string ToCsv(bool quoteForce)
        {
            if (_list.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            foreach (List<string> v in _list)
            {
                if (v != null && v.Count != 0)
                {
                    CsvUtil.AddCsv(buf, v, quoteForce);
                }
                buf.AppendLine();
            }
            return buf.ToString();
        }
    }
}
