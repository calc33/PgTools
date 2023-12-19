using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Db2Source
{
    public class StringList
    {
        public static readonly string[] Empty = new string[0];
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

        public StringList(IEnumerable<string> value)
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
            int p = 0;
            string[] ret = CsvConverter.ReadLine(text, ref p);
            return new StringList(ret ?? Empty);
        }

        public static StringList FromCsv(TextReader reader)
        {
            string[] ret = CsvConverter.ReadLine(reader);
            return new StringList(ret ?? Empty);
        }

        public string ToCsv(bool quoteForce)
        {
            if (_list.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            CsvConverter.WriteLine(buf, _list, quoteForce, false);
            return buf.ToString();
        }
    }
}
