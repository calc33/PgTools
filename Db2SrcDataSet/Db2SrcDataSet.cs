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
                return Items?[index];
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
        void SetError(Exception t);
        void SetError(string message);
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

    public enum NewLineRule
    {
        None,
        Lf,
        Cr,
        Crlf
    }

    public class NameValue
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public interface ISession
    {
        //string GetId();
        bool CanKill();
        bool CanAbortQuery();
        bool Kill(IDbConnection connection);
        bool AbortQuery(IDbConnection connection);
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
        private static readonly object _logLock = new object();
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

        private SettingCollection InitSettings()
        {
            SettingCollection l = new SettingCollection
            {
                new RedirectedPropertySetting<string>("DateFormat", "日付書式(表示)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("TimeFormat", "時刻書式(表示)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("DateTimeFormat", "日時書式(表示)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("ParseDateFormat", "日付書式(入力)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string[]>("ParseTimeFormats", "時刻書式(入力)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string[]>("ParseDateTimeFormats", "日時書式(入力)", typeof(Db2SourceContext)),
                new RedirectedPropertySetting<NewLineRule>("NewLineRule", "改行文字", this)
            };
            return l;
        }
        private SettingCollection _settings = null;
        public virtual SettingCollection Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = InitSettings();
                }
                return _settings;
            }
        }

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

        public string[] UserIds { get; set; }
        public string[] TablespaceNames { get; set; }

        public SessionList SessionList { get; private set; }

        public string GetTreeNodeHeader()
        {
            return ConnectionInfo?.GetTreeNodeHeader();
        }
        public ConnectionInfo ConnectionInfo { get; private set; }
        public SourceSchemaOption ExportSchemaOption { get; set; } = SourceSchemaOption.OmitCurrent;

        public NewLineRule NewLineRule { get; set; } = NewLineRule.None;

        public static readonly Dictionary<NewLineRule, string> NewLineRuleToNewLine = new Dictionary<NewLineRule, string>()
        { 
            { NewLineRule.None, null },
            { NewLineRule.Cr, "\r" },
            { NewLineRule.Lf, "\n" },
            { NewLineRule.Crlf, "\r\n" },
        };

        public string GetNewLine()
        {
            if (NewLineRule == NewLineRule.None)
            {
                return Environment.NewLine;
            }
            string nl;
            if (NewLineRuleToNewLine.TryGetValue(NewLineRule, out nl))
            {
                return nl;
            }
            return Environment.NewLine;
        }
        public string NormalizeNewLine(StringBuilder value)
        {
            if (NewLineRule == NewLineRule.None)
            {
                return value.ToString();
            }
            StringBuilder buf = new StringBuilder(value.Length);
            string nl;
            if (!NewLineRuleToNewLine.TryGetValue(NewLineRule, out nl))
            {
                return value.ToString();
            }
            bool wasCR = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\n')
                {
                    buf.Append(nl);
                }
                else
                {
                    if (wasCR)
                    {
                        buf.Append(nl);
                    }
                    if (c != '\r')
                    {
                        buf.Append(c);
                    }
                }
                wasCR = (c == '\r');
            }
            if (wasCR)
            {
                buf.Append(nl);
            }
            return buf.ToString();
        }

        public string NormalizeNewLine(string value)
        {
            if (NewLineRule == NewLineRule.None)
            {
                return value;
            }
            StringBuilder buf = new StringBuilder(value.Length);
            string nl;
            if (!NewLineRuleToNewLine.TryGetValue(NewLineRule, out nl))
            {
                return value;
            }
            bool wasCR = false;
            foreach (char c in value)
            {
                if (c == '\n')
                {
                    buf.Append(nl);
                }
                else
                {
                    if (wasCR)
                    {
                        buf.Append(nl);
                    }
                    if (c != '\r')
                    {
                        buf.Append(c);
                    }
                }
                wasCR = (c == '\r');
            }
            if (wasCR)
            {
                buf.Append(nl);
            }
            return buf.ToString();
        }

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
            private readonly Db2SourceContext _owner;
            private readonly PropertyInfo _itemsProperty;
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
        public CaseRule ReservedWordCaseRule { get; set; } = CaseRule.Lowercase;
        public CaseRule IdentifierCaseRule { get; set; } = CaseRule.Lowercase;
        /// <summary>
        /// SQL出力時の一行あたりの推奨文字数
        /// </summary>
        public int PreferredCharsPerLine { get; set; } = 80;
        public virtual IDbConnection NewConnection(bool withOpening)
        {
            return ConnectionInfo?.NewConnection(withOpening);
        }
        public abstract bool NeedQuotedIdentifier(string value, bool strict);
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
        /// <param name="strict">
        /// false: 文字列はユーザーが入力した文字列なので大文字小文字の区別が曖昧である
        /// true: 文字列はDBの定義情報を渡しているので大文字小文字の区別が厳格(大文字が渡された場合も引用符でエスケープする)
        /// </param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string objectName, bool strict)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return objectName;
            }
            if (!IsQuotedIdentifier(objectName) && NeedQuotedIdentifier(objectName, strict))
            {
                return GetQuotedIdentifier(objectName);
            }
            return objectName;
        }
        public object Eval(string expression)
        {
            using (IDbConnection conn = NewConnection(true))
            {
                return Eval(expression, conn);
            }
        }
        public abstract object Eval(string expression, IDbConnection connection);
        /// <summary>
        /// SQL文中に表示する値
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string GetImmediatedStr(ColumnInfo column, object value)
        {
            if (value == null || value is DBNull)
            {
                return "NULL";
            }
            if (column.IsNumeric)
            {
                return value.ToString();
            }
            if (column.IsDateTime)
            {
                DateTime dt = Convert.ToDateTime(value);
                if (dt.TimeOfDay == TimeSpan.Zero)
                {
                    return string.Format("TO_DATE('{0:yyyy-M-d}', 'YYYY-MM-DD')", dt);
                }
                return string.Format("TO_DATE('{0:yyyy-M-d HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')", dt);
            }
            return ToLiteralStr(value.ToString());
        }

        /// <summary>
        /// objectNameで指定した識別子の前に、必要に応じてスキーマを付加して返す
        /// 必要に応じて識別子にクオートを付加
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="objectName"></param>
        /// <param name="baseSchemaName"></param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string schemaName, string objectName, string baseSchemaName, bool strict)
        {
            return GetEscapedIdentifier(schemaName, new string[] { objectName }, baseSchemaName, strict);
        }
        /// <summary>
        /// objectNamesで指定した識別子を連結し、必要に応じてスキーマを先頭に付加して返す
        /// 必要に応じて識別子にクオートを付加
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="objectNames"></param>
        /// <param name="baseSchemaName"></param>
        /// <returns></returns>
        public string GetEscapedIdentifier(string schemaName, string[] objectNames, string baseSchemaName, bool strict)
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
                        buf.Append(GetEscapedIdentifier(schemaName, strict));
                        buf.Append('.');
                    }
                    break;
                case SourceSchemaOption.Every:
                    buf.Append(GetEscapedIdentifier(schemaName, strict));
                    buf.Append('.');
                    break;
            }
            buf.Append(GetEscapedIdentifier(objectNames[0], strict));
            for (int i = 1; i < objectNames.Length; i++)
            {
                buf.Append('.');
                buf.Append(GetEscapedIdentifier(objectNames[i], strict));
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
        public string NormalizeSQL(string sql)
        {
            return NormalizeSQL(sql, ReservedWordCaseRule, IdentifierCaseRule);
        }

        public abstract SQLParts SplitSQL(string sql);
        //public abstract IDbCommand[] Execute(SQLParts sqls, ref ParameterStoreCollection parameters);

        public abstract string[] GetSQL(Table table, string prefix, string postfix, int indent, bool addNewline, bool includePrimaryKey);
        public abstract string[] GetSQL(View table, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Column column, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Comment comment, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(KeyConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline);
        public abstract string[] GetSQL(ForeignKeyConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline);
        public abstract string[] GetSQL(CheckConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline);
        public abstract string[] GetSQL(Constraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline);
        public abstract string[] GetSQL(Trigger trigger, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Index index, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Sequence sequence, string prefix, string postfix, int indent, bool addNewline, bool skipOwned, bool ignoreOwned);
        public abstract string[] GetSQL(Parameter p);
        public abstract string[] GetSQL(StoredFunction function, string prefix, string postfix, int indent, bool addNewline);
        //public abstract string[] GetSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(ComplexType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlEnumType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlRangeType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlBasicType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Tablespace tablespace, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(User user, string prefix, string postfix, int indent, bool addNewline);

        public abstract string[] GetAlterSQL(Tablespace after, Tablespace before, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetAlterSQL(User after, User before, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetAlterSQL(Trigger after, Trigger before, string prefix, string postfix, int indent, bool addNewline);

        public abstract string[] GetDropSQL(SchemaObject table, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Table table, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(View table, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Column column, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Comment comment, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Constraint constraint, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Trigger trigger, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Index index, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Sequence sequence, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(StoredFunction function, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        //public abstract string[] GetDropSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(ComplexType type, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlEnumType type, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlRangeType type, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlBasicType type, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Tablespace tablespace, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(User user, string prefix, string postfix, int indent, bool cascade, bool addNewline);

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
        public abstract void ApplyChange(IChangeSet owner, IChangeSetRow row, IDbConnection connection, IDbTransaction transaction, Dictionary<IChangeSetRow, bool> applied);
        public virtual void ApplyChange(IChangeSet owner, IChangeSetRow row, IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            Dictionary<IChangeSetRow, bool> applied = new Dictionary<IChangeSetRow, bool>();
            IDbTransaction txn = connection.BeginTransaction();
            try
            {
                ApplyChange(owner, row, connection, txn, applied);
                txn.Commit();
                row.AcceptChanges();
            }
            catch
            {
                txn.Rollback();
                throw;
            }
            finally
            {
                txn.Dispose();
            }
        }
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

        public abstract IDbDataParameter ApplyParameterByFieldInfo(IDataParameterCollection parameters, ColumnInfo info, object value, bool isOld);
        public abstract IDbDataParameter CreateParameterByFieldInfo(ColumnInfo info, object value, bool isOld);
        public NamedCollection<Schema> Schemas { get; } = new NamedCollection<Schema>();
        public NamedCollection<User> Users { get; } = new NamedCollection<User>();
        public NamedCollection<Tablespace> Tablespaces { get; } = new NamedCollection<Tablespace>();
        public SchemaObjectCollection<SchemaObject> Objects { get; private set; }
        public SchemaObjectCollection<Selectable> Selectables { get; private set; }
        public SchemaObjectCollection<Table> Tables { get; private set; }
        public SchemaObjectCollection<View> Views { get; private set; }
        public SchemaObjectCollection<StoredFunction> StoredFunctions { get; private set; }
        public SchemaObjectCollection<Comment> Comments { get; private set; }
        public SchemaObjectCollection<Constraint> Constraints { get; private set; }
        public SchemaObjectCollection<Trigger> Triggers { get; private set; }
        public SchemaObjectCollection<Sequence> Sequences { get; private set; }
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
            Tablespaces.Clear();
            Users.Clear();
            //Dependencies.Clear();
        }

        public event EventHandler<EventArgs> SchemaLoaded;
        protected void OnSchemaLoaded()
        {
            SchemaLoaded?.Invoke(this, EventArgs.Empty);
        }
        public abstract void LoadSchema(IDbConnection connection, bool clearBeforeLoad);
        public abstract SchemaObject Refresh(SchemaObject obj, IDbConnection connection);
        public SchemaObject Refresh(SchemaObject obj)
        {
            using (IDbConnection conn = NewConnection(true))
            {
                return Refresh(obj, conn);
            }
        }

        public void LoadSchema()
        {
            using (IDbConnection conn = NewConnection(true))
            {
                LoadSchema(conn, true);
            }
        }

        public async Task LoadSchemaAsync(IDbConnection connection)
        {
            await Task.Run(() => LoadSchema(connection, true));
        }
        public async Task LoadSchemaAsync()
        {
            await Task.Run(() => LoadSchema());
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
            if (ret == null && !string.IsNullOrEmpty(s))
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
            //string schnm = sch.Name;
            string objid = table.Identifier;
            Schema.CollectionIndex idx = table.GetCollectionIndex();
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(table, conn);
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
            //string schnm = sch.Name;
            string objid = view.Identifier;
            Schema.CollectionIndex idx = view.GetCollectionIndex();
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(view, conn);
                //LoadSchema(conn, false);
                //LoadView(schnm, objid, conn);
                //LoadColumn(schnm, objid, conn);
                //LoadComment(schnm, objid, conn);
            }
            View newObj = sch.GetCollection(idx)[objid] as View;
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, view));
            return newObj;
        }

        public ComplexType Revert(ComplexType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Schema sch = type.Schema;
            //string schnm = sch.Name;
            string objid = type.Identifier;
            Schema.CollectionIndex idx = type.GetCollectionIndex();
            using (IDbConnection conn = NewConnection(true))
            {
                LoadSchema(conn, false);
                //LoadView(schnm, objid, conn);
                //LoadColumn(schnm, objid, conn);
                //LoadComment(schnm, objid, conn);
            }
            ComplexType newObj = sch.GetCollection(idx)[objid] as ComplexType;
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, type));
            return newObj;
        }

        public StoredFunction Revert(StoredFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }
            Schema sch = function.Schema;
            string objid = function.Identifier;
            Schema.CollectionIndex idx = function.GetCollectionIndex();
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(function, conn);
            }
            StoredFunction newObj = sch.GetCollection(idx)[objid] as StoredFunction;
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, function));
            return newObj;
        }

        public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection);
        public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);
        public abstract IDbCommand GetSqlCommand(StoredFunction function, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);

        public abstract DataTable GetDataTable(string tableName, IDbConnection connection);
        public abstract string GetInsertSql(Table table, int indent, int charPerLine, string postfix);
        /// <summary>
        /// データ付でINSERT文を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="charPerLine"></param>
        /// <param name="postfix"></param>
        /// <param name="data">項目と値の組み合わせを渡す</param>
        /// <returns></returns>
        public abstract string GetInsertSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data);
        public abstract string GetUpdateSql(Table table, string where, int indent, int charPerLine, string postfix);
        /// <summary>
        /// データ付でUPDATE文を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="indent"></param>
        /// <param name="charPerLine"></param>
        /// <param name="postfix"></param>
        /// <param name="data">項目と値の組み合わせを渡す</param>
        /// <param name="keys">抽出条件に使用する項目と値の組み合わせを渡す</param>
        /// <returns></returns>
        public abstract string GetUpdateSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data, Dictionary<ColumnInfo, object> keys);
        public abstract string GetDeleteSql(Table table, string where, int indent, int charPerLine, string postfix);
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
        public abstract string GetDeleteSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> keys);

        /// <summary>
        /// COPY文(PostgreSQL固有SQL文)の宣言部分を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="postfix"></param>
        /// <param name="columns">出力する項目の順番を指定する</param>
        /// <returns></returns>
        public abstract string GetCopySql(Table table, int indent, string postfix, ColumnInfo[] columns);
        /// <summary>
        /// COPY文(PostgreSQL固有SQL文)のデータ部分を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="columns">出力する項目の順番を指定する</param>
        /// <param name="data">出力するデータの配列</param>
        /// <returns></returns>
        public abstract string GetCopyDataSql(Table table, ColumnInfo[] columns, object[][] data);

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

        /// <summary>
        /// 複数のSQLを実行します。SQLの実行はエラーが出たところで停止します。
        /// </summary>
        /// <param name="sqls"></param>
        /// <param name="logEvent"></param>
        /// <returns>最後に実行したSQLで影響を受けた行数</returns>
        public int ExecSqls(IEnumerable<string> sqls, EventHandler<LogEventArgs> logEvent)
        {
            int rowsAffected = 0;
            using (IDbConnection conn = NewConnection(true))
            {
                foreach (string s in sqls)
                {
                    using (IDbCommand cmd = GetSqlCommand(s, logEvent, conn))
                    {
                        rowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            return rowsAffected;
        }

        /// <summary>
        /// 複数のSQLを実行します。SQLの実行はエラーが出たところで停止します。
        /// エラーはログメッセージとして出力されます。
        /// </summary>
        /// <param name="sqls"></param>
        public void ExecSqlsWithLog(IEnumerable<string> sqls)
        {
            using (IDbConnection conn = NewConnection(true))
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

        /// <summary>
        /// SQLを実行します。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="logEvent"></param>
        /// <returns>影響を受けた行数</returns>
        public int ExecSql(string sql, EventHandler<LogEventArgs> logEvent)
        {
            int n = 0;
            using (IDbConnection conn = NewConnection(true))
            {
                using (IDbCommand cmd = GetSqlCommand(sql, logEvent, conn))
                {
                    n = cmd.ExecuteNonQuery();
                }
            }
            return n;
        }

        /// <summary>
        /// SQLを実行します。
        /// エラーはログメッセージとして出力されます。
        /// </summary>
        /// <param name="sql"></param>
        public void ExecSqlWithLog(string sql)
        {
            using (IDbConnection conn = NewConnection(true))
            {
                using (IDbCommand cmd = GetSqlCommand(sql, null, conn))
                {
                    OnLog(sql, LogStatus.Aux, null);
                    try
                    {
                        int n = cmd.ExecuteNonQuery();
                        if (0 < n)
                        {
                            OnLog(string.Format("{0}行に影響を与えました", n), LogStatus.Normal, sql);
                        }
                    }
                    catch (Exception t)
                    {
                        OnLog("[エラー] " + t.Message, LogStatus.Error, sql);
                    }
                }
            }
        }

        public abstract ISession[] GetSessions(IDbConnection connection);
        public ISession[] GetSessions()
        {
            using (IDbConnection conn = NewConnection(true))
            {
                return GetSessions(conn);
            }
        }

        public virtual DbType ToDbType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            DbType ret;
            if (!TypeToDbType.TryGetValue(type, out ret))
            {
                throw new ArgumentException();
            }
            return ret;
        }

        public abstract SchemaObject[] GetStrongReferred(SchemaObject target);

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
            Sequences = new SchemaObjectCollection<Sequence>(this, "Sequences");
            SessionList = new SessionList(this, "Sessions");
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
