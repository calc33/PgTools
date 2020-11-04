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
    public partial class NpgsqlDataSet: Db2SourceContext
    {
        public NpgsqlDataSet(NpgsqlConnectionInfo info) : base(info) { }

        public override IDbConnection NewConnection()
        {
            NpgsqlConnection ret = base.NewConnection() as NpgsqlConnection;
            ret.Notice += NpgsqlConnection_Notice;
            return ret;
        }

        private void NpgsqlConnection_Notice(object sender, NpgsqlNoticeEventArgs e)
        {
            //OnLog(e.Notice.Detail, LogStatus.Aux, null);
        }

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
        private static readonly Regex PgSqlIdentifierRegex = new Regex("^[_A-z][_$0-9A-z]*$");
        public static bool IsReservedWord(string value)
        {
            return PgsqlReservedDict.ContainsKey(value.ToUpper());
        }
        public static bool NeedQuotedPgsqlIdentifier(string value)
        {
            return !PgSqlIdentifierRegex.IsMatch(value) || IsReservedWord(value);
        }
        public override bool NeedQuotedIdentifier(string value)
        {
            return NeedQuotedPgsqlIdentifier(value);
        }
        public static string GetEscapedPgsqlIdentifier(string objectName)
        {
            if (!IsQuotedIdentifier(objectName) && NeedQuotedPgsqlIdentifier(objectName))
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
            bool ret;
            if (HiddenSchemaNames.TryGetValue(schemaName, out ret))
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
            TokenizedSQL tsql = new TokenizedSQL(sql);
            int n = tsql.Tokens.Length;
            for (int i = 0; i < n; i++)
            {
                Token t = tsql.Tokens[i];
                if (t.ID == TokenID.Colon)
                {
                    i++;
                    t = tsql.Tokens[i];
                    Token t0 = t;
                    for (; i < n && tsql.Tokens[i].ID == TokenID.Identifier; t = tsql.Tokens[i++]) ;
                    string p = DequoteIdentifier(tsql.Extract(t0, t));
                    if (!dict.ContainsKey(p))
                    {
                        l.Add(p);
                        dict.Add(p, true);
                    }
                }
            }
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
                OnLog(sender, e.Notice.Detail);
            }
            public void Command_Disposed(object sender, EventArgs e)
            {
                Log = null;
            }
            public static LogConverter NewLogConverter(NpgsqlCommand command, EventHandler<LogEventArgs> logEvent)
            {
                if (logEvent == null)
                {
                    return null;
                }
                LogConverter conv = new LogConverter();
                conv.Log = logEvent;
                command.Disposed += conv.Command_Disposed;
                command.Connection.Notice += conv.Notice;
                return conv;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
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
                NpgsqlParameter p = new NpgsqlParameter(s, DbType.String);
                p.Direction = ParameterDirection.InputOutput;
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
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
                NpgsqlParameter p = new NpgsqlParameter(s, DbType.String);
                p.Direction = ParameterDirection.InputOutput;
                cmd.Parameters.Add(p);
            }
            return cmd;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
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
            buf.Append(GetEscapedIdentifier(function.SchemaName, function.Name, null));
            buf.Append("(");
            bool needComma = false;
            List<NpgsqlParameter> l = new List<NpgsqlParameter>();
            foreach (Parameter p in function.Parameters)
            {
                if (p.Direction == ParameterDirection.Output)
                {
                    continue;
                }
                string pName = p.Name ?? p.Index.ToString();
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(':');
                buf.Append(pName);
                buf.Append("::");
                buf.Append(p.BaseType);
                NpgsqlDbType t;
                if (!TypeToDbType.TryGetValue(p.ValueType, out t))
                {
                    t = NpgsqlDbType.Text;
                }
                NpgsqlParameter np = new NpgsqlParameter(pName, t);
                np.Direction = p.Direction;
                l.Add(np);
                needComma = true;
            }
            buf.Append(")");
            NpgsqlCommand cmd = new NpgsqlCommand(buf.ToString(), connection as NpgsqlConnection, transaction as NpgsqlTransaction);
            cmd.CommandType = CommandType.Text;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
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
            Type t = info.FieldType;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = t.GetGenericArguments()[0];
            }
            NpgsqlDbType dbt;
            if (!TypeToDbType.TryGetValue(t, out dbt))
            {
                dbt = NpgsqlDbType.Text;
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
                return cmd.ExecuteScalar();
            }
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
                if (column.Column == null)
                {
                    DateTime dt = Convert.ToDateTime(value);
                    if (dt.TimeOfDay == TimeSpan.Zero)
                    {
                        return string.Format("TO_DATE('{0:yyyy-M-d}', 'YYYY-MM-DD')", dt);
                    }
                    return string.Format("TO_DATE('{0:yyyy-M-d HH:mm:ss}', 'YYYY-MM-DD HH:MI:SS')", dt);
                }
            }
            return ToLiteralStr(value.ToString());
        }

    private void ApplyParameterByFieldInfo(NpgsqlParameter parameter, ColumnInfo info, object value)
        {
            parameter.IsNullable = info.IsNullable;
            parameter.Value = (value != null) ? value : DBNull.Value;
        }
        public override IDbDataParameter ApplyParameterByFieldInfo(IDataParameterCollection parameters, ColumnInfo info, object value, bool isOld)
        {
            NpgsqlParameter param = parameters[isOld ? "old_" + info.Name : info.Name] as NpgsqlParameter;
            param.NpgsqlDbType = GetNpgsqlDbType(info);
            param.IsNullable = info.IsNullable;
            param.Value = (value != null) ? value : DBNull.Value;
            return param;
        }
        public override IDbDataParameter CreateParameterByFieldInfo(ColumnInfo info, object value, bool isOld)
        {
            NpgsqlParameter param = new NpgsqlParameter(isOld ? "old_" + info.Name : info.Name, GetNpgsqlDbType(info));
            param.SourceColumn = info.Name;
            param.SourceVersion = isOld ? DataRowVersion.Original : DataRowVersion.Current;
            ApplyParameterByFieldInfo(param, info, value);
            return param;
        }

        public override string GetInsertSql(Table table, int indent, int charPerLine, string postfix)
        {
            string spc = new string(' ', indent);
            StringBuilder bufF = new StringBuilder();
            StringBuilder bufP = new StringBuilder();
            bool needComma = false;
            bufF.Append(spc);
            bufF.Append("  ");
            bufP.Append(spc);
            bufP.Append("  ");
            int w = spc.Length + 2;
            foreach (Column c in table.Columns)
            {
                if (needComma)
                {
                    bufF.Append(',');
                    bufP.Append(',');
                    w++;
                    if (charPerLine <= w)
                    {
                        bufF.AppendLine();
                        bufF.Append(spc);
                        bufF.Append("  ");
                        bufP.AppendLine();
                        bufP.Append(spc);
                        bufP.Append("  ");
                        w = spc.Length + 2;
                    }
                    else
                    {
                        bufF.Append(' ');
                        bufP.Append(' ');
                        w++;
                    }
                }
                string col = GetEscapedIdentifier(c.Name);
                string prm = ":" + c.Name;
                w += Math.Max(col.Length, prm.Length);
                bufF.Append(col);
                bufP.Append(prm);
                needComma = true;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("insert into ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" (");
            buf.Append(bufF);
            buf.AppendLine();
            buf.Append(spc);
            buf.AppendLine(") values (");
            buf.Append(bufP);
            buf.AppendLine();
            buf.Append(spc);
            buf.Append(")");
            buf.AppendLine(postfix);
            return buf.ToString();
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
        public override string GetInsertSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data)
        {
            string spc = new string(' ', indent);
            StringBuilder bufF = new StringBuilder();
            StringBuilder bufP = new StringBuilder();
            bool needComma = false;
            bufF.Append(spc);
            bufF.Append("  ");
            bufP.Append(spc);
            bufP.Append("  ");
            int w = spc.Length + 2;
            Dictionary<string, ColumnInfo> name2col = new Dictionary<string, ColumnInfo>();
            foreach (ColumnInfo info in data.Keys){
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
                    if (charPerLine <= w)
                    {
                        bufF.AppendLine();
                        bufF.Append(spc);
                        bufF.Append("  ");
                        bufP.AppendLine();
                        bufP.Append(spc);
                        bufP.Append("  ");
                        w = spc.Length + 2;
                    }
                    else
                    {
                        bufF.Append(' ');
                        bufP.Append(' ');
                        w++;
                    }
                }
                string col = GetEscapedIdentifier(c.Name);
                ColumnInfo info = name2col[c.Name];
                object v = data[info];
                string val = GetImmediatedStr(info, v);
                w += Math.Max(col.Length, val.Length);
                bufF.Append(col);
                bufP.Append(val);
                needComma = true;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("insert into ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" (");
            buf.Append(bufF);
            buf.AppendLine();
            buf.Append(spc);
            buf.AppendLine(") values (");
            buf.Append(bufP);
            buf.AppendLine();
            buf.Append(spc);
            buf.Append(")");
            buf.AppendLine(postfix);
            return buf.ToString();
        }
        public override string GetUpdateSql(Table table, string where, int indent, int charPerLine, string postfix)
        {
            string spc = new string(' ', indent);
            bool needComma = false;
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("update ");
            buf.Append(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(" set");
            foreach (Column c in table.Columns)
            {
                if (needComma)
                {
                    buf.AppendLine(",");
                }
                buf.Append(spc);
                buf.Append("  ");
                buf.Append(GetEscapedIdentifier(c.Name));
                buf.Append(" = :");
                buf.Append(c.Name);
                needComma = true;
            }
            buf.AppendLine();
            buf.Append(spc);
            buf.AppendLine(where);
            buf.AppendLine(postfix);
            return buf.ToString();
        }
        public override string GetDeleteSql(Table table, string where, int indent, int charPerLine, string postfix)
        {
            string spc = new string(' ', indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append("delete from ");
            buf.AppendLine(table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine(where);
            return buf.ToString();
        }

        public override string GetUpdateSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data, Dictionary<ColumnInfo, object> keys)
        {
            throw new NotImplementedException();
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
            Table tbl = owner.Table;
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
                bufF.Append(GetEscapedIdentifier(f.Name));
                if (row[f.Index] == null || row[f.Index] is DBNull) {
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
            buf.Append("  ");
            buf.Append(bufF);
            buf.AppendLine(") values (");
            buf.Append("  ");
            buf.Append(bufP);
            buf.AppendLine();
            buf.Append(") returning *");
            ExecuteSql(buf.ToString(), listP.ToArray(), owner, row, connection, transaction);
        }

        private void ExecuteUpdate(IChangeSet owner, IChangeSetRow row, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Table tbl = owner?.Table;
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
                bufF.Append("  ");
                bufF.Append(GetEscapedIdentifier(f.Name));
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
            foreach (ColumnInfo f in owner.KeyFields)
            {
                if (needAnd)
                {
                    bufC.Append("  and ");
                } else
                {
                    bufC.Append("where ");
                }
                if (f.IsNullable)
                {
                    bufC.AppendFormat("(({0} = :old_{1}) or ({0} is null and :old_{1} is null))", GetEscapedIdentifier(f.Name), f.Name);
                }
                else
                {
                    bufC.AppendFormat("({0} = :old_{1})", GetEscapedIdentifier(f.Name), f.Name);
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
            Table tbl = owner?.Table;
            if (tbl == null)
            {
                throw new ApplicationException("更新対象の表が設定されていないため、削除できません");
            }
            if (owner.KeyFields == null || owner.KeyFields.Length == 0)
            {
                throw new ApplicationException("主キーが設定されていないため、削除できません");
            }
            List<NpgsqlParameter> listP = new List<NpgsqlParameter>();
            StringBuilder bufC = new StringBuilder();
            bool needAnd = false;
            foreach (ColumnInfo f in owner.KeyFields)
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
                    bufC.AppendFormat("(({0} = :old_{1}) or ({0} is null and :old_{1} is null))", GetEscapedIdentifier(f.Name), f.Name);
                }
                else
                {
                    bufC.AppendFormat("({0} = :old_{1})", GetEscapedIdentifier(f.Name), f.Name);
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

        public override void ApplyChange(IChangeSet owner, IChangeSetRow row, IDbConnection connection)
        {
            if (row == null)
            {
                return;
            }
            if (row.ChangeKind == ChangeKind.None)
            {
                return;
            }
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            IChangeSetRow dep = owner.Rows.FingRowByOldKey(row.GetKeys());
            if (dep != null && dep != row)
            {
                ApplyChange(owner, dep, connection);
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
            using (NpgsqlTransaction txn = conn.BeginTransaction())
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
                }
                catch (Exception)
                {
                    txn.Rollback();
                    throw;
                }
                txn.Commit();
                row.AcceptChanges();
            }
        }
    }
}