using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum LogStatus
    {
        Normal = 0,
        Error = 1,
        Aux = 2
    }
    public class SQLPart
    {
        public int Offset { get; set; }
        public string SQL { get; set; }
        public string[] ParameterNames { get; set; }
    }

    public class SQLParts
    {
        public SQLPart[] Items { get; set; }
        public string[] ParameterNames { get; set; }
        public int Count
        {
            get
            {
                return (Items != null) ? Items.Length : 0;
            }
        }
        public SQLPart this[int index]
        {
            get
            {
                return (Items != null) ? Items[index] : null;
            }
        }
    }
    public class LogEventArgs : EventArgs
    {
        public LogStatus Status { get; private set; }
        public string Text { get; private set; }
        public string Sql { get; private set; }
        internal LogEventArgs(string text, LogStatus status, string sql)
        {
            Status = status;
            Text = text;
            Sql = sql;
        }
    }
    public class SchemaObjectReplacedEventArgs : EventArgs
    {
        public SchemaObject New { get; private set; }
        public SchemaObject Old { get; private set; }
        public SchemaObjectReplacedEventArgs(SchemaObject newobj, SchemaObject oldobj)
        {
            New = newobj;
            Old = oldobj;
        }
    }

    public enum ChangeKind
    {
        None,
        New,
        Modify,
        Delete
    }

    public interface IChangeSetRows
    {
        IChangeSetRow FingRowByOldKey(object[] key);
        void AcceptChanges();
        void RevertChanges();
        ICollection<IChangeSetRow> DeletedRows { get; }
        ICollection<IChangeSetRow> TemporaryRows { get; }

    }
    public interface IChangeSet
    {
        Table Table { get; set; }
        ColumnInfo[] KeyFields { get; }
        ColumnInfo[] Fields { get; }
        ColumnInfo GetFieldByName(string name);
        IChangeSetRows Rows { get; }
    }
    public interface IChangeSetRow
    {
        object this[int index] { get; }
        bool IsModified(int index);
        ChangeKind ChangeKind { get; }
        object Old(int index);
        object[] GetKeys();
        void Read(IDataReader reader, int[] indexList);
        void AcceptChanges();
    }

    /// <summary>
    /// ソース出力時にschemaを先頭に付加するかどうか
    /// </summary>
    public enum SourceSchemaOption
    {
        /// <summary>
        /// 常に付加しない
        /// </summary>
        Omitted,
        /// <summary>
        /// カレントスキーマの場合は付加しない
        /// </summary>
        OmitCurrent,
        /// <summary>
        /// 常に付加する
        /// </summary>
        Every
    }

    public enum CaseRule
    {
        Lowercase,
        Uppercase
    }

    public abstract partial class Db2SourceContext: IComparable
    {
        private static string InitAppDataDir()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Db2Src.net");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
        public static string AppDataDir = InitAppDataDir();
        public static bool IsSQLLoggingEnabled = false;
        private static string _logPath = null;
        private static object _logLock = new object();
        private static void RequireLogPath()
        {
            if (_logPath != null)
            {
                return;
            }
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Db2Src.net", "log");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, string.Format("npgsql-{0}.txt", Process.GetCurrentProcess().Id));
        }
        public static void LogSQL(string text)
        {
            if (!IsSQLLoggingEnabled)
            {
                return;
            }
            RequireLogPath();
            lock (_logLock)
            {
                using (FileStream stream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    string s = string.Format("[{0:HH:mm:ss.fff}] {1}{2}", DateTime.Now, text, Environment.NewLine);
                    byte[] b = Encoding.UTF8.GetBytes(s);
                    stream.Write(b, 0, b.Length);
                }
            }
        }

        public static string DateFormat { get; set; } = "yyyy/MM/dd";
        public static string TimeFormat { get; set; } = "HH:mm:ss";
        public static string[] TimeFormats { get; set; } = new string[] { "HH:mm:ss", "HH:mm" };
        public static string DateTimeFormat { get; set; } = "yyyy/MM/dd HH:mm:ss";
        public static string[] DateTimeFormats { get; set; } = new string[] { "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd HH:mm", "yyyy/MM/dd" };

        public static string ParseDateFormat { get; set; } = "yyyy/M/d";
        public static string ParseTimeFormat { get; set; } = "H:m:s";
        public static string[] ParseTimeFormats { get; set; } = new string[] { "H:m:s", "H:m" };
        public static string ParseDateTimeFormat { get; set; } = "yyyy/M/d H:m:s";
        public static string[] ParseDateTimeFormats { get; set; } = new string[] { "yyyy/M/d H:m:s", "yyyy/M/d H:m", "yyyy/M/d" };

        public virtual Dictionary<string, PropertyInfo> BaseTypeToProperty { get { return null; } }

        public string Name
        {
            get
            {
                return ConnectionInfo?.Name;
            }
            set
            {
                if (ConnectionInfo == null)
                {
                    return;
                }
                ConnectionInfo.Name = value;
            }
        }
        public ConnectionInfo ConnectionInfo { get; private set; }
        public SourceSchemaOption ExportSchemaOption { get; set; } = SourceSchemaOption.OmitCurrent;
        protected internal virtual bool IsHiddenSchema(string schemaName)
        {
            return false;
        }
        public bool ExportTablespace { get; set; } = false;

        public string LastSql { get; protected internal set; }
        public IDbDataParameter[] LastParameter { get; protected internal set; }
        public string GetLastSqlText()
        {
            StringBuilder buf = new StringBuilder();
            buf.AppendLine(LastSql);
            buf.AppendLine("----");
            foreach (IDbDataParameter p in LastParameter)
            {
                buf.Append("-- ");
                buf.Append(p.ParameterName);
                buf.Append(" = ");
                if ((p.Value == null) || (p.Value is DBNull))
                {
                    buf.Append("null");
                }
                else if (p.Value is string)
                {
                    buf.Append(ToLiteralStr((string)p.Value));
                }
                else
                {
                    buf.Append(p.Value.ToString());
                }
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public class SchemaObjectCollection<T> where T : NamedObject
        {
            private Db2SourceContext _owner;
            private PropertyInfo _itemsProperty;
            internal SchemaObjectCollection(Db2SourceContext owner, string itemsPropertyName)
            {
                _owner = owner;
                _itemsProperty = typeof(Schema).GetProperty(itemsPropertyName);
                if (_itemsProperty == null)
                {
                    throw new ArgumentException(string.Format("プロパティ {0} が見つかりません", itemsPropertyName), "itemsPropertyName");
                }
            }
            public T this[string schema, string identifier]
            {
                get
                {
                    if (string.IsNullOrEmpty(identifier))
                    {
                        return null;
                    }
                    string s = string.IsNullOrEmpty(schema) ? _owner.CurrentSchema : schema;
                    Schema sc = _owner.Schemas[s];
                    if (sc == null)
                    {
                        return null;
                    }
                    NamedCollection objs = _itemsProperty.GetValue(sc) as NamedCollection;
                    return objs[identifier] as T;
                    //return sc.Objects[identifier] as T;
                }
            }
            public T this[string name]
            {
                get
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return null;
                    }
                    string[] s = name.Split('.');
                    switch (s.Length)
                    {
                        case 0:
                            return null;
                        case 1:
                            return this[null, s[0]];
                        case 2:
                            return this[s[0], s[1]];
                        default:
                            return null;
                    }
                }
            }
            public void ReleaseAll(string schema)
            {
                foreach (Schema sc in _owner.Schemas)
                {
                    if (!string.IsNullOrEmpty(schema) && sc.Name != schema)
                    {
                        continue;
                    }
                    NamedCollection objs = _itemsProperty.GetValue(sc) as NamedCollection;
                    foreach (SchemaObject o in objs)
                    {
                        if (o is T)
                        {
                            o.Release();
                        }
                    }
                }
            }
        }
        public bool CaseSensitive { get; set; }
        public string CurrentSchema { get; set; }
        public string SystemLocale { get; protected set; }
        public string SessionLocale { get; protected set; }
        public Encoding SystemEncoding { get; protected set; }
        public Encoding SessionEncoding { get; protected set; }
        /// <summary>
        /// SQL出力時の一行あたりの推奨文字数
        /// </summary>
        public int PreferedCharsPerLine { get; set; } = 80;
        public virtual IDbConnection NewConnection()
        {
            return ConnectionInfo?.NewConnection();
        }
        public abstract bool NeedQuotedIdentifier(string value);
        protected static string GetQuotedIdentifier(string value)
        {
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
        protected static bool IsQuotedIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (value.Length < 2)
            {
                return false;
            }
            return (value[0] == '"' && value[value.Length - 1] == '"');
        }
        /// <summary>
        /// 必要に応じて識別子にクオートを付加
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string objectName)
        {
            if (!IsQuotedIdentifier(objectName) && NeedQuotedIdentifier(objectName))
            {
                return GetQuotedIdentifier(objectName);
            }
            return objectName;
        }
        /// <summary>
        /// objectNameで指定した識別子の前に、必要に応じてスキーマを付加して返す
        /// 必要に応じて識別子にクオートを付加
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="objectName"></param>
        /// <param name="baseSchemaName"></param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string schemaName, string objectName, string baseSchemaName)
        {
            return GetEscapedIdentifier(schemaName, new string[] { objectName }, baseSchemaName);
        }
        /// <summary>
        /// objectNamesで指定した識別子を連結し、必要に応じてスキーマを先頭に付加して返す
        /// 必要に応じて識別子にクオートを付加
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="objectNames"></param>
        /// <param name="baseSchemaName"></param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string schemaName, string[] objectNames, string baseSchemaName)
        {
            if (objectNames == null)
            {
                throw new ArgumentNullException("objectNames");
            }
            if (objectNames.Length == 0)
            {
                throw new ArgumentException("objectNames");
            }
            StringBuilder buf = new StringBuilder();
            switch (ExportSchemaOption)
            {
                case SourceSchemaOption.Omitted:
                    break;
                case SourceSchemaOption.OmitCurrent:
                    string sb = string.IsNullOrEmpty(baseSchemaName) ? CurrentSchema : baseSchemaName;
                    if (!string.IsNullOrEmpty(schemaName) && string.Compare(schemaName, sb, true) != 0)
                    {
                        buf.Append(GetEscapedIdentifier(schemaName));
                        buf.Append('.');
                    }
                    break;
                case SourceSchemaOption.Every:
                    buf.Append(GetEscapedIdentifier(schemaName));
                    buf.Append('.');
                    break;
            }
            buf.Append(GetEscapedIdentifier(objectNames[0]));
            for (int i = 1; i < objectNames.Length; i++)
            {
                buf.Append('.');
                buf.Append(GetEscapedIdentifier(objectNames[i]));
            }
            return buf.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DequoteIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            int i = value.IndexOf('"');
            if (i == -1)
            {
                return value;
            }
            StringBuilder buf = new StringBuilder(value);
            bool inQuote = false;
            bool wasQuote = false;
            while (i < buf.Length)
            {
                char c = buf[i];
                if (c == '"')
                {
                    if (!inQuote && wasQuote)
                    {
                        i++;
                    }
                    else
                    {
                        buf.Remove(i, 1);
                    }
                    inQuote = !inQuote;
                }
                else
                {
                    if (char.IsSurrogate(c))
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                wasQuote = (c == '"');
            }
            return buf.ToString();
        }
        public static string ToLiteralStr(string value)
        {
            if (value == null)
            {
                return "null";
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('\'');
            foreach (char c in value)
            {
                if (c == '\'')
                {
                    buf.Append(c);
                }
                buf.Append(c);
            }
            buf.Append('\'');
            return buf.ToString();
        }

        /// <summary>
        /// SQLの記法(クオートの有無、大文字小文字など)を統一化して返す
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract string NormalizeSQL(string sql, CaseRule reservedRule, CaseRule identifierRule);

        public abstract SQLParts SplitSQL(string sql);
        //public abstract IDbCommand[] Execute(SQLParts sqls, ref ParameterStoreCollection parameters);

        public abstract string GetSQL(Table table, string prefix, string postfix, int indent, bool addNewline, bool includePrimaryKey);
        public abstract string GetSQL(View table, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Column column, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Comment comment, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(KeyConstraint constraint, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(ForeignKeyConstraint constraint, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(CheckConstraint constraint, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Constraint constraint, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Trigger trigger, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Index index, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(Sequence sequence, string prefix, string postfix, int indent, bool addNewline, bool ignoreOwned);
        public abstract string GetSQL(Parameter p);
        public abstract string GetSQL(StoredFunction function, string prefix, string postfix, int indent, bool addNewline);
        //public abstract string GetSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(ComplexType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(EnumType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(RangeType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string GetSQL(BasicType type, string prefix, string postfix, int indent, bool addNewline);

        protected abstract void LoadEncodings(IDbConnection connection);
        public abstract string GetServerEncoding();
        public abstract string GetClientEncoding();
        public abstract string[] GetEncodings();

        public static Type GetCommonType(object a, object b)
        {
            if (a == null && b == null)
            {
                return null;
            }
            if (a == null && b != null)
            {
                return b.GetType();
            }
            if (a != null && b == null)
            {
                return a.GetType();
            }
            if (a.GetType() == b.GetType())
            {
                return a.GetType();
            }
            return null;
        }
        public string[] GetAlterSQL(object after, object before)
        {
            Type t = GetCommonType(after, before);
            if (t == null)
            {
                return null;
            }
            if (t == typeof(Table) || t.IsSubclassOf(typeof(Table)))
            {
                return GetAlterTableSQL((Table)after, (Table)before);
            }
            if (t == typeof(View) || t.IsSubclassOf(typeof(View)))
            {
                return GetAlterViewSQL((View)after, (View)before);
            }
            if (t == typeof(Column) || t.IsSubclassOf(typeof(Column)))
            {
                return GetAlterColumnSQL((Column)after, (Column)before);
            }
            if (t == typeof(Comment) || t.IsSubclassOf(typeof(Comment)))
            {
                return GetAlterCommentSQL((Comment)after, (Comment)before);
            }
            return null;
        }
        public abstract string[] GetAlterTableSQL(Table after, Table before);
        public abstract string[] GetAlterViewSQL(View after, View before);
        public abstract string[] GetAlterColumnSQL(Column after, Column before);
        public abstract string[] GetAlterCommentSQL(Comment after, Comment before);
        //public abstract void ApplyChange(DataGridController owner, DataGridController.Row row, IDbConnection connection, IDbTransaction transaction);
        public abstract void ApplyChange(IChangeSet owner, IChangeSetRow row, IDbConnection connection);
        //public abstract void ApplyChangeLog(DataGridController.ChangeLog log, IDbConnection connection, IDbTransaction transaction);
        public static readonly Dictionary<Type, DbType> TypeToDbType = new Dictionary<Type, DbType>()
        {
            { typeof(string), DbType.String },
            { typeof(bool), DbType.Boolean },
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(DateTime), DbType.DateTime },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
        };
        //public static readonly Dictionary<DbType, Type> DbTypeToType = InitDbTypeToType();

        public abstract IDbDataParameter CreateParameterByFieldInfo(ColumnInfo info, object value, bool isOld);
        public NamedCollection<Schema> Schemas { get; } = new NamedCollection<Schema>();
        public SchemaObjectCollection<SchemaObject> Objects { get; private set; }
        public SchemaObjectCollection<Selectable> Selectables { get; private set; }
        public SchemaObjectCollection<Table> Tables { get; private set; }
        public SchemaObjectCollection<View> Views { get; private set; }
        public SchemaObjectCollection<StoredFunction> StoredFunctions { get; private set; }
        public SchemaObjectCollection<Comment> Comments { get; private set; }
        public SchemaObjectCollection<Constraint> Constraints { get; private set; }
        public SchemaObjectCollection<Trigger> Triggers { get; private set; }
        /// <summary>
        /// 文字列のnullをDbNullに置換
        /// </summary>
        /// <param name="value"></param>
        /// <param name="emptyIsNull"></param>
        /// <returns></returns>
        public static object ToDbStr(string value, bool emptyIsNull)
        {
            if (value == null || (emptyIsNull && value == string.Empty))
            {
                return DBNull.Value;
            }
            return value;
        }
        //public abstract void LoadColumn(string schema, string table, IDbConnection connection);
        //public void LoadColumn(string schema, string table)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadColumn(null, null, conn);
        //    }
        //}
        //public abstract void LoadTable(string schema, string table, IDbConnection connection);
        //public void LoadTable(string schema, string table)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadTable(schema, table, conn);
        //    }
        //}

        //public abstract void LoadConstraint(string schema, string table, IDbConnection connection);
        //public void LoadConstraint(string schema, string table)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadConstraint(schema, table, conn);
        //    }
        //}

        //public abstract void LoadTrigger(string schema, string table, IDbConnection connection);
        //public void LoadTrigger(string schema, string table)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadTrigger(schema, table, conn);
        //    }
        //}

        //public abstract void LoadView(string schema, string view, IDbConnection connection);
        //public void LoadView(string schema, string view)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadView(schema, view, conn);
        //    }
        //}

        //public abstract void LoadUser(IDbConnection connection);
        //public void LoadUser()
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadUser(conn);
        //    }
        //}
        //public abstract void LoadStoredFunction(string schema, string objectName, IDbConnection connection);
        //public void LoadStoredFunction(string schema, string objectName)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadStoredFunction(schema, objectName, conn);
        //    }
        //}
        //public abstract void LoadStoredProcedure(string schema, string objectName, IDbConnection connection);
        //public void LoadStoredProcedure(string schema, string objectName)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadStoredProcedure(schema, objectName, conn);
        //    }
        //}
        //public abstract void LoadComment(string schema, string objectName, IDbConnection connection);
        //public void LoadComment(string schema, string objectName)
        //{
        //    using (IDbConnection conn = Connection())
        //    {
        //        LoadComment(schema, objectName, conn);
        //    }
        //}

        public void Clear()
        {
            Schemas.Clear();
            //Dependencies.Clear();
        }

        public event EventHandler<EventArgs> SchemaLoaded;
        public abstract void LoadSchema(IDbConnection connection);
        public void LoadSchema()
        {
            Clear();
            using (IDbConnection conn = NewConnection())
            {
                LoadSchema(conn);
            }
            SchemaLoaded?.Invoke(this, EventArgs.Empty);
        }

        public async Task LoadSchemaAsync()
        {
            await Task.Run(new Action(LoadSchema));
        }

        //internal Schema FindSchema(string schemaName)
        //{
        //    string s = schemaName;
        //    if (string.IsNullOrEmpty(s))
        //    {
        //        s = CurrentSchema;
        //    }
        //    return Schemas[s];
        //}
        internal Schema RequireSchema(string schemaName)
        {
            string s = schemaName;
            if (string.IsNullOrEmpty(s))
            {
                s = CurrentSchema;
            }
            Schema ret = Schemas[s];
            if (ret == null)
            {
                ret = new Schema(this, s);
            }
            return ret;
        }

        public void InvalidateColumns()
        {
            foreach (Schema s in Schemas)
            {
                s.InvalidateColumns();
            }
        }

        public void InvalidateConstraints()
        {
            foreach (Schema s in Schemas)
            {
                s.InvalidateConstraints();
            }
        }

        public void InvalidateTriggers()
        {
            foreach (Schema s in Schemas)
            {
                s.InvalidateTriggers();
            }
        }

        public void InvalidateIndexes()
        {
            foreach (Schema s in Schemas)
            {
                s.InvalidateIndexes();
            }
        }

        public event EventHandler<SchemaObjectReplacedEventArgs> SchemaObjectReplaced;
        protected void OnSchemaObjectReplaced(SchemaObjectReplacedEventArgs e)
        {
            SchemaObjectReplaced?.Invoke(this, e);
        }

        public Table Revert(Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            Schema sch = table.Schema;
            string schnm = sch.Name;
            string objid = table.Identifier;
            Schema.CollectionIndex idx = table.GetCollectionIndex();
            using (IDbConnection conn = NewConnection())
            {
                LoadSchema(conn);
                //LoadTable(schnm, objid, conn);
                //LoadColumn(schnm, objid, conn);
                //LoadComment(schnm, objid, conn);
            }
            Table newObj = sch.GetCollection(idx)[objid] as Table;
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, table));
            return newObj;
        }
        public View Revert(View view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }
            Schema sch = view.Schema;
            string schnm = sch.Name;
            string objid = view.Identifier;
            Schema.CollectionIndex idx = view.GetCollectionIndex();
            using (IDbConnection conn = NewConnection())
            {
                LoadSchema(conn);
                //LoadView(schnm, objid, conn);
                //LoadColumn(schnm, objid, conn);
                //LoadComment(schnm, objid, conn);
            }
            View newObj = sch.GetCollection(idx)[objid] as View;
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, view));
            return newObj;
        }

        public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection);
        public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);
        public abstract IDbCommand GetSqlCommand(StoredFunction function, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);

        public abstract DataTable GetDataTable(string tableName, IDbConnection connection);
        public abstract string GetInsertSql(Table table, int indent, int charPerLine, string postfix);
        public abstract string GetUpdateSql(Table table, string where, int indent, int charPerLine, string postfix);
        public abstract string GetDeleteSql(Table table, string where, int indent, int charPerLine, string postfix);

        private int _changeLogDisabledLevel = 0;
        public bool IsChangeLogDisabled()
        {
            return (0 < _changeLogDisabledLevel);
        }
        public void DisableChangeLog()
        {
            _changeLogDisabledLevel++;
        }
        public void EnableChangeLog()
        {
            _changeLogDisabledLevel--;
        }
        public event EventHandler<LogEventArgs> Log;
        public void OnLog(string text, LogStatus status, string sql)
        {
            Log?.Invoke(this, new LogEventArgs(text, status, sql));
        }

        public void ExecSqls(IEnumerable<string> sqls)
        {
            using (IDbConnection conn = NewConnection())
            {
                foreach (string s in sqls)
                {
                    using (IDbCommand cmd = GetSqlCommand(s, null, conn))
                    {
                        OnLog(s, LogStatus.Aux, null);
                        try
                        {
                            int n = cmd.ExecuteNonQuery();
                            if (0 < n)
                            {
                                OnLog(string.Format("{0}行に影響を与えました", n), LogStatus.Normal, s);
                            }
                        }
                        catch (Exception t)
                        {
                            OnLog("[エラー] " + t.Message, LogStatus.Error, s);
                        }
                    }
                }
            }
        }
        public Db2SourceContext(ConnectionInfo info)
        {
            ConnectionInfo = info;
            Objects = new SchemaObjectCollection<SchemaObject>(this, "Objects");
            Selectables = new SchemaObjectCollection<Selectable>(this, "Objects");
            Tables = new SchemaObjectCollection<Table>(this, "Objects");
            Views = new SchemaObjectCollection<View>(this, "Objects");
            StoredFunctions = new SchemaObjectCollection<StoredFunction>(this, "Objects");
            Comments = new SchemaObjectCollection<Comment>(this, "Comments");
            Constraints = new SchemaObjectCollection<Constraint>(this, "Constraints");
            Triggers = new SchemaObjectCollection<Trigger>(this, "Triggers");
        }

        public override string ToString()
        {
            return ConnectionInfo.Name;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Db2SourceContext))
            {
                return false;
            }
            return ConnectionInfo.Equals(((Db2SourceContext)obj).ConnectionInfo);
        }
        public override int GetHashCode()
        {
            return ConnectionInfo.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Db2SourceContext))
            {
                return -1;
            }
            return ConnectionInfo.CompareTo(((Db2SourceContext)obj).ConnectionInfo);
        }
    }
}
