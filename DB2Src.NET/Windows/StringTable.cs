using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Db2Source
{
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
            int p = 0;
            string[] ret = CsvConverter.ReadLine(text, ref p);
            if (ret == null)
            {
                return new StringTable(Empty);
            }
            List<string[]> l = new List<string[]>();
            for (; ret != null; ret = CsvConverter.ReadLine(text, ref p))
            {
                l.Add(ret);
            }
            return new StringTable(l.ToArray());
        }

        public static StringTable FromCsv(TextReader reader)
        {
            string[] ret = CsvConverter.ReadLine(reader);
            if (ret == null)
            {
                return new StringTable(Empty);
            }
            List<string[]> l = new List<string[]>();
            for (; ret != null; ret = CsvConverter.ReadLine(reader))
            {
                l.Add(ret);
            }
            return new StringTable(l.ToArray());
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
                    CsvConverter.WriteLine(buf, v, quoteForce, false);
                }
                buf.AppendLine();
            }
            return buf.ToString();
        }
    }
}
