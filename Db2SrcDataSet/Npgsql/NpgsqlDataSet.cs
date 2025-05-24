using Db2Source.DataSet.Properties;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Db2Source
{
    public partial class NpgsqlDataSet : Db2SourceContext
    {
        public enum ParameterDir
        {
            Input = 1,
            Output = 2,
            InputOutput = 3,
            VariaDic = 4,
            Table = 5,
            ReturnValue = 6
        }
        public new PgsqlDatabase Database
        {
            get { return base.Database as PgsqlDatabase; }
            set { base.Database = value; }
        }
        public NamedCollection<PgsqlDatabase> OtherDatabases { get; } = new NamedCollection<PgsqlDatabase>();
        public NamedCollection<PgsqlDatabase> DatabaseTemplates { get; } = new NamedCollection<PgsqlDatabase>();

        //public PgsqlDatabase[] OtherDatabases { get; set; }
        //public PgsqlDatabase[] DatabaseTemplates { get; set; }
        public NpgsqlDataSet(NpgsqlConnectionInfo info) : base(info) { }

        public override IDbConnection NewConnection(bool withOpening)
        {
            NpgsqlConnection ret = base.NewConnection(withOpening) as NpgsqlConnection;
            ret.Notice += NpgsqlConnection_Notice;
            return ret;
        }

        private void NpgsqlConnection_Notice(object sender, NpgsqlNoticeEventArgs e)
        {
            //OnLog(e.Notice.Detail, LogStatus.Aux, null);
        }

        public static readonly Dictionary<string, NpgsqlDbType> TypeNameToDbType = new Dictionary<string, NpgsqlDbType>()
        {
            { "boolean", NpgsqlDbType.Boolean },
            { "bool", NpgsqlDbType.Boolean },
            { "smallint", NpgsqlDbType.Smallint },
            { "integer", NpgsqlDbType.Integer },
            { "int2", NpgsqlDbType.Smallint },
            { "int4", NpgsqlDbType.Integer },
            { "bigint", NpgsqlDbType.Bigint },
            { "int8", NpgsqlDbType.Bigint },
            { "real", NpgsqlDbType.Real },
            { "float4", NpgsqlDbType.Real },
            { "money", NpgsqlDbType.Money },

            //可変長
            { "text", NpgsqlDbType.Text },
            { "citext", NpgsqlDbType.Citext },
            { "json", NpgsqlDbType.Json },
            { "jsonb", NpgsqlDbType.Jsonb },
            { "xml", NpgsqlDbType.Xml },

            //固定長あり
            { "character varying", NpgsqlDbType.Varchar },
            { "nvarchar", NpgsqlDbType.Varchar },
            { "varchar", NpgsqlDbType.Varchar },
            { "character", NpgsqlDbType.Char },
            { "char", NpgsqlDbType.Char },
            { "\"char\"", NpgsqlDbType.Char },
            { "name", NpgsqlDbType.Name },
            //固定桁数あり
            { "double precision", NpgsqlDbType.Double },
            { "numeric", NpgsqlDbType.Numeric },

            { "point", NpgsqlDbType.Point },
            { "lseg", NpgsqlDbType.LSeg },
            { "path", NpgsqlDbType.Path },
            { "polygon", NpgsqlDbType.Polygon },
            { "line", NpgsqlDbType.Line },
            { "circle", NpgsqlDbType.Circle },
            { "box", NpgsqlDbType.Box },
            { "bit", NpgsqlDbType.Bit },

            // 固定桁数あり
            { "bit varying", NpgsqlDbType.Varbit },
            { "hstore", NpgsqlDbType.Hstore },
            { "uuid", NpgsqlDbType.Uuid },
            { "inet", NpgsqlDbType.Inet },
            { "macaddr", NpgsqlDbType.MacAddr },
            { "tsquery", NpgsqlDbType.TsQuery },
            { "tsvector", NpgsqlDbType.TsVector },
            //{ "abstime", NpgsqlDbType.Abstime },
            { "date", NpgsqlDbType.Date },
            { "timestamp", NpgsqlDbType.Timestamp },
            { "timestamp without time zone", NpgsqlDbType.Timestamp },
            { "timestamp with time zone", NpgsqlDbType.TimestampTz },
            { "time", NpgsqlDbType.Time },
            { "interval", NpgsqlDbType.Interval },
            { "time with time zone", NpgsqlDbType.TimeTz },

            { "bytea", NpgsqlDbType.Bytea },
            { "oid", NpgsqlDbType.Oid },
            { "xid", NpgsqlDbType.Xid },
            { "cid", NpgsqlDbType.Cid },

            // 固定長あり
            { "oidvector", NpgsqlDbType.Oidvector },
            //{ "record", NpgsqlDbType.Record },
            { "array", NpgsqlDbType.Array },
            //{ "void", null },

            //配列
            { "_xml", NpgsqlDbType.Array | NpgsqlDbType.Xml },
            { "_json", NpgsqlDbType.Array | NpgsqlDbType.Json },
            { "_line", NpgsqlDbType.Array | NpgsqlDbType.Line },
            { "_circle", NpgsqlDbType.Array | NpgsqlDbType.Circle },
            { "_money", NpgsqlDbType.Array | NpgsqlDbType.Money },
            { "_bool", NpgsqlDbType.Array | NpgsqlDbType.Boolean },
            { "_bytea", NpgsqlDbType.Array | NpgsqlDbType.Bytea },
            { "_char", NpgsqlDbType.Array | NpgsqlDbType.Char },
            { "_name", NpgsqlDbType.Array | NpgsqlDbType.Name },
            { "_int2", NpgsqlDbType.Array | NpgsqlDbType.Smallint },
            { "int2vector", NpgsqlDbType.Int2Vector },
            { "_int2vector", NpgsqlDbType.Array | NpgsqlDbType.Int2Vector },
            { "_int4", NpgsqlDbType.Array | NpgsqlDbType.Integer },
            //{ "_regproc", typeof(string[]) },
            { "_text", NpgsqlDbType.Array | NpgsqlDbType.Text },
            { "_oid", NpgsqlDbType.Array | NpgsqlDbType.Oid },
            { "_tid", NpgsqlDbType.Array | NpgsqlDbType.Tid },
            { "_xid", NpgsqlDbType.Array | NpgsqlDbType.Xid },
            { "_cid", NpgsqlDbType.Array | NpgsqlDbType.Cid },
            { "_oidvector", NpgsqlDbType.Array | NpgsqlDbType.Oidvector },
            { "_bpchar", NpgsqlDbType.Array | NpgsqlDbType.Char },
            { "_varchar", NpgsqlDbType.Array | NpgsqlDbType.Varchar },
            { "_int8", NpgsqlDbType.Array | NpgsqlDbType.Bigint },
            { "_point", NpgsqlDbType.Array | NpgsqlDbType.Point },
            { "_lseg", NpgsqlDbType.Array | NpgsqlDbType.LSeg },
            { "_path", NpgsqlDbType.Array | NpgsqlDbType.Path },
            { "_box", NpgsqlDbType.Array | NpgsqlDbType.Box },
            { "_float4", NpgsqlDbType.Array | NpgsqlDbType.Real },
            { "_float8", NpgsqlDbType.Array | NpgsqlDbType.Double },
            //{ "_abstime", NpgsqlDbType.Array | NpgsqlDbType.Abstime },
            //{ "_reltime", NpgsqlDbType.Array | NpgsqlDbType.Timestamp },
            //{ "_tinterval", NpgsqlDbType.Array | NpgsqlDbType.Interval },
            { "_polygon", NpgsqlDbType.Array | NpgsqlDbType.Polygon },
            //{ "_aclitem", typeof(string[]) },
            { "_macaddr", NpgsqlDbType.Array | NpgsqlDbType.MacAddr },
            { "_inet", NpgsqlDbType.Array | NpgsqlDbType.Inet },
            { "_cidr", NpgsqlDbType.Array | NpgsqlDbType.Cidr },
            { "_cstring", NpgsqlDbType.Array | NpgsqlDbType.Varchar },
            { "_timestamp", NpgsqlDbType.Array | NpgsqlDbType.Timestamp },
            { "_date", NpgsqlDbType.Array | NpgsqlDbType.Date },
            { "_time", NpgsqlDbType.Array | NpgsqlDbType.Time },
            { "_timestamptz", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz },
            { "_interval", NpgsqlDbType.Array | NpgsqlDbType.Interval },
            { "_numeric", NpgsqlDbType.Array | NpgsqlDbType.Numeric },
            { "_timetz", NpgsqlDbType.Array | NpgsqlDbType.TimeTz },
            { "_bit", NpgsqlDbType.Array | NpgsqlDbType.Bit },
            { "_varbit", NpgsqlDbType.Array | NpgsqlDbType.Varbit },
            { "_refcursor", NpgsqlDbType.Array | NpgsqlDbType.Refcursor },
            //{ "_regprocedure", NpgsqlDbType.Array | NpgsqlDbType. },
            //{ "_regoper", NpgsqlDbType.Array | NpgsqlDbType. },
            //{ "_regoperator", NpgsqlDbType.Array | NpgsqlDbType. },
            //{ "_regclass", NpgsqlDbType.Array | NpgsqlDbType. },
            { "_regtype", NpgsqlDbType.Array | NpgsqlDbType.Regtype },
            //{ "_regrole", NpgsqlDbType.Array | NpgsqlDbType. },
            //{ "_regnamespace", NpgsqlDbType.Array | NpgsqlDbType. },
            { "_uuid", NpgsqlDbType.Array | NpgsqlDbType.Uuid },
            { "_pg_lsn", NpgsqlDbType.Array | NpgsqlDbType.PgLsn },
            { "_tsvector", NpgsqlDbType.Array | NpgsqlDbType.TsVector },
            { "_gtsvector", NpgsqlDbType.Array | NpgsqlDbType.TsVector },
            { "_tsquery", NpgsqlDbType.Array | NpgsqlDbType.TsQuery },
            { "_regconfig", NpgsqlDbType.Array | NpgsqlDbType.Regconfig },
            //{ "_regdictionary", NpgsqlDbType.Array | NpgsqlDbType. },
            { "_jsonb", NpgsqlDbType.Array | NpgsqlDbType.Jsonb },
            //{ "_txid_snapshot", NpgsqlDbType.Array | NpgsqlDbType. },
            { "_int4range", NpgsqlDbType.Array | NpgsqlDbType.IntegerRange },
            { "_numrange", NpgsqlDbType.Array | NpgsqlDbType.NumericRange },
            { "_tsrange", NpgsqlDbType.Array | NpgsqlDbType.TimestampRange },
            { "_tstzrange", NpgsqlDbType.Array | NpgsqlDbType.TimestampTzRange },
            { "_daterange", NpgsqlDbType.Array | NpgsqlDbType.DateRange },
            { "_int8range", NpgsqlDbType.Array | NpgsqlDbType.BigIntRange },
            //{ "_record", NpgsqlDbType.Array | NpgsqlDbType. },
        };

        public new static readonly Dictionary<Type, NpgsqlDbType> TypeToDbType = new Dictionary<Type, NpgsqlDbType>()
        {
            { typeof(string), NpgsqlDbType.Text },
            { typeof(bool), NpgsqlDbType.Boolean },
            { typeof(byte), NpgsqlDbType.Smallint },
            { typeof(sbyte), NpgsqlDbType.Smallint },
            { typeof(short), NpgsqlDbType.Smallint },
            { typeof(ushort), NpgsqlDbType.Smallint },
            { typeof(int), NpgsqlDbType.Integer },
            { typeof(uint), NpgsqlDbType.Oid },
            { typeof(long), NpgsqlDbType.Bigint },
            { typeof(ulong), NpgsqlDbType.Bigint },
            { typeof(DateTime), NpgsqlDbType.Timestamp },
            { typeof(TimeSpan), NpgsqlDbType.Interval },
            { typeof(float), NpgsqlDbType.Real },
            { typeof(double), NpgsqlDbType.Double },
            { typeof(decimal), NpgsqlDbType.Numeric },
        };

        private static Dictionary<string, bool> InitPgsqlReservedDict()
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
            foreach (string s in DataSet.Properties.Resources.PostgresReservedWords.Split('\r', '\n'))
            {
                string k = s.Trim();
                if (string.IsNullOrEmpty(k))
                {
                    continue;
                }
                ret.Add(k, true);
            }
            return ret;
        }
        private static readonly Dictionary<string, bool> PgsqlReservedDict = InitPgsqlReservedDict();
        private static readonly Dictionary<bool, Regex> PgSqlIdentifierRegex = new Dictionary<bool, Regex>() {
            { false, new Regex("^[_A-z][_$0-9A-z]*$") },
            { true, new Regex("^[_a-z][_$0-9a-z]*$") }
        };
        // スキーマ情報読込時になぜか大文字で返されるキーワード一覧
        private static readonly Dictionary<string, bool> UpperCaseEmbeddedWords = new Dictionary<string, bool>() {
            { "CURRENT_DATE", true},
            { "CURRENT_TIME", true},
            { "CURRENT_TIMESTAMP", true}
        };

        public static bool IsReservedWord(string value)
        {
            return PgsqlReservedDict.ContainsKey(value.ToUpper());
        }

        /// <summary>
        /// valueで渡された文字列が識別子として使う場合に引用符でエスケープする必要があるかどうかを判定する
        /// エスケープが必要な場合はtrue、不要な場合はfalseを返す
        /// </summary>
        /// <param name="value"></param>
        /// <param name="strict">
        /// false: 文字列はユーザーが入力した文字列なので大文字小文字の区別が曖昧である
        /// true: 文字列はDBの定義情報を渡しているので大文字小文字の区別が厳格(大文字が渡された場合も引用符でエスケープする)
        ///       なお、一部ワード(CURRENT_DATE等)はDBが定義情報を大文字で返すため例外扱いする
        /// </param>
        /// <returns></returns>
        public static bool NeedQuotedPgsqlIdentifier(string value, bool strict)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (strict && UpperCaseEmbeddedWords.ContainsKey(value))
            {
                return false;
            }
            return !PgSqlIdentifierRegex[strict].IsMatch(value) || IsReservedWord(value);
        }

        public override bool NeedQuotedIdentifier(string value, bool strict)
        {
            return NeedQuotedPgsqlIdentifier(value, strict);
        }
        public static string GetEscapedPgsqlIdentifier(string objectName, bool strict)
        {
            if (!IsQuotedIdentifier(objectName) && NeedQuotedPgsqlIdentifier(objectName, strict))
            {
                return GetQuotedIdentifier(objectName);
            }
            return objectName;
        }

        private static readonly Dictionary<string, PropertyInfo> BaseTypeToProp = new Dictionary<string, PropertyInfo>()
        {
            { "date", typeof(Db2SourceContext).GetProperty("DateFormat") },
            { "timestamp", typeof(Db2SourceContext).GetProperty("DateTimeFormat") },
        };
        public override Dictionary<string, PropertyInfo> BaseTypeToProperty
        {
            get
            {
                return BaseTypeToProp;
            }
        }

        private static readonly Dictionary<string, bool> HiddenSchemaNames = new Dictionary<string, bool>()
        {
            {"information_schema", true },
            {"pg_catalog", true },
            {"pg_toast", true }
        };

        protected internal override bool IsHiddenSchema(string schemaName)
        {
            if (HiddenSchemaNames.TryGetValue(schemaName, out bool ret))
            {
                return ret;
            }
            string sc = schemaName.ToLower();
            ret = (sc == "information_schema" || sc.StartsWith("pg_"));
            HiddenSchemaNames.Add(schemaName, ret);
            return ret;
        }

        private static List<string> GetParameterNames(string sql)
        {
            List<string> l = new List<string>();
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            TokenizedPgsql tsql = new TokenizedPgsql(sql);
            IEnumerator<Token> enumerator = tsql.GetEnumerator();
            do
            {
                while (enumerator.MoveNext() && enumerator.Current != null && ((PgsqlToken)enumerator.Current).ID != TokenID.Colon) ;

                if (enumerator.MoveNext() && enumerator.Current.Kind == TokenKind.Identifier)
                {
                    string p = DequoteIdentifier(enumerator.Current.Value);
                    if (!dict.ContainsKey(p))
                    {
                        l.Add(p);
                        dict.Add(p, true);
                    }
                }
            }
            while (enumerator.Current != null);
            return l;
        }

        private class LogConverter
        {
            public EventHandler<LogEventArgs> Log;
            private void OnLog(object sender, string message)
            {
                Log?.Invoke(sender, new LogEventArgs(message, LogStatus.Aux, null));
            }
            public void Notice(object sender, NpgsqlNoticeEventArgs e)
            {
                OnLog(sender, e.Notice.MessageText);
            }
            public static LogConverter NewLogConverter(NpgsqlCommand command, EventHandler<LogEventArgs> logEvent)
            {
                if (logEvent == null)
                {
                    return null;
                }
                LogConverter conv = new LogConverter()
                {
                    Log = logEvent
                };
                command.Connection.Notice += conv.Notice;
                return conv;
            }
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
        public override IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection)
        {
            if (connection != null && !(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connectoin");
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            NpgsqlCommand cmd = new NpgsqlCommand(sqlCommand, connection as NpgsqlConnection);
            LogConverter.NewLogConverter(cmd, logEvent);
            foreach (string s in GetParameterNames(sqlCommand))
            {
                NpgsqlParameter p = new NpgsqlParameter(s, DbType.String)
                {
                    Direction = ParameterDirection.InputOutput
                };
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
        public override IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction)
        {
            if (connection != null && !(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connectoin");
            }
            if ((transaction != null) && !(transaction is NpgsqlTransaction))
            {
                throw new ArgumentException("transaction");
            }
            NpgsqlConnection conn = (NpgsqlConnection)connection;
            NpgsqlCommand cmd = new NpgsqlCommand(sqlCommand, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
            LogConverter.NewLogConverter(cmd, logEvent);
            foreach (string s in GetParameterNames(sqlCommand))
            {
                NpgsqlParameter p = new NpgsqlParameter(s, DbType.String)
                {
                    Direction = ParameterDirection.InputOutput
                };
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
        public override IDbCommand GetSqlCommand(StoredFunction function, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction)
        {
            if (connection != null && !(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connectoin");
            }
            if ((transaction != null) && !(transaction is NpgsqlTransaction))
            {
                throw new ArgumentException("transaction");
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("select * from ");
            buf.Append(GetEscapedIdentifier(function.SchemaName, function.Name, null, true));
            buf.Append("(");
            bool needComma = false;
            List<NpgsqlParameter> l = new List<NpgsqlParameter>();
            foreach (Parameter p in function.Parameters)
            {
                if (p.Direction == ParameterDirection.Output)
                {
                    continue;
                }
                string pName = p.ParameterName;
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(':');
                buf.Append(pName);
                buf.Append("::");
                buf.Append(p.BaseType);
                NpgsqlDbType t;
                if (!TypeNameToDbType.TryGetValue(p.BaseType, out t) && !TypeToDbType.TryGetValue(p.ValueType, out t))
                {
                    t = NpgsqlDbType.Text;
                }
                NpgsqlParameter np = new NpgsqlParameter(pName, t)
                {
                    Direction = p.Direction
                };
                l.Add(np);
                needComma = true;
            }
            buf.Append(")");
            NpgsqlCommand cmd = new NpgsqlCommand(buf.ToString(), connection as NpgsqlConnection, transaction as NpgsqlTransaction)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddRange(l.ToArray());
            LogConverter.NewLogConverter(cmd, logEvent);
            return cmd;
        }

        public override IDbCommand GetSqlCommand(StoredProcedure procedure, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction)
        {
            if (connection != null && !(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connectoin");
            }
            if ((transaction != null) && !(transaction is NpgsqlTransaction))
            {
                throw new ArgumentException("transaction");
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("call ");
            buf.Append(GetEscapedIdentifier(procedure.SchemaName, procedure.Name, null, true));
            buf.Append("(");
            bool needComma = false;
            List<NpgsqlParameter> l = new List<NpgsqlParameter>();
            foreach (Parameter p in procedure.Parameters)
            {
                if (p.Direction == ParameterDirection.Output)
                {
                    continue;
                }
                string pName = p.ParameterName;
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(':');
                buf.Append(pName);
                //buf.Append("::");
                //buf.Append(p.BaseType);
                NpgsqlDbType t;
                if (!TypeNameToDbType.TryGetValue(p.BaseType, out t) && !TypeToDbType.TryGetValue(p.ValueType, out t))
                {
                    t = NpgsqlDbType.Text;
                }
                NpgsqlParameter np = new NpgsqlParameter(pName, t)
                {
                    Direction = p.Direction
                };
                l.Add(np);
                needComma = true;
            }
            buf.Append(")");
            NpgsqlCommand cmd = new NpgsqlCommand(buf.ToString(), connection as NpgsqlConnection, transaction as NpgsqlTransaction)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddRange(l.ToArray());
            LogConverter.NewLogConverter(cmd, logEvent);
            return cmd;
        }

        public override DataTable GetDataTable(string tableName, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connectoin");
            }
            if (!(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connectoin");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            DataTable tbl = new DataTable(tableName);

            return tbl;
        }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
        private void ExecuteSql(string sql, NpgsqlParameter[] paramList, IChangeSet owner, IChangeSetRow row, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            LastSql = sql;
            LastParameter = paramList;
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddRange(paramList);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                if (reader.FieldCount == 0)
                {
                    return;
                }
                int[] idxList = new int[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    ColumnInfo fi = owner.GetFieldByName(reader.GetName(i));
                    idxList[i] = (fi != null) ? fi.Index : -1;
                }
                row.Read(reader, idxList);
            }
        }
        private static NpgsqlDbType GetNpgsqlDbType(ColumnInfo info)
        {
            Type ft = info.FieldType;
            Type t = ft;
            bool isArray = false;
            if (ft.IsArray)
            {
                isArray = true;
                t = ft.GetElementType();
            }
            if (ft.IsGenericType && ft.GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>)))
            {
                isArray = true;
                t = ft.GetGenericArguments()[0];
            }
            if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = ft.GetGenericArguments()[0];
            }
            if (!TypeToDbType.TryGetValue(t, out NpgsqlDbType dbt))
            {
                dbt = NpgsqlDbType.Text;
            }
            if (isArray)
            {
                dbt |= NpgsqlDbType.Array;
            }
            return dbt;
        }
        public override object Eval(string expression, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            using (IDbCommand cmd = GetSqlCommand("select " + expression, null, connection))
            {
                object v = cmd.ExecuteScalar();
                if (v is DateTime time && time.Kind == DateTimeKind.Utc)
                {
                    v = time.ToLocalTime();
                }
                return v;
            }
        }

        private static readonly string[] TimestampFormats = new string[]
        {
            "{0:yyyy-MM-dd HH:mm:ss}",
            "{0:yyyy-MM-dd HH:mm:ss.F}",
            "{0:yyyy-MM-dd HH:mm:ss.FF}",
            "{0:yyyy-MM-dd HH:mm:ss.FFF}",
            "{0:yyyy-MM-dd HH:mm:ss.FFFF}",
            "{0:yyyy-MM-dd HH:mm:ss.FFFFF}",
            "{0:yyyy-MM-dd HH:mm:ss.FFFFFF}",
        };
        private string GetImmediatedDateTimeStr(Column column, DateTime value)
        {
            string baseType = string.Empty;
            int precision = 6;
            if (column != null)
            {
                baseType = column.BaseType.ToLower();
                precision = Math.Min(column.Precision ?? 6, TimestampFormats.Length - 1);
            }
            if (baseType == "date")
            {
                return string.Format("date '{0:yyyy-MM-dd}'", value);
            }
            string s = string.Format(TimestampFormats[precision], value);
            s = s.TrimEnd('.');
            return string.Format("timestamp '{0}'", s);
        }
        public override string GetImmediatedStr(ColumnInfo column, object value)
        {
            if (value == null || value is DBNull)
            {
                return "null";
            }
            if (column.IsNumeric)
            {
                return base.GetImmediatedStr(column, value);
            }
            if (column.IsDateTime)
            {
                DateTime dt = Convert.ToDateTime(value);
                return GetImmediatedDateTimeStr(column.Column, dt);
            }
            return ToLiteralStr(value.ToString());
        }

        private void ApplyParameterByFieldInfo(NpgsqlParameter parameter, ColumnInfo info, object value)
        {
            parameter.IsNullable = info.IsNullable;
            parameter.Value = value ?? DBNull.Value;
        }
        public override IDbDataParameter ApplyParameterByFieldInfo(IDataParameterCollection parameters, ColumnInfo info, object value, bool isOld)
        {
            NpgsqlParameter param = parameters[isOld ? "old_" + info.Name : info.Name] as NpgsqlParameter;
            param.NpgsqlDbType = GetNpgsqlDbType(info);
            param.IsNullable = info.IsNullable;
            param.Value = value ?? DBNull.Value;
            return param;
        }
        public override IDbDataParameter CreateParameterByFieldInfo(ColumnInfo info, object value, bool isOld)
        {
            NpgsqlParameter param = new NpgsqlParameter(isOld ? "old_" + info.Name : info.Name, GetNpgsqlDbType(info))
            {
                SourceColumn = info.Name,
                SourceVersion = isOld ? DataRowVersion.Original : DataRowVersion.Current
            };
            ApplyParameterByFieldInfo(param, info, value);
            return param;
        }

        private void GetInsertColumnsByParamsSql(Table table, int indent, int charPerLine, out string fields, out string values)
        {
            string spc = GetIndent(indent);
            StringBuilder bufF = new StringBuilder();
            StringBuilder bufP = new StringBuilder();
            bool needComma = false;
            bufF.Append(spc);
            bufP.Append(spc);
            int w = spc.Length;
            bool isFirst = true;
            foreach (Column c in table.Columns)
            {
                string col = GetEscapedIdentifier(c.Name, true);
                string prm = ":" + c.Name;
                int wColumn = Math.Max(GetCharWidth(col), GetCharWidth(prm));
                if (needComma)
                {
                    bufF.Append(',');
                    bufP.Append(',');
                    w++;
                    if (charPerLine <= w + wColumn && !isFirst)
                    {
                        bufF.AppendLine();
                        bufF.Append(spc);
                        bufP.AppendLine();
                        bufP.Append(spc);
                        w = spc.Length;
                    }
                    else
                    {
                        bufF.Append(' ');
                        bufP.Append(' ');
                        w++;
                    }
                }
                w += wColumn;
                bufF.Append(col);
                bufP.Append(prm);
                needComma = true;
                isFirst = false;
            }
            fields = bufF.ToString();
            values = bufP.ToString();
        }
        private string GetUpdateColumnsByParamsSql(Table table, int indent, KeyConstraint excludeKey)
        {
            string spc = GetIndent(indent);
            Dictionary<string, bool> keys = new Dictionary<string, bool>();
            if (excludeKey != null)
            {
                foreach (string c in excludeKey.Columns)
                {
                    keys[c] = true;
                }
            }

            StringBuilder buf = new StringBuilder();
            bool needComma = false;
            foreach (Column c in table.Columns)
            {
                if (keys.ContainsKey(c.Name))
                {
                    continue;
                }
                if (needComma)
                {
                    buf.AppendLine(",");
                }
                buf.Append(spc);
                buf.Append(GetEscapedIdentifier(c.Name, true));
                buf.Append(" = :");
                buf.Append(c.Name);
                needComma = true;
            }
            buf.AppendLine();
            return buf.ToString();
        }

        private string GetUpdateColumnsBySameColumnSql(Table table, int indent, string prefix, KeyConstraint excludeKey)
        {
            string spc = GetIndent(indent);
            Dictionary<string, bool> keys = new Dictionary<string, bool>();
            if (excludeKey != null)
            {
                foreach (string c in excludeKey.Columns)
                {
                    keys[c] = true;
                }
            }

            StringBuilder buf = new StringBuilder();
            bool needComma = false;
            foreach (Column c in table.Columns)
            {
                if (keys.ContainsKey(c.Name))
                {
                    continue;
                }
                if (needComma)
                {
                    buf.AppendLine(",");
                }
                string col = GetEscapedIdentifier(c.Name, true);
                buf.Append(spc);
                buf.Append(col);
                buf.Append(" = ");
                buf.Append(prefix);
                buf.Append(col);
                needComma = true;
            }
            buf.AppendLine();
            return buf.ToString();
        }

        private string GetInsertSql(Table table, string alias, int indent, int charPerLine, string postfix, bool noNewLine, bool addNewline)
        {
            string spc = GetIndent(indent);
            GetInsertColumnsByParamsSql(table, indent + 1, noNewLine ? int.MaxValue : charPerLine, out string flds, out string prms);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("insert into ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            if (!string.IsNullOrEmpty(alias))
            {
                buf.Append(" as ");
                buf.Append(alias);
            }
            buf.Append(" (");
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(flds);
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(spc);
            buf.Append(") values (");
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(prms);
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(spc);
            buf.Append(")");
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetInsertSql(Table table, int indent, int charPerLine, string postfix, bool noNewLine)
        {
            return GetInsertSql(table, string.Empty, indent, charPerLine, postfix, noNewLine, true);
        }

        /// <summary>
        /// データ付でINSERT文を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="charPerLine"></param>
        /// <param name="postfix"></param>
        /// <param name="data">項目と値の組み合わせを渡す</param>
        /// <returns></returns>
        public override string GetInsertSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data, bool noNewLine)
        {
            string spc = GetIndent(indent);
            StringBuilder bufF = new StringBuilder();
            StringBuilder bufP = new StringBuilder();
            bool needComma = false;
            int w = 0;
            if (!noNewLine)
            {
                bufF.Append(spc);
                bufF.Append(IndentText);
                bufP.Append(spc);
                bufP.Append(IndentText);
                w = spc.Length + 2;
            }
            Dictionary<string, ColumnInfo> name2col = new Dictionary<string, ColumnInfo>();
            foreach (ColumnInfo info in data.Keys)
            {
                name2col.Add(info.Name, info);
            }
            foreach (Column c in table.Columns)
            {
                if (!name2col.ContainsKey(c.Name))
                {
                    // dataにない項目は出力しない
                    continue;
                }
                if (needComma)
                {
                    bufF.Append(',');
                    bufP.Append(',');
                    w++;
                    if (!noNewLine && charPerLine <= w)
                    {
                        bufF.AppendLine();
                        bufF.Append(spc);
                        bufF.Append(IndentText);
                        bufP.AppendLine();
                        bufP.Append(spc);
                        bufP.Append(IndentText);
                        w = spc.Length + 2;
                    }
                    else
                    {
                        bufF.Append(' ');
                        bufP.Append(' ');
                        w++;
                    }
                }
                string col = GetEscapedIdentifier(c.Name, true);
                ColumnInfo info = name2col[c.Name];
                object v = data[info];
                string val = GetImmediatedStr(info, v);
                w += Math.Max(GetCharWidth(col), GetCharWidth(val));
                bufF.Append(col);
                bufP.Append(val);
                needComma = true;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("insert into ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.Append(" (");
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(bufF);
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(spc);
            buf.Append(") values (");
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(bufP);
            if (!noNewLine)
            {
                buf.AppendLine();
            }
            buf.Append(spc);
            buf.Append(")");
            buf.AppendLine(postfix);
            return buf.ToString();
        }
        public override string GetUpdateSql(Table table, string where, int indent, int charPerLine, string postfix)
        {
            string spc = GetIndent(indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("update ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" set");
            buf.Append(GetUpdateColumnsByParamsSql(table, indent + 1, null));
            buf.Append(spc);
            buf.AppendLine(where);
            if (!string.IsNullOrEmpty(postfix))
            {
                buf.Append(spc);
                buf.AppendLine(postfix);
            }
            return buf.ToString();
        }
        public override string GetDeleteSql(Table table, string where, int indent, int charPerLine, string postfix)
        {
            string spc = GetIndent(indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("delete from ");
            buf.AppendLine(table.EscapedIdentifier(CurrentSchema));
            buf.Append(spc);
            buf.AppendLine(where);
            if (!string.IsNullOrEmpty(postfix))
            {
                buf.Append(spc);
                buf.AppendLine(postfix);
            }
            return buf.ToString();
        }

        public override string GetUpdateSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data, Dictionary<ColumnInfo, object> keys)
        {
            string spc = GetIndent(indent);
            Dictionary<string, ColumnInfo> name2col = new Dictionary<string, ColumnInfo>();
            foreach (ColumnInfo info in data.Keys)
            {
                name2col.Add(info.Name, info);
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("update ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            string prefix = " set";
            foreach (Column c in table.Columns)
            {
                if (!name2col.TryGetValue(c.Name, out ColumnInfo info))
                {
                    continue;
                }
                buf.AppendLine(prefix);
                buf.Append(spc);
                buf.Append(Db2SourceContext.IndentText);
                buf.Append(GetEscapedIdentifier(c.Name, true));
                buf.Append(" = ");
                buf.Append(GetImmediatedStr(info, data[info]));
                prefix = ",";
            }
            prefix = "where ";
            string prefixAnd = Db2SourceContext.IndentText + "and ";
            foreach (string c in table.PrimaryKey.Columns)
            {
                if (!name2col.TryGetValue(c, out ColumnInfo info))
                {
                    continue;
                }
                buf.AppendLine();
                buf.Append(spc);
                buf.Append(prefix);
                buf.Append(GetEscapedIdentifier(c, true));
                buf.Append(" = ");
                buf.Append(GetImmediatedStr(info, data[info]));
                prefix = prefixAnd;
            }
            buf.AppendLine(postfix);
            return buf.ToString();
        }
        /// <summary>
        /// 条件文付でDELETE文を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="indent"></param>
        /// <param name="charPerLine"></param>
        /// <param name="postfix"></param>
        /// <param name="keys">抽出条件に使用する項目と値の組み合わせを渡す</param>
        /// <returns></returns>
        public override string GetDeleteSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> keys)
        {
            throw new NotImplementedException();
        }
        public override string GetInsertUpdateSql(Table table, int indent, int charPerLine, string postfix)
        {
            if (table.PrimaryKey == null)
            {
                throw new ArgumentException("主キーがありません");
            }
            StringBuilder buf = new StringBuilder(GetInsertSql(table, null, indent, charPerLine, string.Empty, false, false));
            buf.Append(" on conflict on constraint ");
            buf.Append(GetEscapedIdentifier(table.PrimaryKey.Name, true));
            buf.AppendLine(" do update set ");
            buf.Append(GetUpdateColumnsBySameColumnSql(table, indent + 1, "excluded.", table.PrimaryKey));
            return buf.ToString();
        }
        public override string GetMergeSql(Table table, int indent, int charPerLine, string postfix)
        {
            if (table.PrimaryKey == null)
            {
                throw new ArgumentException("主キーがありません");
            }
            string spc = GetIndent(indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("merge into ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" as t");
            buf.Append(spc);
            string prefix = "using (values (";
            foreach (string c in table.PrimaryKey.Columns)
            {
                buf.Append(prefix);
                buf.Append(":");
                buf.Append(c);
                prefix = ", ";
            }
            prefix = ")) as i(";
            foreach (string c in table.PrimaryKey.Columns)
            {
                buf.Append(prefix);
                buf.Append(GetEscapedIdentifier(c, true));
                prefix = ", ";
            }
            buf.Append(")");
            prefix = "  on (";
            foreach (string c in table.PrimaryKey.Columns)
            {
                string col = GetEscapedIdentifier(c, true);
                buf.AppendLine();
                buf.Append(spc);
                buf.Append(prefix);
                buf.Append("t.");
                buf.Append(col);
                buf.Append(" = i.");
                buf.Append(col);
                prefix = "    and ";
            }
            buf.AppendLine(")");
            buf.Append(spc);
            buf.AppendLine("when matched then update set");
            buf.Append(GetUpdateColumnsByParamsSql(table, indent + 1, table.PrimaryKey));
            buf.Append(spc);
            buf.AppendLine("when not matched then insert (");
            GetInsertColumnsByParamsSql(table, indent + 1, charPerLine, out string flds, out string prms);
            buf.AppendLine(flds);
            buf.Append(spc);
            buf.AppendLine(") values (");
            buf.AppendLine(prms);
            buf.Append(spc);
            buf.AppendLine(")");
            return buf.ToString();
        }

        /// <summary>
        /// COPY文(PostgreSQL固有SQL文)の宣言部分を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="postfix"></param>
        /// <param name="columns">出力する項目の順番を指定する</param>
        /// <returns></returns>
        public override string GetCopySql(Table table, int indent, string postfix, ColumnInfo[] columns)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// COPY文(PostgreSQL固有SQL文)のデータ部分を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="columns">出力する項目の順番を指定する</param>
        /// <param name="data">出力するデータの配列</param>
        /// <returns></returns>
        public override string GetCopyDataSql(Table table, ColumnInfo[] columns, object[][] data)
        {
            throw new NotImplementedException();
        }
        private void ExecuteInsert(IChangeSet owner, IChangeSetRow row, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Table tbl = owner.Table as Table;
            if (tbl == null)
            {
                throw new ApplicationException("更新対象の表が設定されていないため、更新できません");
            }
            StringBuilder bufF = new StringBuilder();
            StringBuilder bufP = new StringBuilder();
            List<NpgsqlParameter> listP = new List<NpgsqlParameter>();
            bool needComma = false;
            foreach (ColumnInfo f in owner.Fields)
            {
                if (needComma)
                {
                    bufF.Append(", ");
                    bufP.Append(", ");
                }
                bufF.Append(GetEscapedIdentifier(f.Name, true));
                if (row[f.Index] == null || row[f.Index] is DBNull)
                {
                    if (f.IsDefaultDefined)
                    {
                        bufP.Append("DEFAULT");
                    }
                    else
                    {
                        bufP.Append("NULL");
                    }
                }
                else
                {
                    bufP.Append(':');
                    bufP.Append(f.Name);
                    NpgsqlParameter p = CreateParameterByFieldInfo(f, row[f.Index], false) as NpgsqlParameter;
                    p.SourceColumn = f.Name;
                    p.SourceVersion = DataRowVersion.Current;
                    listP.Add(p);
                }
                needComma = true;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("insert into ");
            buf.Append(tbl.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" (");
            buf.Append(Db2SourceContext.IndentText);
            buf.Append(bufF);
            buf.AppendLine(") values (");
            buf.Append(Db2SourceContext.IndentText);
            buf.Append(bufP);
            buf.AppendLine();
            buf.Append(") returning *");
            ExecuteSql(buf.ToString(), listP.ToArray(), owner, row, connection, transaction);
        }

        private void ExecuteUpdate(IChangeSet owner, IChangeSetRow row, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Table tbl = owner?.Table as Table;
            if (tbl == null)
            {
                throw new ApplicationException("更新対象の表が設定されていないため、更新できません");
            }
            if (owner.KeyFields == null || owner.KeyFields.Length == 0)
            {
                throw new ApplicationException("主キーが設定されていないため、更新できません");
            }
            StringBuilder bufF = new StringBuilder();
            List<NpgsqlParameter> listP = new List<NpgsqlParameter>();
            bool needComma = false;
            foreach (ColumnInfo f in owner.Fields)
            {
                if (!row.IsModified(f.Index))
                {
                    continue;
                }
                if (needComma)
                {
                    bufF.AppendLine(", ");
                }
                bufF.Append(Db2SourceContext.IndentText);
                bufF.Append(GetEscapedIdentifier(f.Name, true));
                bufF.Append(" = :");
                bufF.Append(f.Name);
                NpgsqlParameter p = CreateParameterByFieldInfo(f, row[f.Index], false) as NpgsqlParameter;
                p.SourceColumn = f.Name;
                p.SourceVersion = DataRowVersion.Current;
                listP.Add(p);
                needComma = true;
            }
            bufF.AppendLine();
            if (!needComma)
            {
                // 変更がなかった
                return;
            }
            StringBuilder bufC = new StringBuilder();
            bool needAnd = false;
            ColumnInfo[] columns = owner.KeyFields;
            if (columns == null || columns.Length == 0)
            {
                columns = owner.Fields;
            }
            foreach (ColumnInfo f in columns)
            {
                if (needAnd)
                {
                    bufC.Append("  and ");
                }
                else
                {
                    bufC.Append("where ");
                }
                if (f.IsNullable)
                {
                    bufC.AppendFormat("(({0} = :old_{1}) or ({0} is null and :old_{1} is null))", GetEscapedIdentifier(f.Name, true), f.Name);
                }
                else
                {
                    bufC.AppendFormat("({0} = :old_{1})", GetEscapedIdentifier(f.Name, true), f.Name);
                }
                bufC.AppendLine();
                NpgsqlParameter p = CreateParameterByFieldInfo(f, row.Old(f.Index), true) as NpgsqlParameter;
                listP.Add(p);
                needAnd = true;
            }

            StringBuilder buf = new StringBuilder();
            buf.Append("update ");
            buf.Append(tbl.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" set");
            buf.Append(bufF);
            buf.Append(bufC);
            buf.Append("returning *");
            ExecuteSql(buf.ToString(), listP.ToArray(), owner, row, connection, transaction);
        }
        private void ExecuteDelete(IChangeSet owner, IChangeSetRow row, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Table tbl = owner?.Table as Table;
            if (tbl == null)
            {
                throw new ApplicationException("更新対象の表が設定されていないため、削除できません");
            }
            //if (owner.KeyFields == null || owner.KeyFields.Length == 0)
            //{
            //    throw new ApplicationException("主キーが設定されていないため、削除できません");
            //}
            List<NpgsqlParameter> listP = new List<NpgsqlParameter>();
            StringBuilder bufC = new StringBuilder();
            bool needAnd = false;
            ColumnInfo[] columns = owner.KeyFields;
            if (columns == null || columns.Length == 0)
            {
                columns = owner.Fields;
            }
            foreach (ColumnInfo f in columns)
            {
                if (needAnd)
                {
                    bufC.Append("  and ");
                }
                else
                {
                    bufC.Append("where ");
                }
                if (f.IsNullable)
                {
                    bufC.AppendFormat("(({0} = :old_{1}) or ({0} is null and :old_{1} is null))", GetEscapedIdentifier(f.Name, true), f.Name);
                }
                else
                {
                    bufC.AppendFormat("({0} = :old_{1})", GetEscapedIdentifier(f.Name, true), f.Name);
                }
                bufC.AppendLine();
                NpgsqlParameter p = CreateParameterByFieldInfo(f, row.Old(f.Index), true) as NpgsqlParameter;
                listP.Add(p);
                needAnd = true;
            }

            StringBuilder buf = new StringBuilder();
            buf.Append("delete from ");
            buf.AppendLine(tbl.EscapedIdentifier(CurrentSchema));
            buf.Append(bufC);
            ExecuteSql(buf.ToString(), listP.ToArray(), owner, row, connection, transaction);
        }

        public override void ApplyChange(IChangeSet owner, IChangeSetRow row, IDbConnection connection, IDbTransaction transaction, Dictionary<IChangeSetRow, bool> applied)
        {
            if (row == null)
            {
                return;
            }
            if (row.ChangeKind == ChangeKind.None)
            {
                return;
            }
            if (applied.ContainsKey(row))
            {
                return;
            }
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            IChangeSetRow dep = owner.Rows.FindRowByOldKey(row.GetKeys());
            if (dep != null && dep != row)
            {
                ApplyChange(owner, dep, connection, transaction, applied);
            }

            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            if (!(connection is NpgsqlConnection))
            {
                throw new ArgumentException("connection");
            }
            NpgsqlConnection conn = (NpgsqlConnection)connection;
            NpgsqlTransaction txn = (NpgsqlTransaction)transaction;
            {
                try
                {
                    switch (row.ChangeKind)
                    {
                        case ChangeKind.None:
                            return;
                        case ChangeKind.New:
                            ExecuteInsert(owner, row, conn, txn);
                            break;
                        case ChangeKind.Modify:
                            ExecuteUpdate(owner, row, conn, txn);
                            break;
                        case ChangeKind.Delete:
                            ExecuteDelete(owner, row, conn, txn);
                            break;
                    }
                    applied.Add(row, true);
                }
                catch (Exception t)
                {
                    row.SetError(t);
                    throw;
                }
            }
        }

        private SchemaObject[] GetStrongReferred(Table table)
        {
            List<SchemaObject> l = new List<SchemaObject>();
            foreach (ForeignKeyConstraint c in table.ReferFrom)
            {
                l.Add(c);
            }
            return l.ToArray();
        }

        private static readonly SchemaObject[] NoSchemas = new SchemaObject[0];

        public override SchemaObject[] GetStrongReferred(SchemaObject target)
        {
            if (!(target is Table tbl))
            {
                return NoSchemas;
            }
            return GetStrongReferred(tbl);
        }

        public override long? GetCurrentSequenceValue(Sequence sequence, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (conn == null)
            {
                throw new ArgumentException("connectionがNpgsqlConnectionではありません");
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = Resources.GetLastSequence_SQL;
                cmd.Parameters.Add(new NpgsqlParameter("seqowner", sequence.SchemaName));
                cmd.Parameters.Add(new NpgsqlParameter("seqname", sequence.Name));
                object ret = cmd.ExecuteScalar();
                if (ret == null || ret is DBNull)
                {
                    return null;
                }
                return Convert.ToInt64(ret);
            }
        }
        public override void SetSequenceValue(long value, Sequence sequence, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (conn == null)
            {
                throw new ArgumentException("connectionがNpgsqlConnectionではありません");
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format("select setval('{0}'::regclass, :val)", sequence.EscapedIdentifier(null));
                cmd.Parameters.Add(new NpgsqlParameter("val", NpgsqlDbType.Bigint) { Value = value });
                cmd.ExecuteNonQuery();
            }
        }
        public override long? GetMaxValueOfColumn(Column column, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (conn == null)
            {
                throw new ArgumentException("connectionがNpgsqlConnectionではありません");
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format("select max({0}) from {1}", column.EscapedName, column.Table.EscapedIdentifier(null));
                object ret = cmd.ExecuteScalar();
                if (ret == null || ret is DBNull)
                {
                    return null;
                }
                return Convert.ToInt64(ret);
            }
        }

        public override long? GetMinValueOfColumn(Column column, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (conn == null)
            {
                throw new ArgumentException("connectionがNpgsqlConnectionではありません");
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format("select min({0}) from {1}", column.EscapedName, column.Table.EscapedIdentifier(null));
                object ret = cmd.ExecuteScalar();
                if (ret == null || ret is DBNull)
                {
                    return null;
                }
                return Convert.ToInt64(ret);
            }
        }
    }
}