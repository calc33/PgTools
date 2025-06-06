﻿using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum NamespaceIndex
    {
        None = 0,
        Schemas = 1,
        Objects = 2,
        Columns = 3,
        Constraints = 4,
        Comments = 5,
        Indexes = 6,
        Triggers = 7,
        Sequences = 8,
        Extension = 9,
        Tablespaces = 10,
        Databases = 11,
        Users = 12,
        Sessions = 13,
        ForeignDataWrapper = 14,
        ForeignServer = 15,
    }
	public enum LogStatus
    {
        Normal = 0,
        Error = 1,
        Aux = 2
    }
    public class SQLPart
    {
        public static readonly SQLPart[] EmptyArray = new SQLPart[0];
        public int Offset { get; set; }
        public string SQL { get; set; }
        public string[] ParameterNames { get; set; }
        /// <summary>
        /// false: SQLが空文字列もしくは空白・改行・コメントのみで構成されているため実行不可能
        /// true: SQLは実行可能
        /// </summary>
        public bool IsExecutable { get; set; }

        public override string ToString()
        {
            return SQL;
        }
    }

    public class SQLParts: IEnumerable<SQLPart>
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

        #region IEnumerableの実装
        public IEnumerator<SQLPart> GetEnumerator()
        {
            return ((IEnumerable<SQLPart>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
        #endregion
    }
    public class LogEventArgs : EventArgs
    {
        public LogStatus Status { get; private set; }
        public string Text { get; private set; }
        public IDbCommand Command { get; private set; }

        internal LogEventArgs(string text, LogStatus status, IDbCommand command)
        {
            Status = status;
            Text = text;
            Command = command;
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

    public sealed class DataArray : ICollection<object>
    {
        private readonly object[] _data;
        public DataArray(int length)
        {
            _data = new object[length];
        }
        public DataArray(object[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            _data = new object[data.Length];
            data.CopyTo(_data, 0);
        }

        public object this[int index]
        {
            get
            {
                return _data[index];
            }
            set
            {
                _data[index] = value;
            }
        }

        public int Length
        {
            get
            {
                return _data.Length;
            }
        }

        public int Count
        {
            get
            {
                return _data.Length;
            }
        }

        public object SyncRoot
        {
            get
            {
                return _data.SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return _data.IsSynchronized;
            }
        }

        public bool IsReadOnly => ((ICollection<object>)_data).IsReadOnly;

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DataArray))
            {
                return false;
            }
            DataArray o = (DataArray)obj;
            if (Length != o.Length)
            {
                return false;
            }
            for (int i = 0; i < Length; i++)
            {
                if (!Equals(this[i], o[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (object o in _data)
            {
                int v = o != null ? o.GetHashCode() : 0;
                hash = hash * 17 + v;
            }
            return hash;
        }

        public override string ToString()
        {
            return string.Format("DataArray[{0}]", Length);
        }

        public void CopyTo(Array array, int index)
        {
            _data.CopyTo(array, index);
        }

        public void Add(object item)
        {
            ((ICollection<object>)_data).Add(item);
        }

        public void Clear()
        {
            ((ICollection<object>)_data).Clear();
        }

        public bool Contains(object item)
        {
            return ((ICollection<object>)_data).Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            ((ICollection<object>)_data).CopyTo(array, arrayIndex);
        }

        public bool Remove(object item)
        {
            return ((ICollection<object>)_data).Remove(item);
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return ((IEnumerable<object>)_data).GetEnumerator();
        }
    }

    public interface IChangeSetRows
    {
        IChangeSetRow FindRowByOldKey(DataArray key);
        void AcceptChanges();
        void RevertChanges();
        //ICollection<IChangeSetRow> TemporaryRows { get; }

    }
    public interface IChangeSet
    {
        Selectable Table { get; set; }
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
        DataArray GetKeys();
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

        private static void AddException(List<string> buffer, Exception exception)
        {
            if (exception == null)
            {
                return;
            }
            if (exception is AggregateException)
            {
                AggregateException ae = (AggregateException)exception;
                foreach (Exception t in ae.InnerExceptions)
                {
                    AddException(buffer, t);
                }
                return;
            }
            string message = exception.Message;
            if (exception is FileNotFoundException)
            {
                FileNotFoundException fe = (FileNotFoundException)exception;
                message = fe.FileName + ": " + fe.Message;
            }
            else if (exception is PostgresException)
            {
                // 文字化けが発生していたらローカルで用意したメッセージを表示する
                if (message.Contains((Encoding.UTF8.DecoderFallback as DecoderReplacementFallback).DefaultString))
                {
                    PostgresException pe = (PostgresException)exception;
                    string state = pe.Data["SqlState"].ToString();
                    if (!string.IsNullOrEmpty(state))
                    {
                        string s = DataSet.Properties.Resources.ResourceManager.GetString(state);
                        if (!string.IsNullOrEmpty(s) && s != state)
                        {
                            message = s;
                        }
                    }
                }
            }
            buffer.Add(message);
            AddException(buffer, exception.InnerException);
        }

        public static string[] GetExceptionMessages(Exception exception)
        {
            List<string> list = new List<string>();
            AddException(list, exception);
            return list.ToArray();
        }

        public static string IndentText { get; set; } = "  ";
        public static string GetIndent(int indent)
        {
            return string.IsNullOrEmpty(IndentText) ? string.Empty : new string(IndentText[0], IndentText.Length * indent);
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

        private static int GetCharWidthInternal(string s, ref int index)
        {
            char c1 = s[index++];
            if (char.IsHighSurrogate(c1))
            {
                if (index <= s.Length)
                {
                    return 1;
                }
                char c2 = s[index++];
                if (('\uD840' <= c1 && c1 < '\uD869' && char.IsLowSurrogate(c2)) || (c1 == '\uD869' && '\uDC00' <= c2 && c2 <= '\uDEDF'))
                {
                    return 2;
                }
            }
            if ('\u3000' <= c1 && c1 <= '\u4DBF' || '\u4E00' <= c1 && c1 <= '\u9FCF' || '\uF900' <= c1 && c1 <= '\uFAFF' || '\uFF00' <= c1 && c1 <= '\uFF5F')
            {
                return 2;
            }
            return 1;
        }
        public static int GetCharWidth(string s)
        {
            int i = 0;
            int n = s.Length;
            int l = 0;
            while (i < n)
            {
                l += GetCharWidthInternal(s, ref i);
            }
            return l;
        }

        private SettingCollection InitSettings()
        {
            SettingCollection l = new SettingCollection
            {
                new RedirectedPropertySetting<string>("DateFormat", DataSet.Db2srcDataSet.DateFormat, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("TimeFormat", DataSet.Db2srcDataSet.TimeFormat, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("DateTimeFormat", DataSet.Db2srcDataSet.DateTimeFormat, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string>("ParseDateFormat", DataSet.Db2srcDataSet.ParseDateFormat, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string[]>("ParseTimeFormats", DataSet.Db2srcDataSet.ParseTimeFormats, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<string[]>("ParseDateTimeFormats", DataSet.Db2srcDataSet.ParseDateTimeFormats, typeof(Db2SourceContext)),
                new RedirectedPropertySetting<NewLineRule>("NewLineRule", DataSet.Db2srcDataSet.NewLineRule, this)
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

        public QueryHistory History { get; private set; }

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
            if (NewLineRuleToNewLine.TryGetValue(NewLineRule, out string nl))
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
            if (!NewLineRuleToNewLine.TryGetValue(NewLineRule, out string nl))
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
            if (!NewLineRuleToNewLine.TryGetValue(NewLineRule, out string nl))
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
                else if (p.Value is string str)
                {
                    buf.Append(ToLiteralStr(str));
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
            private readonly int _identifierDepth;
            internal SchemaObjectCollection(Db2SourceContext owner, string itemsPropertyName, int identifierDepth)
            {
                _owner = owner;
                _itemsProperty = typeof(Schema).GetProperty(itemsPropertyName);
                _identifierDepth = identifierDepth;
                if (_itemsProperty == null)
                {
                    throw new ArgumentException(string.Format(DataSet.Db2srcDataSet.MessagePropertyNotFound, itemsPropertyName), "itemsPropertyName");
                }
            }
            public T this[string schema, string table, string identifier]
            {
                get
                {
                    if (_identifierDepth < 3)
                    {
                        return null;
                    }
                    if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(identifier))
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
                    string id = JointIdentifier(table, identifier);
                    return objs[id] as T;
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
                        case 3:
                            return this[s[0], s[1], s[2]];
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
        public Database Database { get; set; }
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
        public virtual IDbConnection NewConnection(bool withOpening, int commandTimeout)
        {
            return ConnectionInfo?.NewConnection(withOpening, commandTimeout);
        }
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
                    if (!string.IsNullOrEmpty(schemaName))
                    {
                        buf.Append(GetEscapedIdentifier(schemaName, strict));
                        buf.Append('.');
                    }
                    break;
            }
            bool needDot = false;
            foreach (string s in objectNames)
            {
                if (needDot)
                {
                    buf.Append('.');
                }
                buf.Append(GetEscapedIdentifier(s, strict));
                needDot = true;
            }
            return buf.ToString();
        }
        public static string JointIdentifier(string id1, string id2)
        {
            return id1 + "." + id2;
        }
        public static string JointIdentifier(string id1, string id2, string id3)
        {
            return id1 + "." + id2 + "." + id3;
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

        /// <summary>
        /// 複数SQLが列挙された文字列を個々のSQLに分割する
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract SQLParts SplitSQL(string sql);
        //public abstract IDbCommand[] Execute(SQLParts sqls, ref ParameterStoreCollection parameters);

        public abstract Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken);

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
		public abstract string[] GetSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool addNewline);
		public string[] GetSQL(StoredProcedureBase procedure, string prefix, string postfix, int indent, bool addNewline)
        {
            if (procedure is StoredProcedure)
            {
                return GetSQL((StoredProcedure)procedure, prefix, postfix, indent, addNewline);
            }
            if (procedure is StoredFunction)
            {
                return GetSQL((StoredFunction)procedure, prefix, postfix, indent, addNewline);
            }
            throw new NotImplementedException();
        }
		public abstract string[] GetSQL(Type_ type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(ComplexType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlEnumType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlRangeType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(PgsqlBasicType type, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Tablespace tablespace, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(User user, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetSQL(Schema schema, string prefix, string postfix, int indent, bool addNewline);

        public abstract string[] GetAlterSQL(Tablespace after, Tablespace before, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetAlterSQL(User after, User before, string prefix, string postfix, int indent, bool addNewline);
        public abstract string[] GetAlterSQL(Trigger after, Trigger before, string prefix, string postfix, int indent, bool addNewline);

        public abstract string[] GetDropSQL(SchemaObject table, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Table table, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(View table, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Column column, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Comment comment, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Constraint constraint, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Trigger trigger, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Index index, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline );
        public abstract string[] GetDropSQL(Sequence sequence, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
		public abstract string[] GetDropSQL(StoredFunction function, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
		public abstract string[] GetDropSQL(StoredProcedure procedure, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public string[] GetDropSQL(StoredProcedureBase procedure, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (procedure is StoredProcedure)
            {
                return GetDropSQL((StoredProcedure)procedure, ifExists, prefix, postfix, indent, cascade, addNewline);
            }
            if (procedure is StoredFunction)
            {
                return GetDropSQL((StoredFunction)procedure, ifExists, prefix, postfix, indent, cascade, addNewline);
            }
            throw new NotImplementedException();
        }
		public abstract string[] GetDropSQL(ComplexType type, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlEnumType type, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlRangeType type, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(PgsqlBasicType type, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(Tablespace tablespace, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public abstract string[] GetDropSQL(User user, bool ifExists, string prefix, string postfix, int indent, bool cascade, bool addNewline);

        public abstract string[] GetResetSequenceSQL(Sequence sequence, string prefix, string postfix, int indent, bool addNewline);

		protected abstract void LoadEncodings(IDbConnection connection);
        public abstract string GetServerEncoding();
        public abstract string GetClientEncoding();
        public abstract string[] GetEncodings();

        public abstract void ChangeUserPassword(User user, string password, DateTime? expiration, EventHandler<LogEventArgs> logEvent);

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
		
        private NamedCollection[] _namespaces;
        public NamedCollection GetNamedCollection(NamespaceIndex index)
        {
            int i = (int)index;
            if (i < 0 || _namespaces.Length <= i)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return _namespaces[i];
        }
        public T Find<T>(NamedObjectId id) where T: NamedObject
        {
            return GetNamedCollection(id.Index)?[id.Identifier] as T;
        }
        public T Find<T>(NamespaceIndex index, string identifier) where T: NamedObject
        {
			return GetNamedCollection(index)?[identifier] as T;
		}
        public T FindRegistered<T>(T obj) where T: NamedObject
        {
            return Find<T>(new NamedObjectId(obj));
        }

		public NamedCollection<Schema> Schemas { get; } = new NamedCollection<Schema>();
		public NamedCollection<SchemaObject> Objects { get; } = new NamedCollection<SchemaObject>();
		public FilteredNamedCollection<Selectable> Selectables { get; }
		public FilteredNamedCollection<Table> Tables { get; }
		public FilteredNamedCollection<View> Views { get; }
		public FilteredNamedCollection<StoredFunction> StoredFunctions { get; }
		public FilteredNamedCollection<StoredProcedure> StoredProcedures { get; }
		public NamedCollection<Column> Columns { get; } = new NamedCollection<Column>();
		public NamedCollection<Constraint> Constraints { get; } = new NamedCollection<Constraint>();
		public NamedCollection<Comment> Comments { get; } = new NamedCollection<Comment>();
		public NamedCollection<Index> Indexes { get; } = new NamedCollection<Index>();
		public NamedCollection<Trigger> Triggers { get; } = new NamedCollection<Trigger>();
		public NamedCollection<Sequence> Sequences { get; } = new NamedCollection<Sequence>();
		public NamedCollection<PgsqlExtension> Extensions { get; } = new NamedCollection<PgsqlExtension>();
		public NamedCollection<Tablespace> Tablespaces { get; } = new NamedCollection<Tablespace>();
		public NamedCollection<Database> Databases { get; } = new NamedCollection<Database>();
		public NamedCollection<User> Users { get; } = new NamedCollection<User>();
		public NamedCollection<SessionList> Sessions { get; } = new NamedCollection<SessionList>();
        public NamedCollection<PgsqlForeignDataWrapper> ForeignDataWrappers { get; } = new NamedCollection<PgsqlForeignDataWrapper>();
		public NamedCollection<PgsqlForeignServer> ForeignServers { get; } = new NamedCollection<PgsqlForeignServer>();

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
        public abstract void RefreshSettings(IDbConnection connection);
        public abstract void RefreshTablespaces(IDbConnection connection);

		public abstract long? GetCurrentSequenceValue(Sequence sequence, IDbConnection connection);
		public abstract void SetSequenceValue(long value, Sequence sequence, IDbConnection connection);
		public abstract long? GetMaxValueOfColumn(Column column, IDbConnection connection);
		public abstract long? GetMinValueOfColumn(Column column, IDbConnection connection);

		public void RefreshTablespaces()
        {
            using (IDbConnection conn = NewConnection(true))
            {
                RefreshTablespaces(conn);
            }
        }
        public abstract void RefreshUsers(IDbConnection connection);
        public void RefreshUsers()
        {
            using (IDbConnection conn = NewConnection(true))
            {
                RefreshUsers(conn);
            }
        }

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
            Columns.Invalidate();
        }

        public void InvalidateConstraints()
        {
            Constraints.Invalidate();
        }

        public void InvalidateTriggers()
        {
            Triggers.Invalidate();
        }

        public void InvalidateIndexes()
        {
            Indexes.Invalidate();
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
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(table, conn);
            }
            Table newObj = FindRegistered(table);
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, table));
            return newObj;
        }
        public View Revert(View view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(view, conn);
            }
            View newObj = FindRegistered(view);
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, view));
            return newObj;
        }

        public ComplexType Revert(ComplexType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            using (IDbConnection conn = NewConnection(true))
            {
                LoadSchema(conn, false);
                //LoadView(schnm, objid, conn);
                //LoadColumn(schnm, objid, conn);
                //LoadComment(schnm, objid, conn);
            }
            ComplexType newObj = FindRegistered(type);
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, type));
            return newObj;
        }

        public StoredFunction Revert(StoredFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }
            using (IDbConnection conn = NewConnection(true))
            {
                Refresh(function, conn);
            }
            StoredFunction newObj = FindRegistered(function);
            OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, function));
            return newObj;
        }


		public StoredProcedure Revert(StoredProcedure procedure)
		{
			if (procedure == null)
			{
				throw new ArgumentNullException("procedure");
			}
			using (IDbConnection conn = NewConnection(true))
			{
				Refresh(procedure, conn);
			}
			StoredProcedure newObj = FindRegistered(procedure);
			OnSchemaObjectReplaced(new SchemaObjectReplacedEventArgs(newObj, procedure));
			return newObj;
		}

		public StoredProcedureBase Revert(StoredProcedureBase procedure)
		{
			if (procedure == null)
			{
				throw new ArgumentNullException("procedure");
			}
            if (procedure is StoredProcedure)
            {
                return Revert((StoredProcedure)procedure);
            }
            if (procedure is StoredFunction)
            {
                return Revert((StoredFunction)procedure);
            }
            throw new NotImplementedException();
		}

		public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection);
        public abstract IDbCommand GetSqlCommand(string sqlCommand, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);
		public abstract IDbCommand GetSqlCommand(StoredFunction function, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);
		public abstract IDbCommand GetSqlCommand(StoredProcedure procedure, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction);
		public IDbCommand GetSqlCommand(StoredProcedureBase procedure, EventHandler<LogEventArgs> logEvent, IDbConnection connection, IDbTransaction transaction)
        {
            if (procedure is StoredFunction)
            {
                return GetSqlCommand((StoredFunction)procedure, logEvent, connection, transaction);
            }
            if (procedure is StoredProcedure)
            {
                return GetSqlCommand((StoredProcedure)procedure, logEvent, connection, transaction);
            }
            throw new NotImplementedException();
        }

		public abstract DataTable GetDataTable(string tableName, IDbConnection connection);
        public abstract string GetInsertSql(Table table, int indent, int charPerLine, string postfix, bool noNewLine);
        /// <summary>
        /// データ付でINSERT文を生成する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="indent"></param>
        /// <param name="charPerLine"></param>
        /// <param name="postfix"></param>
        /// <param name="data">項目と値の組み合わせを渡す</param>
        /// <returns></returns>
        public abstract string GetInsertSql(Table table, int indent, int charPerLine, string postfix, Dictionary<ColumnInfo, object> data, bool noNewLine);
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
        public abstract string GetInsertUpdateSql(Table table, int indent, int charPerLine, string postfix);
        public abstract string GetMergeSql(Table table, int indent, int charPerLine, string postfix);

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
        public void OnLog(string text, LogStatus status, IDbCommand command)
        {
            Log?.Invoke(this, new LogEventArgs(text, status, command));
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
                                OnLog(string.Format(DataSet.Db2srcDataSet.MessageRowsAffected, n), LogStatus.Normal, cmd);
                            }
                        }
                        catch (Exception t)
                        {
                            OnLog(DataSet.Db2srcDataSet.PrefixError + t.Message, LogStatus.Error, cmd);
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
        public int ExecSql(string sql, EventHandler<LogEventArgs> logEvent, bool forceDisconnect = false)
        {
            int n = 0;
            using (IDbConnection conn = NewConnection(true))
            {
                using (IDbCommand cmd = GetSqlCommand(sql, logEvent, conn))
                {
                    n = cmd.ExecuteNonQuery();
                }
                if (forceDisconnect)
                {
                    conn.Close();
                }
            }
            return n;
        }

        /// <summary>
        /// SQLを実行します。
        /// エラーはログメッセージとして出力されます。
        /// </summary>
        /// <param name="sql"></param>
        public void ExecSqlWithLog(string sql, bool forceDisconnect = false)
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
                            OnLog(string.Format(DataSet.Db2srcDataSet.MessageRowsAffected, n), LogStatus.Normal, cmd);
                        }
                    }
                    catch (Exception t)
                    {
                        OnLog(DataSet.Db2srcDataSet.PrefixError + t.Message, LogStatus.Error, cmd);
                    }
                }
                if (forceDisconnect)
                {
                    conn.Close();
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
            if (!TypeToDbType.TryGetValue(type, out DbType ret))
            {
                throw new ArgumentException();
            }
            return ret;
        }

        public abstract SchemaObject[] GetStrongReferred(SchemaObject target);

        public Db2SourceContext(ConnectionInfo info)
        {
            ConnectionInfo = info;
            _namespaces = new NamedCollection[]
            {
                null,
                Schemas,
                Objects,
                Columns,
                Constraints,
                Comments,
                Indexes,
                Triggers,
                Sequences,
                Extensions,
                Tablespaces,
                Databases,
                Users,
                Sessions,
				ForeignDataWrappers,
				ForeignServers,
			};
            Selectables = new FilteredNamedCollection<Selectable>(Objects);
		    Tables = new FilteredNamedCollection<Table>(Objects);
            Views = new FilteredNamedCollection<View>(Objects);
			StoredFunctions = new FilteredNamedCollection<StoredFunction>(Objects);
			StoredProcedures = new FilteredNamedCollection<StoredProcedure>(Objects);

			SessionList = new SessionList(this, "Sessions");
    		History = new QueryHistory(ConnectionInfo);
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
