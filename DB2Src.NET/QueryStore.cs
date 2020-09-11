using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Db2Source
{
    public class QueryStore
    {
        private string _sql;
        private string _hash = null;
        public string Sql
        {
            get
            {
                return _sql;
            }
        }
        public ParameterStoreCollection Parameters { get; set; }
        public static string GetHash(string sql)
        {
            SHA1 sha = SHA1.Create();
            byte[] b = Encoding.UTF8.GetBytes(sql);
            using (MemoryStream stream = new MemoryStream(b))
            {
                stream.Position = 0;
                byte[] hash = sha.ComputeHash(b);
                return Convert.ToBase64String(hash);
            }
        }
        public string Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = GetHash(Sql);
                }
                return _hash;
            }
        }

        private static void ReadQuotedStr(StringBuilder buffer, TextReader reader)
        {
            // 文字列の抽出
            for (int c = reader.Read(); c != -1; c = reader.Read())
            {
                buffer.Append(c);
                if (c == '"')
                {
                    if (reader.Peek() != '"')
                    {
                        break;
                    }
                    c = reader.Read();
                    buffer.Append(c);
                }
            }
        }
        private static string GetWord(TextReader reader, bool isValue)
        {
            int c;
            for (c = reader.Read(); c != -1 && char.IsWhiteSpace((char)c); c = reader.Read()) ;
            if (c == -1)
            {
                return null;
            }
            // : = は区切り記号
            switch ((char)c)
            {
                case ':':
                case '=':
                    return ((char)c).ToString();
            }
            StringBuilder buf = new StringBuilder();
            if (c == '"')
            {
                buf.Append(c);
                ReadQuotedStr(buf, reader);
                return buf.ToString();
            }

            while (c != -1)
            {
                buf.Append((char)c);
                c = reader.Peek();
                if (!isValue && char.IsWhiteSpace((char)c))
                {
                    return buf.ToString();
                }
                switch ((char)c)
                {
                    case '\u0010':
                    case '\u0013':
                    case ':':
                    case '=':
                    case '"':
                        return buf.ToString();
                }
                c = reader.Read();
            }
            return buf.ToString();
        }
        public static string QuoteStr(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('"');
            foreach (char c in value)
            {
                if (c == '"')
                {
                    buf.Append(c);
                }
                buf.Append(c);
            }
            buf.Append('"');
            return buf.ToString();
        }
        public static string DequoteStr(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value[0] != '"')
            {
                return value;
            }
            StringBuilder buf = new StringBuilder(value.Length - 2);
            for (int i = 1, n = value.Length - 1; i < n; i++)
            {
                char c = value[i];
                if (c == '"')
                {
                    i++;
                }
                buf.Append(c);
            }
            return buf.ToString();
        }
        private const string ParamSeparator = "-----";
        public QueryStore(TextReader reader)
        {
            StringBuilder buf = new StringBuilder();
            for (string s = reader.ReadLine(); s != null && s.StartsWith(ParamSeparator); s = reader.ReadLine())
            {
                buf.AppendLine(s);
            }
            _sql = buf.ToString();
            Parameters = new ParameterStoreCollection();
            while (reader.Peek() != -1)
            {
                string pName = GetWord(reader, false);
                if (pName == null)
                {
                    break;
                }
                if (pName.StartsWith(ParamSeparator))
                {
                    return;
                }
                if (!pName.StartsWith("--"))
                {
                    throw new ApplicationException("パラメータ宣言が -- で開始されていません");
                }
                if (pName == "--")
                {
                    pName = GetWord(reader, false);
                }
                else
                {
                    pName = pName.Substring(2);
                }
                string s = GetWord(reader, false);
                if (s == null || s != ":")
                {
                    throw new ApplicationException(":が見つかりません");
                }
                string typeStr = GetWord(reader, false);
                s = GetWord(reader, false);
                if (s == null || s != "=")
                {
                    throw new ApplicationException("=が見つかりません");
                }
                string val = GetWord(reader, true);
                ParameterStore param = new ParameterStore(pName);
                DbTypeInfo info = DbTypeInfo.GetDbTypeInfo(typeStr);
                if (info == null)
                {
                    throw new ApplicationException(string.Format("不明な型です: {0}", typeStr));
                }
                param.DbType = info.DbType;
                if (string.IsNullOrEmpty(val) || string.Compare(val, "null", true) == 0)
                {
                    param.Value = DBNull.Value;
                }
                else
                {
                    val = DequoteStr(val);
                    param.Value = info.Parse(val);
                }
                Parameters.Add(param);
            }
        }
        public QueryStore(string sql, ParameterStoreCollection parameters)
        {
            _sql = sql;
            Parameters = new ParameterStoreCollection();
            foreach (ParameterStore p in parameters)
            {
                Parameters.Add(p.Clone());
            }
        }

        public void WriteToStream(TextWriter writer)
        {
            string s = Sql?.TrimEnd();
            writer.WriteLine(s);
            writer.WriteLine(ParamSeparator);
            foreach (ParameterStore ps in Parameters)
            {
                writer.Write("-- ");
                writer.Write(ps.ParameterName);
                writer.Write(": ");
                writer.Write(ps.DbType.ToString());
                writer.Write(" = ");
                if (ps.Value == null || ps.Value is DBNull)
                {
                    writer.Write("null");
                }
                else
                {
                    writer.Write(QuoteStr(ps.Text));
                }
                writer.WriteLine();
            }
            writer.WriteLine(ParamSeparator);
        }
    }
    public class QueryStoreCollection: IList, IList<QueryStore>
    {
        private Dictionary<string, QueryStore> _sqlToQuery;
        private List<QueryStore> _list = new List<QueryStore>();

        private void InvalidateSqlToQuery()
        {
            _sqlToQuery = null;
        }
        private void UpdateSqlToQuery()
        {
            if (_sqlToQuery != null)
            {
                return;
            }
            _sqlToQuery = new Dictionary<string, QueryStore>();
            foreach (QueryStore q in _list)
            {
                _sqlToQuery.Add(q.Sql, q);
            }
        }

        public void LoadFromStream(TextReader reader)
        {
            while (reader.Peek() != -1)
            {
                QueryStore q = new QueryStore(reader);
                _list.Add(q);
            }
        }

        public void SaveToStream(TextWriter writer)
        {
            foreach (QueryStore q in _list)
            {
                q.WriteToStream(writer);
            }
        }

        public QueryStore this[string sql]
        {
            get
            {
                UpdateSqlToQuery();
                QueryStore ret;
                if (!_sqlToQuery.TryGetValue(sql, out ret))
                {
                    return null;
                }
                return ret;
            }
        }

        #region IList, IList<QueryStore>の実装
        public QueryStore this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
                InvalidateSqlToQuery();
            }
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_list)[index];
            }

            set
            {
                ((IList)_list)[index] = value;
                InvalidateSqlToQuery();
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return ((IList)_list).IsFixedSize;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IList<QueryStore>)_list).IsReadOnly;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        public int Add(object value)
        {
            int ret = ((IList)_list).Add(value);
            InvalidateSqlToQuery();
            return ret;
        }

        public void Add(QueryStore item)
        {
            _list.Add(item);
            InvalidateSqlToQuery();
        }

        public void Clear()
        {
            _list.Clear();
            InvalidateSqlToQuery();
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public bool Contains(QueryStore item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public void CopyTo(QueryStore[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<QueryStore> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public int IndexOf(QueryStore item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
            InvalidateSqlToQuery();
        }

        public void Insert(int index, QueryStore item)
        {
            _list.Insert(index, item);
            InvalidateSqlToQuery();
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
            InvalidateSqlToQuery();
        }

        public bool Remove(QueryStore item)
        {
            bool ret = _list.Remove(item);
            InvalidateSqlToQuery();
            return ret;
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            InvalidateSqlToQuery();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }
}
