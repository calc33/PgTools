using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Db2Source
{
    public class QueryHistory: IReadOnlyList<QueryHistory.Query>
    {
        private const long UNASSIGNED_ID = long.MinValue;

        private string _dbFileName;
        private SqlCollection _sqlList;
        private List<Query> _queryList = new List<Query>();

        public int Count { get { return _queryList.Count; } }

        public Query this[int index] { get { return _queryList[index]; } }

        private static string GetDbFileName(ConnectionInfo info)
        {
            string s = info.GetDatabaseIdentifier();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }
            string filename = string.Format("History_{0}.db", s);
            return Path.Combine(Db2SourceContext.AppDataDir, filename);
        }

        public QueryHistory(ConnectionInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            _dbFileName = GetDbFileName(info);
            _sqlList = new SqlCollection(this);
        }

        public SQLiteConnection GetConnection()
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = _dbFileName,
                FailIfMissing = false,
                ReadOnly = false,
            };
            SQLiteConnection ret = new SQLiteConnection(builder.ToString());
            ret.Open();
            RequireDataTable(ret);
            return ret;
        }

        public void Fill()
        {
            _sqlList = new SqlCollection(this);
            _queryList = new List<Query>();

            using (SQLiteConnection conn = GetConnection())
            {
                _sqlList.Fill(conn);
                LoadQuery(conn, string.Empty, _queryList, false, null);
            }
        }

        public void Fill(DateTime? startTime, DateTime? endTime)
        {
            string additional = string.Empty;
            if (startTime.HasValue && endTime.HasValue)
            {
                additional = string.Format(" WHERE LAST_EXECUTED BETWEEN {0} AND {1}", startTime.Value.ToOADate(), endTime.Value.ToOADate());
            }
            else if (startTime.HasValue && !endTime.HasValue)
            {
                additional = string.Format(" WHERE LAST_EXECUTED >= {0}", startTime.Value.ToOADate());
            }
            else if (!startTime.HasValue && endTime.HasValue)
            {
                additional = string.Format(" WHERE LAST_EXECUTED < {0}", endTime.Value.ToOADate());
            }
            _sqlList = new SqlCollection(this);
            _queryList = new List<Query>();

            using (SQLiteConnection conn = GetConnection())
            {
                _sqlList.Fill(conn);
                LoadQuery(conn, additional, _queryList, false, null);
            }
        }

        public Query NewQuery(IDbCommand command)
        {
            string s = command.CommandText.TrimEnd();
            
            Sql sql = _sqlList.FindBySql(s) ?? new Sql(s);
            return new Query(sql, command.Parameters);
        }
        public Query AddHistory(Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            using (SQLiteConnection conn = GetConnection())
            {
                query.Sql = _sqlList.Require(query.SqlText, conn);
                Apply(query, conn);
            }
            return query;
        }

        private void Apply(Query query, SQLiteConnection connection)
        {
            if (query.Id == UNASSIGNED_ID)
            {
                List<Query> l = new List<Query>();
                LoadQuery(connection, " WHERE SQL_ID = @SQL_ID AND PARAM_HASH = @PARAM_HASH", l, true,
                    new SQLiteParameter[] { new SQLiteParameter("SQL_ID", DbType.Int64) { Value = query.SqlId }, new SQLiteParameter("PARAM_HASH", DbType.String) { Value = query.ParamHash } });
                foreach (Query q in l)
                {
                    if (query.ParamText == q.ParamText)
                    {
                        query.Id = q.Id;
                        break;
                    }
                }
            }
            query.LastExecuted = DateTime.Now;
            if (query.Id != UNASSIGNED_ID)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QueryHistory_UpdateSQL, connection))
                {
                    cmd.Parameters.Add(new SQLiteParameter("LAST_EXECUTED", DbType.Double) { Value = query.LastExecuted.ToOADate() });
                    cmd.Parameters.Add(new SQLiteParameter("ID", DbType.Int64) { Value = query.Id });
                    cmd.ExecuteNonQuery();
                }
                return;
            }
            using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QueryHistory_InsertSQL, connection))
            {
                cmd.Parameters.Add(new SQLiteParameter("SQL_ID", DbType.Int64) { Value = query.SqlId });
                cmd.Parameters.Add(new SQLiteParameter("PARAM_HASH", DbType.String) { Value = query.ParamHash });
                cmd.Parameters.Add(new SQLiteParameter("LAST_EXECUTED", DbType.Double) { Value = query.LastExecuted.ToOADate() });
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        query.Id = reader.GetInt64(0);
                    }
                }
            }
            foreach (Parameter p in query.Parameters)
            {
                p.SqlId = query.Id;
                p.Save(connection);
            }
            _queryList.Remove(query);
            _queryList.Insert(0, query);
        }

        public void LoadQuery(SQLiteConnection connection, string additionalSql, List<Query> queryList, bool loadParamById, SQLiteParameter[] parameters)
        {
            List<Query> list = new List<Query>();
            Dictionary<long, List<Parameter>> paramDict = new Dictionary<long, List<Parameter>>();
            using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QueryHistory_SQL + additionalSql, connection))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    int idxId = IndexOfField(reader, "ID");
                    int idxSqlId = IndexOfField(reader, "SQL_ID");
                    int idxLastEx = IndexOfField(reader, "LAST_EXECUTED");
                    while (reader.Read())
                    {
                        Query query = new Query() { Id = reader.GetInt64(idxId), Sql = _sqlList.FindById(reader.GetInt64(idxSqlId)), LastExecuted = DateTime.FromOADate(reader.GetDouble(idxLastEx)) };
                        list?.Add(query);
                        if (paramDict != null)
                        {
                            paramDict[query.Id] = new List<Parameter>();
                        }
                    }
                }
            }
            StringBuilder buf = new StringBuilder();
            if (loadParamById)
            {
                if (list.Count != 0)
                {
                    string prefix = " WHERE QUERY_ID IN (";
                    foreach (Query q in list)
                    {
                        buf.Append(prefix);
                        buf.Append(q.Id);
                        prefix = ",";
                    }
                    buf.Append(")");
                }
                else
                {
                    buf.Append(" WHERE 0=1");
                }
            }

            string sql = DataSet.Properties.Resources.QueryParameter_SQL + buf.ToString() + " ORDER BY ID";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    int idxId = IndexOfField(reader, "ID");
                    int idxSqlId = IndexOfField(reader, "QUERY_ID");
                    int idxName = IndexOfField(reader, "NAME");
                    int idxParamType = IndexOfField(reader, "PARAM_TYPE");
                    int idxValue = IndexOfField(reader, "VALUE");
                    while (reader.Read())
                    {
                        Parameter p = new Parameter()
                        {
                            Id = reader.GetInt64(idxId),
                            SqlId = reader.GetInt64(idxSqlId),
                            Name = reader.GetString(idxName),
                            DbType = (DbType)reader.GetInt32(idxParamType),
                            Value = DBNull.Value
                        };
                        try
                        {
                            p.StringValue = reader.GetString(idxValue);
                        }
                        catch
                        {
                            p.Value = DBNull.Value;
                        }
                        if (paramDict.TryGetValue(p.SqlId, out List<Parameter> prms))
                        {
                            prms.Add(p);
                        }
                    }
                }
            }
            foreach (Query q in list)
            {
                q.Parameters = paramDict[q.Id].ToArray();
            }
            queryList.AddRange(list);
            queryList.Sort(Query.CompareByLastExecuted);
        }

        private static int IndexOfField(SQLiteDataReader reader, string name)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Compare(reader.GetName(i), name, true) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        internal static string GetSHA1Hash(string value)
        {
            SHA1 sha = SHA1.Create();
            byte[] b = Encoding.UTF8.GetBytes(value);
            byte[] hash = sha.ComputeHash(b);
            return Convert.ToBase64String(hash);
        }

        private static readonly TableDefinition QuerySqlTable = new TableDefinition()
        {
            Name = "QUERY_SQL",
            Columns = new FieldDefinition[]
            {
                new FieldDefinition("ID", SqliteDbType.Integer){ AutoIncrement = true },
                new FieldDefinition("SQL", SqliteDbType.Text),
                new FieldDefinition("HASH", SqliteDbType.Text),
            },
            Indexes = new IndexDefinition[]
            {
                new IndexDefinition("QUERY_SQL_HASH", "QUERY_SQL", new string[] { "HASH" })
            }
        };

        private static readonly TableDefinition QueryHistoryTable = new TableDefinition()
        {
            Name = "QUERY_HISTORY",
            Columns = new FieldDefinition[]
            {
                new FieldDefinition("ID", SqliteDbType.Integer) { AutoIncrement = true },
                new FieldDefinition("SQL_ID", SqliteDbType.Integer) { NotNull = true },
                new FieldDefinition("LAST_EXECUTED", SqliteDbType.Real),
                new FieldDefinition("PARAM_HASH", SqliteDbType.Text),
            },
            Indexes = new IndexDefinition[]
            {
                new IndexDefinition("QUERY_HISTORY_SQL_HASH", "QUERY_HISTORY", new string[] { "SQL_ID" })
            }
        };

        private static readonly TableDefinition QueryParameterTable = new TableDefinition()
        {
            Name = "QUERY_PARAMETER",
            Columns = new FieldDefinition[]
            {
                new FieldDefinition("ID", SqliteDbType.Integer){ AutoIncrement = true },
                new FieldDefinition("QUERY_ID", SqliteDbType.Integer){ NotNull = true },
                new FieldDefinition("NAME", SqliteDbType.Text),
                new FieldDefinition("PARAM_TYPE", SqliteDbType.Integer),
                new FieldDefinition("VALUE", SqliteDbType.Text),
            },
            Indexes = new IndexDefinition[]
            {
                new IndexDefinition("QUERY_PARAMETER_QUERY_ID", "QUERY_PARAMETER", new string[] { "QUERY_ID" })
            }
        };
        private void RequireDataTable(SQLiteConnection connection)
        {
            QuerySqlTable.Apply(connection);
            QueryHistoryTable.Apply(connection);
            QueryParameterTable.Apply(connection);
        }

        public IEnumerator<Query> GetEnumerator()
        {
            return ((IEnumerable<Query>)_queryList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_queryList).GetEnumerator();
        }

        public class Sql
        {
            internal long Id { get; set; } = UNASSIGNED_ID;
            public string Text { get; private set; }

            public string Hash { get; private set; }

            internal Sql(string sql)
            {
                Text = sql.TrimEnd();
                Hash = GetSHA1Hash(Text);
            }

            internal Sql(long id, string sql)
            {
                Id = id;
                Text = sql.TrimEnd();
                Hash = GetSHA1Hash(Text);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Sql))
                {
                    return false;
                }
                return Id == ((Sql)obj).Id;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
            public override string ToString()
            {
                return Text;
            }
            public static int CompareByHash(Sql item1, Sql item2)
            {
                if (item1 == null || item2 == null)
                {
                    return ((item1 != null) ? 0 : 1) - ((item2 != null) ? 0 : 1);
                }
                int ret = string.Compare(item1.Hash, item2.Hash);
                if (ret != 0)
                {
                    return ret;
                }
                return string.Compare(item1.Text, item2.Text);
            }
        }

        public class SqlCollection: IList<Sql>
        {
            private readonly QueryHistory _owner;
            private List<Sql> _list = new List<Sql>();
            private Dictionary<long, Sql> _idToSql = null;
            private bool _isValid = false;
            private void Invalidate()
            {
                _isValid = false;
                _idToSql = null;
            }
            private void Update()
            {
                if (_isValid)
                {
                    return;
                }
                _list.Sort(Sql.CompareByHash);
                _idToSql = new Dictionary<long, Sql>();
                foreach (Sql sql in _list)
                {
                    _idToSql[sql.Id] = sql;
                }
            }

            private int FindIndex(Sql target, int index0, int index1)
            {
                if (index1 < index0)
                {
                    return -1;
                }
                int i = (index0 + index1) / 2;
                int ret = Math.Sign(Sql.CompareByHash(target, this[i]));
                switch (ret)
                {
                    case 0:
                        return i;
                    case 1:
                        return FindIndex(target, i + 1, index1);
                    case -1:
                        return FindIndex(target, index0, i - 1);
                    default:
                        throw new NotImplementedException();
                }
            }

            public Sql FindBySql(string sql)
            {
                Update();
                int i = FindIndex(new Sql(sql), 0, Count - 1);
                if (i == -1)
                {
                    return null;
                }
                return this[i];
            }

            public Sql FindById(long id)
            {
                Update();
                if (!_idToSql.TryGetValue(id, out Sql sql))
                {
                    return null;
                }
                return sql;
            }

            private static void Load(SQLiteConnection connection, string additionalSql, IList<Sql> sqlList, SQLiteParameter[] parameters)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QuerySql_SQL + additionalSql, connection))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        int idxId = IndexOfField(reader, "ID");
                        int idxSql = IndexOfField(reader, "SQL");
                        while (reader.Read())
                        {
                            Sql sql = new Sql(reader.GetInt64(idxId), reader.GetString(idxSql));
                            sqlList.Add(sql);
                        }
                    }
                }
            }

            public Sql Require(string sql, SQLiteConnection connection)
            {
                string s = sql.TrimEnd();
                Sql q = FindBySql(s);
                if (q != null)
                {
                    return q;
                }
                List<Sql> l = new List<Sql>();
                Load(connection, " WHERE HASH = @HASH", l, new SQLiteParameter[] { new SQLiteParameter("HASH", DbType.String) { Value = GetSHA1Hash(s) } });
                foreach (Sql q2 in l)
                {
                    if (q2.Text == s)
                    {
                        Add(q2);
                        return q2;
                    }
                }
                q = new Sql(s);
                using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QuerySql_InsertSQL, connection))
                {
                    cmd.Parameters.Add(new SQLiteParameter("SQL", DbType.String) { Value = q.Text });
                    cmd.Parameters.Add(new SQLiteParameter("HASH", DbType.String) { Value = q.Hash });
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            q.Id = reader.GetInt64(0);
                        }
                    }
                }
                Add(q);
                return q;
            }
            public Sql Require(string sql)
            {
                using (SQLiteConnection conn = _owner.GetConnection())
                {
                    return Require(sql, conn);
                }
            }

            public void Fill(SQLiteConnection connection)
            {
                Load(connection, string.Empty, this, null);
            }

            public void Fill()
            {
                using (SQLiteConnection conn = _owner.GetConnection())
                {
                    Fill(conn);
                }
            }

            public Sql this[int index]
            {
                get
                {
                    Update();
                    return _list[index];
                }
                set
                {
                    _list[index] = value;
                    Invalidate();
                }
            }

            public int Count { get { return _list.Count; } }

            public bool IsReadOnly { get { return false; } }

            public void Add(Sql item)
            {
                _list.Add(item);
                Invalidate();
            }

            public void Clear()
            {
                _list.Clear();
                Invalidate();
            }

            public bool Contains(Sql item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(Sql[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Sql> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            public int IndexOf(Sql item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, Sql item)
            {
                _list.Insert(index, item);
                Invalidate();
            }

            public bool Remove(Sql item)
            {
                bool ret = _list.Remove(item);
                Invalidate();
                return ret;
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                Invalidate();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_list).GetEnumerator();
            }

            public SqlCollection(QueryHistory owner)
            {
                _owner = owner;
            }
        }

        public class Query
        {
            internal long Id { get; set; } = UNASSIGNED_ID;
            private long _sqlId = UNASSIGNED_ID;
            public long SqlId
            {
                get
                {
                    return (Sql != null) ? Sql.Id : _sqlId;
                }
                set
                {
                    if (Sql != null)
                    {
                        return;
                    }
                    _sqlId = value;
                }
            }
            public Sql Sql { get; set; }
            public string SqlText
            {
                get
                {
                    return Sql?.Text;
                }
            }

            public string ParamHash
            {
                get
                {
                    return GetSHA1Hash(ParamText);
                }
            }

            private string _paramText = null;
            private void InvalidateParamText()
            {
                _paramText = null;
            }

            private void UpdateParamText()
            {
                if (_paramText != null)
                {
                    return;
                }
                StringBuilder buf = new StringBuilder();
                foreach (Parameter p in Parameters)
                {
                    buf.Append(p.Name);
                    buf.Append(':');
                    buf.Append(p.DbType.ToString());
                    buf.Append('=');
                    buf.AppendLine(p.StringValue);
                }
                _paramText = buf.ToString();
            }

            public string ParamText
            {
                get
                {
                    UpdateParamText();
                    return _paramText;
                }
            }

            public DateTime LastExecuted { get; set; } = DateTime.Now;

            private Parameter[] _parameters;
            public Parameter[] Parameters
            {
                get { return _parameters; }
                internal set
                {
                    _parameters = value;
                    InvalidateParamText();
                }
            }

            public Query() { }
            public Query(IDbCommand command)
            {
                if (command == null)
                {
                    throw new ArgumentNullException("command");
                }
                Sql = new Sql(command.CommandText.TrimEnd());
                List<Parameter> l = new List<Parameter>();
                foreach (IDataParameter p in command.Parameters)
                {
                    l.Add(new Parameter(p));
                }
                Parameters = l.ToArray();
            }
            public Query(Sql sql, IDataParameterCollection parameters)
            {
                Sql = sql;
                List<Parameter> l = new List<Parameter>();
                foreach (IDataParameter p in parameters)
                {
                    l.Add(new Parameter(p));
                }
                Parameters = l.ToArray();
            }

            public void SetParameters(IDataParameterCollection parameters)
            {
                foreach (Parameter src in parameters)
                {
                    try
                    {
                        IDbDataParameter dest = parameters[src.Name] as IDbDataParameter;
                        dest.DbType = src.DbType;
                        dest.Value = src.Value;
                    }
                    catch (IndexOutOfRangeException) { }
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Query))
                {
                    return false;
                }
                Query q = (Query)obj;
                if (Id != UNASSIGNED_ID && q.Id != UNASSIGNED_ID)
                {
                    return Id == q.Id;
                }
                return string.Equals(SqlText, q.SqlText) && string.Equals(ParamText, q.ParamText);
            }

            public override int GetHashCode()
            {
                return Sql.Hash.GetHashCode() * 13 + ParamHash.GetHashCode();
            }

            public override string ToString()
            {
                return SqlText;
            }

            public static int CompareByLastExecuted(Query item1, Query item2)
            {
                if (item1 == null || item2 == null)
                {
                    return (item1 != null ? 0 : 1) - (item2 != null ? 0 : 1);
                }
                int ret = -DateTime.Compare(item1.LastExecuted, item2.LastExecuted);
                if (ret != 0)
                {
                    return ret;
                }
                return item1.Id.CompareTo(item2.Id);
            }
        }

        public class Parameter
        {
            internal long Id { get; set; } = UNASSIGNED_ID;
            internal long SqlId { get; set; } = UNASSIGNED_ID;
            public string Name { get; set; }
            public DbType DbType { get; set; }
            public object Value { get; set; }

            public string StringValue
            {
                get
                {
                    return ToStringValue(Value, DbType);
                }
                set
                {
                    Value = Parse(value, DbType);
                }
            }

            public Parameter() { }
            public Parameter(IDataParameter parameter)
            {
                Name = parameter.ParameterName;
                DbType = parameter.DbType;
                if (parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input)
                {
                    Value = parameter.Value;
                }
                else
                {
                    Value = null;
                }
            }

            internal void Save(SQLiteConnection connection)
            {
                if (Id != UNASSIGNED_ID)
                {
                    return;
                }
                using (SQLiteCommand cmd = new SQLiteCommand(DataSet.Properties.Resources.QueryParameter_InsertSQL, connection))
                {
                    cmd.Parameters.Add(new SQLiteParameter("QUERY_ID", DbType.Int64) { Value = SqlId });
                    cmd.Parameters.Add(new SQLiteParameter("NAME", DbType.String) { Value = Name });
                    cmd.Parameters.Add(new SQLiteParameter("PARAM_TYPE", DbType.Int32) { Value = (int)DbType });
                    cmd.Parameters.Add(new SQLiteParameter("VALUE", DbType.String) { Value = StringValue });
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Id = reader.GetInt64(0);
                        }
                    }
                }
            }

            private const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss.ffffff";
            private const string DateTimeOffsetFormat = "yyyy/MM/dd HH:mm:ss.ffffffzzz";
            public static object Parse(string value, DbType type)
            {
                if (value == null)
                {
                    return DBNull.Value;
                }
                switch (type)
                {
                    case DbType.String:
                    case DbType.AnsiString:
                    case DbType.AnsiStringFixedLength:
                        return value;
                    case DbType.Byte:
                        return byte.Parse(value);
                    case DbType.SByte:
                        return sbyte.Parse(value);
                    case DbType.Int16:
                        return short.Parse(value);
                    case DbType.UInt16:
                        return ushort.Parse(value);
                    case DbType.Int32:
                        return int.Parse(value);
                    case DbType.UInt32:
                        return uint.Parse(value);
                    case DbType.Int64:
                        return long.Parse(value);
                    case DbType.UInt64:
                        return ulong.Parse(value);
                    case DbType.Boolean:
                        return bool.Parse(value);
                    case DbType.Single:
                        return float.Parse(value);
                    case DbType.Double:
                        return double.Parse(value);
                    case DbType.Currency:
                    case DbType.Decimal:
                    case DbType.VarNumeric:
                        return decimal.Parse(value);
                    case DbType.Guid:
                        return Guid.Parse(value);
                    case DbType.Date:
                    case DbType.DateTime:
                    case DbType.DateTime2:
                        return DateTime.ParseExact(value, DateTimeFormat, CultureInfo.CurrentCulture);
                    case DbType.DateTimeOffset:
                        return DateTimeOffset.ParseExact(value, DateTimeOffsetFormat, CultureInfo.CurrentCulture);
                        //case DbType.Object:
                        //case DbType.Time:
                        //case DbType.Xml:
                }
                throw new NotImplementedException(string.Format("UnsupportedDbType: {0}", type.ToString()));
            }
            public static string ToStringValue(object value, DbType type)
            {
                if (value == null || value is DBNull)
                {
                    return null;
                }
                switch (type)
                {
                    case DbType.String:
                    case DbType.AnsiString:
                    case DbType.AnsiStringFixedLength:
                    case DbType.Byte:
                    case DbType.SByte:
                    case DbType.Int16:
                    case DbType.UInt16:
                    case DbType.Int32:
                    case DbType.UInt32:
                    case DbType.Int64:
                    case DbType.UInt64:
                    case DbType.Boolean:
                    case DbType.Single:
                    case DbType.Double:
                    case DbType.Currency:
                    case DbType.Decimal:
                    case DbType.VarNumeric:
                    case DbType.Guid:
                        return value.ToString();
                    case DbType.Date:
                    case DbType.DateTime:
                    case DbType.DateTime2:
                        if (value is DateTime time)
                        {
                            return time.ToString(DateTimeFormat, CultureInfo.CurrentCulture);
                        }
                        break;
                    case DbType.DateTimeOffset:
                        if (value is DateTimeOffset offset)
                        {
                            return offset.ToString(DateTimeOffsetFormat, CultureInfo.CurrentCulture);
                        }
                        break;
                    //case DbType.Object:
                    //case DbType.Time:
                    //case DbType.Xml:
                }
                throw new NotImplementedException(string.Format("Unmatched type: {0}", type.ToString()));
            }
        }
    }
}
