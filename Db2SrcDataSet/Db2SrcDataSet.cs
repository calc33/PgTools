using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

    public class NamedObject : IComparable
    {
        private string _identifier;
        public string Identifier
        {
            get { return _identifier; }
            set
            {
                if (_identifier == value)
                {
                    return;
                }
                _identifier = value;
                IdentifierChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        internal event EventHandler IdentifierChanged;
        internal NamedObject(NamedCollection owner)
        {
            if (owner != null)
            {
                owner.Add(this);
            }
        }
        internal NamedObject(NamedCollection owner, string identifier)
        {
            Identifier = identifier;
            if (owner != null)
            {
                owner.Add(this);
            }
        }
        public virtual bool IsModified() { return false; }
        public virtual void Release() { }

        public override string ToString()
        {
            return _identifier;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Identifier == ((NamedObject)obj).Identifier;
        }
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(Identifier))
            {
                return 0;
            }
            return Identifier.GetHashCode();
        }
        public virtual int CompareTo(object obj)
        {
            if (obj == null)
            {
                return -1;
            }
            if (GetType() != obj.GetType())
            {
                return -1;
            }
            return string.Compare(Identifier, (((NamedObject)obj).Identifier));
        }
    }
    public class NamedCollection : ICollection<NamedObject>
    {
        internal List<NamedObject> _list = new List<NamedObject>();
        Dictionary<string, NamedObject> _nameDict = null;

        private void RequireNameDict()
        {
            if (_nameDict != null)
            {
                return;
            }
            _nameDict = new Dictionary<string, NamedObject>();
            foreach (NamedObject item in _list)
            {
                if (string.IsNullOrEmpty(item.Identifier))
                {
                    continue;
                }
                _nameDict[item.Identifier] = item;
            }
        }

        public NamedObject this[int index] { get { return _list[index]; } }
        public NamedObject this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameDict();
                NamedObject ret;
                if (!_nameDict.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }

        internal void ItemIdentifierChanged(object sender, EventArgs e)
        {
            _nameDict = null;
        }

        public void ReleaseAll()
        {
            foreach (NamedObject o in _list)
            {
                o.Release();
            }
        }
        public void Sort()
        {
            _list.Sort();
        }

        #region ICollection<T>の実装
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(NamedObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            _list.Add(item);
            item.IdentifierChanged += ItemIdentifierChanged;
            _nameDict = null;
        }

        public void Clear()
        {
            _list.Clear();
            _nameDict = null;
        }

        public bool Contains(NamedObject item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(NamedObject[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<NamedObject> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Remove(NamedObject item)
        {
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameDict = null;
            }
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }

    public class NamedCollection<T> : NamedCollection, ICollection<T> where T : NamedObject
    {
        internal class EnumeratorWrapper : IEnumerator<T>
        {
            private IEnumerator _base;
            internal EnumeratorWrapper(IEnumerator enumerator)
            {
                _base = enumerator;
            }

            public T Current
            {
                get
                {
                    return (T)_base.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return _base.Current;
                }
            }

            public void Dispose()
            {
                ((IDisposable)_base).Dispose();
            }

            public bool MoveNext()
            {
                return _base.MoveNext();
            }

            public void Reset()
            {
                _base.Reset();
            }
        }
        public new T this[int index] { get { return (T)base[index]; } }
        public new T this[string name] { get { return (T)base[name]; } }

        #region ICollection<T>の実装
        //public int Count {
        //    get {
        //        return _list.Count;
        //    }
        //}

        //public bool IsReadOnly {
        //    get {
        //        return false;
        //    }
        //}

        public void Add(T item)
        {
            base.Add(item);
        }

        //public void Clear() {
        //    _list.Clear();
        //    _nameDict = null;
        //}

        public bool Contains(T item)
        {
            return base.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return new EnumeratorWrapper(_list.GetEnumerator());
        }

        public bool Remove(T item)
        {
            return base.Remove(item);
        }

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return _list.GetEnumerator();
        //}
        #endregion
    }
    public class CommentChangedEventArgs : EventArgs
    {
        public Comment Comment { get; private set; }
        internal CommentChangedEventArgs(Comment comment)
        {
            Comment = comment;
        }
    }
    public class PropertyChangedEventArgs : EventArgs
    {
        public string Property { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; set; }
        internal PropertyChangedEventArgs(string property, object newValue, object oldValue)
        {
            Property = property;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
    public enum CollectionOperation
    {
        Add,
        Remove,
        Update,
        AddRange,
        Clear
    }
    public class CollectionOperationEventArgs<T> : EventArgs
    {
        public string Property { get; private set; }
        public CollectionOperation Operation { get; private set; }
        /// <summary>
        /// Insert, RemoveAt など追加した位置が判明しているときに
        /// 通常は-1
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Operation = Remove, Update の時に設定する
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Operation = Add, Update の時に設定する
        /// </summary>
        public T NewValue { get; set; }
        internal CollectionOperationEventArgs(string property, CollectionOperation operation, int index, T newValue, T oldValue)
        {
            Property = property;
            Operation = operation;
            Index = index;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
    public class ColumnPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public Column Column { get; private set; }
        internal ColumnPropertyChangedEventArgs(object sender, PropertyChangedEventArgs e) : base(e.Property, e.NewValue, e.OldValue)
        {
            Column = sender as Column;
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

    public class Dependency
    {
        public SchemaObject From { get; private set; }
        public SchemaObject To { get; private set; }
        public Dependency(SchemaObject from, SchemaObject to)
        {
            From = from;
            To = to;
        }
    }

    //public sealed class DependencyCollection : IList<Dependency>
    //{
    //    private List<Dependency> _dependencies = new List<Dependency>();
    //    private Dictionary<SchemaObject, IList<SchemaObject>> _dependFrom = null;
    //    private Dictionary<SchemaObject, IList<SchemaObject>> _dependTo = null;
    //    private void InvalidateDependencies()
    //    {
    //        _dependFrom = null;
    //        _dependTo = null;
    //    }
    //    private void UpdateDependencies()
    //    {
    //        if (_dependFrom != null && _dependTo != null)
    //        {
    //            return;
    //        }
    //        Dictionary<SchemaObject, IList<SchemaObject>> from = new Dictionary<SchemaObject, IList<SchemaObject>>();
    //        Dictionary<SchemaObject, IList<SchemaObject>> to = new Dictionary<SchemaObject, IList<SchemaObject>>();
    //        IList<SchemaObject> l;
    //        foreach (Dependency d in _dependencies)
    //        {
    //            l = null;
    //            if (!from.TryGetValue(d.From, out l))
    //            {
    //                l = new List<SchemaObject>();
    //                from.Add(d.From, l);
    //            }
    //            l.Add(d.To);
    //            l = null;
    //            if (!to.TryGetValue(d.To, out l))
    //            {
    //                l = new List<SchemaObject>();
    //                to.Add(d.To, l);
    //            }
    //            l.Add(d.From);
    //        }
    //        foreach (SchemaObject o in from.Keys)
    //        {
    //            l = from[o];
    //            ((List<SchemaObject>)l).Sort();
    //            from[o] = l.ToArray();
    //        }
    //        foreach (SchemaObject o in to.Keys)
    //        {
    //            l = to[o];
    //            ((List<SchemaObject>)l).Sort();
    //            to[o] = l.ToArray();
    //        }
    //        _dependFrom = from;
    //        _dependTo = to;
    //    }
    //    private static SchemaObject[] EmptySchemaObjectArray = new SchemaObject[0];

    //    public SchemaObject[] GetDepended(SchemaObject target)
    //    {
    //        UpdateDependencies();
    //        IList<SchemaObject> l;
    //        if (_dependFrom.TryGetValue(target, out l))
    //        {
    //            return l as SchemaObject[];
    //        }
    //        return EmptySchemaObjectArray;
    //    }
    //    public SchemaObject[] GetDependOn(SchemaObject target)
    //    {
    //        UpdateDependencies();
    //        IList<SchemaObject> l;
    //        if (_dependTo.TryGetValue(target, out l))
    //        {
    //            return l as SchemaObject[];
    //        }
    //        return EmptySchemaObjectArray;
    //    }

    //    public DependencyCollection() { }
    //    public DependencyCollection(IEnumerable<Dependency> dependencies)
    //    {
    //        _dependencies = new List<Dependency>(dependencies);
    //    }

    //    #region IList<SchemaObject>の実装
    //    public int Count { get { return (_dependencies).Count; } }

    //    public bool IsReadOnly { get { return false; } }

    //    public Dependency this[int index]
    //    {
    //        get { return _dependencies[index]; }
    //        set
    //        {
    //            if (_dependencies[index] == value)
    //            {
    //                return;
    //            }
    //            _dependencies[index] = value;
    //            InvalidateDependencies();
    //        }
    //    }
    //    public int IndexOf(Dependency item)
    //    {
    //        return _dependencies.IndexOf(item);
    //    }

    //    public void Insert(int index, Dependency item)
    //    {
    //        _dependencies.Insert(index, item);
    //        InvalidateDependencies();
    //    }

    //    public void RemoveAt(int index)
    //    {
    //        _dependencies.RemoveAt(index);
    //        InvalidateDependencies();
    //    }

    //    public void Add(Dependency item)
    //    {
    //        _dependencies.Add(item);
    //        InvalidateDependencies();
    //    }

    //    public void Clear()
    //    {
    //        _dependencies.Clear();
    //        InvalidateDependencies();
    //    }

    //    public bool Contains(Dependency item)
    //    {
    //        return _dependencies.Contains(item);
    //    }

    //    public void CopyTo(Dependency[] array, int arrayIndex)
    //    {
    //        _dependencies.CopyTo(array, arrayIndex);
    //    }

    //    public bool Remove(Dependency item)
    //    {
    //        bool flag = _dependencies.Remove(item);
    //        if (flag)
    //        {
    //            InvalidateDependencies();
    //        }
    //        return flag;
    //    }

    //    public IEnumerator<Dependency> GetEnumerator()
    //    {
    //        return _dependencies.GetEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return _dependencies.GetEnumerator();
    //    }
    //    #endregion
    //}

    public class Schema: NamedObject, IComparable
    {
        public enum CollectionIndex
        {
            Objects = 0,
            Constraints = 1,
            Columns = 2,
            Comments = 3,
            Indexes = 4,
            Triggers = 5,
            //Procedures = 6,
        }
        public Db2SourceContext Owner { get; private set; }
        public string Name { get { return Identifier; } }
        public bool IsHidden
        {
            get
            {
                return Owner.IsHiddenSchema(Name);
            }
        }
        private NamedCollection[] _collections;
        public NamedCollection GetCollection(CollectionIndex index)
        {
            return _collections[(int)index];
        }
        public NamedCollection<SchemaObject> Objects { get; } = new NamedCollection<SchemaObject>();
        public NamedCollection<Column> Columns { get; } = new NamedCollection<Column>();
        public NamedCollection<Comment> Comments { get; } = new NamedCollection<Comment>();
        public NamedCollection<Constraint> Constraints { get; } = new NamedCollection<Constraint>();
        public NamedCollection<Index> Indexes { get; } = new NamedCollection<Index>();
        public NamedCollection<Trigger> Triggers { get; } = new NamedCollection<Trigger>();
        //public NamedCollection<StoredFunction> Procedures { get; } = new NamedCollection<StoredFunction>();
        internal Schema(Db2SourceContext owner, string name) : base(owner.Schemas, name)
        {
            Owner = owner;
            _collections = new NamedCollection[] { Objects, Constraints, Columns, Comments, Indexes, Triggers /*, Procedures */ };
        }

        public void InvalidateColumns()
        {
            foreach (SchemaObject o in Objects)
            {
                o.InvalidateColumns();
            }
        }

        public void InvalidateConstraints()
        {
            foreach (SchemaObject o in Objects)
            {
                (o as Table)?.InvalidateConstraints();
            }
        }

        public void InvalidateTriggers()
        {
            foreach (SchemaObject o in Objects)
            {
                o.InvalidateTriggers();
            }
        }

        public void InvalidateIndexes()
        {
            foreach (SchemaObject o in Objects)
            {
                (o as Table)?.InvalidateIndexes();
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Schema))
            {
                return false;
            }
            Schema sc = (Schema)obj;
            return ((Owner != null) ? Owner.Equals(sc.Owner) : (Owner == sc.Owner)) && (Name == sc.Name);
        }

        public override int GetHashCode()
        {
            return ((Owner != null) ? Owner.GetHashCode() : 0) + (string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode());
        }

        public override string ToString()
        {
            return Name;
        }

        public override int CompareTo(object obj)
        {
            if (!(obj is Schema))
            {
                return -1;
            }
            Schema sc = (Schema)obj;
            int ret = string.Compare(Name, sc.Name);
            if (ret != 0)
            {
                return ret;
            }
            if (Owner == null)
            {
                ret = (sc.Owner == null) ? 0 : 1;
            }
            else
            {
                ret = Owner.CompareTo(sc.Owner);
            }
            return ret;
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
    public abstract partial class Db2SourceContext: IComparable
    {
        public static bool IsSQLLoggingEnabled = false;
        private static string _logPath = null;
        private static object _logLock = new object();
        private static void RequireLogPath()
        {
            if (_logPath != null)
            {
                return;
            }
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Db2Src.net");
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
        /// <summary>
        /// SQL出力時の一行あたりの推奨文字数
        /// </summary>
        public int PreferedCharsPerLine { get; set; } = 80;
        public IDbConnection Connection()
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
            using (IDbConnection conn = Connection())
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
            using (IDbConnection conn = Connection())
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
            using (IDbConnection conn = Connection())
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

        public abstract IDbCommand GetSqlCommand(string sqlCommand, IDbConnection connection);
        public abstract IDbCommand GetSqlCommand(string sqlCommand, IDbConnection connection, IDbTransaction transaction);
        public abstract IDbCommand GetSqlCommand(StoredFunction function, IDbConnection connection, IDbTransaction transaction);

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
            using (IDbConnection conn = Connection())
            {
                foreach (string s in sqls)
                {
                    using (IDbCommand cmd = GetSqlCommand(s, conn))
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

    public enum ConstraintType
    {
        Primary,
        Unique,
        ForeignKey,
        Check
    }

    public interface IDb2SourceInfo
    {
        Db2SourceContext Context { get; }
        Schema Schema { get; }
    }
    public interface IConstraint: IDb2SourceInfo
    {
        string Name { get; }
        ConstraintType ConstraintType { get; }
        string TableName { get; set; }
        Table Table { get; set; }
    }

    public interface ICommentable: IDb2SourceInfo
    {
        string GetSqlType();
        Comment Comment { get; set; }
        void OnCommentChanged(CommentChangedEventArgs e);
    }

    public class Comment: NamedObject, IDb2SourceInfo
    {
        public Db2SourceContext Context { get; private set; }
        public Schema Schema { get; private set; }
        public string SchemaName
        {
            get
            {
                return Schema?.Name;
            }
        }
        public virtual string GetCommentType()
        {
            return GetTarget()?.GetSqlType();
        }
        /// <summary>
        /// コメントが編集されていたらtrueを返す
        /// 変更を重ねて元と同じになった場合はfalseを返す
        /// </summary>
        /// <returns></returns>
        public override bool IsModified() { return _text != _oldText; }
        public string Target { get; set; }
        private string _text;
        private string _oldText;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }
        public virtual ICommentable GetTarget()
        {
            SchemaObject o = Schema.Objects[Target];
            return o;
        }
        public void Link()
        {
            ICommentable o = GetTarget();
            if (o != null)
            {
                o.Comment = this;
            }
        }

        protected void OnTextChanged(EventArgs e)
        {
            GetTarget()?.OnCommentChanged(new CommentChangedEventArgs(this));
        }
        public virtual string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Target, baseSchemaName);
        }
        internal Comment(Db2SourceContext context, string schema) : base(context.RequireSchema(schema).Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal Comment(Db2SourceContext context, string schema, string target, string comment, bool isLoaded) : base(context.RequireSchema(schema).Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
            Target = target;
            Text = comment;
            if (isLoaded)
            {
                _oldText = Text;
            }
        }
        public override void Release()
        {
            if (Schema != null)
            {
                Schema.Comments.Remove(this);
            }
        }
    }

    public class ColumnComment: Comment
    {
        public override string GetCommentType()
        {
            return "COLUMN";
        }
        private string _column;
        public string Column
        {
            get
            {
                return _column;
            }
            set
            {
                _column = value;
                Identifier = Target + "." + _column;
            }
        }
        public override ICommentable GetTarget()
        {
            Selectable o = Schema?.Objects[Target] as Selectable;
            if (o == null)
            {
                return null;
            }
            Column c = o.Columns[Column];
            return c;
        }
        public override string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, new string[] { Target, Column }, baseSchemaName);
        }
        internal ColumnComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal ColumnComment(Db2SourceContext context, string schema, string table, string column, string comment, bool isLoaded) : base(context, schema, table, comment, isLoaded)
        {
            Column = column;
        }
    }

    [Serializable]
    public class ColumnNameException: ArgumentException
    {
        public ColumnNameException(string name) : base(string.Format("列名\"{0}\"が重複します", name)) { }
    }

    public class Constraint: SchemaObject, IComparable, IConstraint
    {
        internal Constraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isNoName) : base(context, owner, schema, name, Schema.CollectionIndex.Constraints)
        {
            _tableSchema = tableSchema;
            _tableName = tableName;
            _table = null;
            _isTemporaryName = isNoName;
        }

        public override string GetSqlType()
        {
            return "CONSTRAINT";
        }
        public override string GetExportFolderName()
        {
            return "Constraint";
        }
        public virtual ConstraintType ConstraintType { get; }

        //public override string Name
        //{
        //    get
        //    {
        //        return base.Name;
        //    }
        //    set
        //    {
        //        base.Name = value;
        //        InvalidateIsTemporaryName();
        //    }
        //}
        protected override void NameChanged(string oldValue)
        {
            base.NameChanged(oldValue);
            InvalidateIsTemporaryName();
        }

        public override Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Constraints;
        }

        private bool? _isTemporaryName;
        private Table _table;
        private string _tableSchema;
        private string _tableName;

        private void InvalidateIsTemporaryName()
        {
            //_isTemporaryName = null;
        }
        //private void UpdateIsTemporaryName()
        //{
        //    if (_isTemporaryName.HasValue)
        //    {
        //        return;
        //    }
        //    _isTemporaryName = Table?.TemporaryNamePattern.IsMatch(Name);
        //}
        public bool IsTemporaryName
        {
            get
            {
                //UpdateIsTemporaryName();
                return _isTemporaryName.Value;
            }
        }
        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context.Tables[TableSchema, TableName];

            InvalidateIsTemporaryName();
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public Table Table
        {
            get
            {
                UpdateTable();
                return _table;
            }
            set
            {
                if (_table != value)
                {
                    return;
                }
                _table = value;
                if (_table == null)
                {
                    return;
                }
                _tableSchema = _table.SchemaName;
                _tableName = _table.Name;
                InvalidateIsTemporaryName();
            }
        }

        public string TableSchema
        {
            get
            {
                return _tableSchema;
            }
            set
            {
                if (_tableSchema == value)
                {
                    return;
                }
                _tableSchema = value;
                InvalidateTable();
            }
        }
        public string TableName
        {
            get
            {
                return _tableName;
            }
            set
            {
                if (_tableName == value)
                {
                    return;
                }
                _tableName = value;
                InvalidateTable();
            }
        }
        public override int CompareTo(object obj)
        {
            if (!(obj is Constraint))
            {
                return base.CompareTo(obj);
            }
            Constraint c = (Constraint)obj;
            int ret = (int)ConstraintType - (int)c.ConstraintType;
            if (ret != 0)
            {
                return ret;
            }
            return base.CompareTo(obj);
        }
    }

    public abstract class ColumnsConstraint: Constraint
    {
        public string[] Columns { get; set; }
        internal ColumnsConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isNoName)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName) { }
    }

    public partial class KeyConstraint: ColumnsConstraint
    {
        private bool _isPrimary = false;
        public override ConstraintType ConstraintType { get { return _isPrimary ? ConstraintType.Primary : ConstraintType.Unique; } }
        public string[] ExtraInfo { get; set; }

        public KeyConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isPrimary, bool isNoName)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName)
        {
            _isPrimary = isPrimary;
        }
    }

    public enum ForeignKeyRule
    {
        NoAction,
        Restrict,
        Cascade,
        SetNull,
        SetDefault
    }
    public partial class ForeignKeyConstraint: ColumnsConstraint
    {
        public override ConstraintType ConstraintType { get { return ConstraintType.ForeignKey; } }
        public ForeignKeyRule UpdateRule { get; set; } = ForeignKeyRule.NoAction;
        public ForeignKeyRule DeleteRule { get; set; } = ForeignKeyRule.NoAction;
        private KeyConstraint _refConstraint = null;
        private void InvalidateReferenceConstraint()
        {
            _refConstraint = null;
        }
        private void UpdateReferenceConstraint()
        {
            if (_refConstraint != null)
            {
                return;
            }
            _refConstraint = Context.Constraints[ReferenceSchemaName, ReferenceConstraintName] as KeyConstraint;
        }
        public KeyConstraint ReferenceConstraint
        {
            get
            {
                UpdateReferenceConstraint();
                return _refConstraint;
            }
        }
        private string _refSchema;
        private string _refConsName;
        private string _refTableName;
        public string ReferenceSchemaName
        {
            get
            {
                return _refSchema;
            }
            set
            {
                if (_refSchema == value)
                {
                    return;
                }
                _refSchema = value;
                InvalidateReferenceConstraint();
                RefColumns = ReferenceConstraint?.Columns;
            }
        }
        public string ReferenceConstraintName
        {
            get
            {
                return _refConsName;
            }
            set
            {
                if (_refConsName == value)
                {
                    return;
                }
                _refConsName = value;
                if (!string.IsNullOrEmpty(_refConsName))
                {
                    _refTableName = null;
                }
                InvalidateReferenceConstraint();
            }
        }
        public string ReferenceTableName
        {
            get
            {
                if (_refTableName == null)
                {
                    return ReferenceConstraint.TableName;
                }
                return _refTableName;
            }
            set
            {
                if (_refTableName == value)
                {
                    return;
                }
                _refTableName = value;
            }
        }
        public string[] RefColumns { get; set; }
        //{
        //    get
        //    {
        //        return ReferenceConstraint.Columns;
        //    }
        //}
        public string[] ExtraInfo { get; set; }

        public ForeignKeyConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, string refSchema, string refConstraint, ForeignKeyRule updateRule, ForeignKeyRule deleteRule, bool isNoName)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName)
        {
            ReferenceSchemaName = refSchema;
            ReferenceConstraintName = refConstraint;
            UpdateRule = updateRule;
            DeleteRule = deleteRule;
        }
    }

    public partial class CheckConstraint: Constraint
    {
        //public Db2SourceContext Context { get; private set; }
        //public Schema Schema { get; private set; }
        //public string Name { get; set; }
        public override ConstraintType ConstraintType { get { return ConstraintType.Check; } }
        //public string TableName { get; set; }
        //public Table Table { get; set; }
        public string Condition { get; set; }
        public string[] ExtraInfo { get; set; }

        public CheckConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, string condition, bool isNoName)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName)
        {
            Condition = condition;
        }
    }

    public interface IDbTypeDef
    {
        string BaseType { get; set; }
        int? DataLength { get; set; }
        int? Precision { get; set; }
        bool? WithTimeZone { get; set; }
        bool IsSupportedType { get; set; }
        Type ValueType { get; set; }
    }
    public static class DbTypeDefUtil
    {
        public static string ToTypeText(IDbTypeDef def)
        {
            if (!def.DataLength.HasValue && !def.Precision.HasValue)
            {
                return def.BaseType;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(def.BaseType);
            buf.Append('(');

            buf.Append(def.DataLength.HasValue ? def.DataLength.Value.ToString() : "*");
            if (def.Precision.HasValue)
            {
                buf.Append(',');
                buf.Append(def.Precision.Value);
            }
            buf.Append(')');
            if (def.WithTimeZone.HasValue && def.WithTimeZone.Value)
            {
                buf.Append(" with time zone");
            }
            return buf.ToString();
        }
    }

    public partial class Column: NamedObject, ICommentable, IComparable, IDbTypeDef
    {
        [Flags]
        public enum ColumnGeneration
        {
            New = 1,
            Old = 2
        };

        public string GetSqlType()
        {
            return "COLUMN";
        }
        public ColumnGeneration Generation { get; private set; }
        private Column _oldColumn = null;
        public Db2SourceContext Context { get; private set; }
        public Schema Schema { get; private set; }
        public string SchemaName
        {
            get
            {
                return Schema?.Name;
            }
        }
        private string _tableName;
        private Selectable _owner;
        private string _name;
        private void UpdateIdentifier()
        {
            Identifier = TableName + "." + Name;
        }
        public int Index { get; set; }
        public string TableName
        {
            get { return _tableName; }
            set
            {
                if (_tableName == value)
                {
                    return;
                }
                _tableName = value;
                _owner = null;
                UpdateIdentifier();
            }
        }

        public string EscapedName
        {
            get
            {
                return Context.GetEscapedIdentifier(Name);
            }
        }
        public string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, new string[] { TableName, Name }, baseSchemaName);
        }

        private void UpdateSelectable()
        {
            if (_owner != null)
            {
                return;
            }

            _owner = Context.Selectables[SchemaName, _tableName];
        }
        public override bool IsModified()
        {
            return (Comment != null) ? Comment.IsModified() : false;
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            Table.OnColumnPropertyChanged(new ColumnPropertyChangedEventArgs(this, e));
        }

        void ICommentable.OnCommentChanged(CommentChangedEventArgs e)
        {
            Table.OnCommentChanged(e);
        }

        protected void OnCommentChanged(CommentChangedEventArgs e)
        {
            Table.OnCommentChanged(e);
        }

        public override int CompareTo(object obj)
        {
            if (obj == null)
            {
                return (this == null) ? 0 : -1;
            }
            if (!(obj is Column))
            {
                throw new ArgumentException();
            }
            Column c = (Column)obj;
            int ret = string.Compare(TableName, c.TableName);
            if (ret != 0)
            {
                return ret;
            }
            ret = Index.CompareTo(c.Index);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(Name, c.Name);
            return ret;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {

            }
            if (!(obj is Column))
            {
                return false;
            }
            Column c = (Column)obj;
            return (TableName == c.TableName) && (Index == c.Index) && (Name == c.Name);
        }
        public override int GetHashCode()
        {
            return TableName.GetHashCode() + Index.GetHashCode() + Name.GetHashCode();
        }
        public override string ToString()
        {
            return Identifier + Name;
        }
        public Column BecomeModifiedColumn()
        {
            if (Context.IsChangeLogDisabled())
            {
                return null;
            }
            if ((Generation & ColumnGeneration.New) == 0)
            {
                return this;
            }
            if (_oldColumn != null)
            {
                return _oldColumn;
            }
            Generation = ColumnGeneration.New;
            Column ret = (Column)MemberwiseClone();
            ret.Generation = ColumnGeneration.Old;
            _oldColumn = ret;
            Schema sc = ret.Schema;
            if (sc == null)
            {
                return ret;
            }
            sc.Columns.Add(ret);
            sc.InvalidateColumns();
            return ret;
        }

        public Selectable Table
        {
            get
            {
                UpdateSelectable();
                return _owner;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name == value)
                {
                    return;
                }
                if (_owner != null && !_owner.Columns.ColumnNameChanging(this, value))
                {
                    throw new ColumnNameException(value);
                }
                _name = value;
                _owner?.Columns?.ColumnNameChanged(this);
            }
        }
        public string BaseType { get; set; }
        public int? DataLength { get; set; }
        public int? Precision { get; set; }
        public bool? WithTimeZone { get; set; }
        public bool IsSupportedType { get; set; }
        public string StringFormat
        {
            get
            {
                Dictionary<string, PropertyInfo> dict = Context.BaseTypeToProperty;
                if (dict == null)
                {
                    return null;
                }
                PropertyInfo prop;
                if (!dict.TryGetValue(BaseType, out prop))
                {
                    return null;
                }
                if (prop == null)
                {
                    return null;
                }
                return (string)prop.GetValue(null);
            }
        }
        public void UpdateDataType()
        {
            _dataType = DbTypeDefUtil.ToTypeText(this);
        }
        private string _dataType;
        public string DataType
        {
            get
            {
                return _dataType;
            }
            set
            {
                if (_dataType == value)
                {
                    return;
                }
                BecomeModifiedColumn();
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("DataType", value, _dataType);
                _dataType = value;
                OnPropertyChanged(e);
            }
        }

        public Type ValueType { get; set; }

        private string _defaultValue;
        public string DefaultValue
        {
            get
            {
                return _defaultValue;
            }
            set
            {
                if (_defaultValue == value)
                {
                    return;
                }
                BecomeModifiedColumn();
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("DefaultValue", value, _defaultValue);
                _defaultValue = value;
                OnPropertyChanged(e);
            }
        }
        private bool _notNull;
        public bool NotNull
        {
            get
            {
                return _notNull;
            }
            set
            {
                if (_notNull == value)
                {
                    return;
                }
                BecomeModifiedColumn();
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("DefaultValue", value, _notNull);
                _notNull = value;
                OnPropertyChanged(e);
            }
        }
        private Comment _comment;
        public Comment Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (_comment == value)
                {
                    return;
                }
                CommentChangedEventArgs e = new CommentChangedEventArgs(value);
                _comment = value;
                OnCommentChanged(e);
            }
        }

        public string CommentText
        {
            get
            {
                return Comment?.Text;
            }
            set
            {
                if (Comment == null)
                {
                    Comment = new ColumnComment(Context, SchemaName, Table?.Name, Name, value, false);
                    Comment.Link();
                }
                Comment.Text = value;
            }
        }

        public bool IsHidden { get; set; }

        internal Column(Db2SourceContext context, string schema) : base(context.RequireSchema(schema).Columns)
        {
            Generation = (ColumnGeneration.New | ColumnGeneration.Old);
            Context = context;
            Schema = context.RequireSchema(schema);
        }
        public override void Release()
        {
            if (Schema != null)
            {
                Schema.Columns.Remove(this);
            }
            Comment?.Release();
        }
    }
    public class DbTypeInfo
    {
        public DbType DbType { get; private set; }
        public string Name { get; private set; }
        private delegate object ParseDbType(string value);
        private delegate string DbTypeToString(object value);
        private ParseDbType _parse;
        private DbTypeToString _valueToString;
        private static object ParseString(string value)
        {
            return value;
        }
        private static object ParseBool(string value)
        {
            return bool.Parse(value);
        }
        private static object ParseSByte(string value)
        {
            return sbyte.Parse(value);
        }
        private static object ParseInt16(string value)
        {
            return short.Parse(value);
        }
        private static object ParseInt32(string value)
        {
            return int.Parse(value);
        }
        private static object ParseInt64(string value)
        {
            return long.Parse(value);
        }
        private static object ParseByte(string value)
        {
            return byte.Parse(value);
        }
        private static object ParseUInt16(string value)
        {
            return ushort.Parse(value);
        }
        private static object ParseUInt32(string value)
        {
            return uint.Parse(value);
        }
        private static object ParseUInt64(string value)
        {
            return ulong.Parse(value);
        }
        private static object ParseSingle(string value)
        {
            return float.Parse(value);
        }
        private static object ParseDouble(string value)
        {
            return double.Parse(value);
        }
        private static object ParseDecimal(string value)
        {
            return decimal.Parse(value);
        }
        private static object ParseDate(string value)
        {
            return DateTime.ParseExact(value, Db2SourceContext.DateFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static object ParseDateTime(string value)
        {
            return DateTime.ParseExact(value, Db2SourceContext.DateTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static string ObjectToString(object value)
        {
            return value?.ToString();
        }
        private static string DateToString(object value)
        {
            if (!(value is DateTime))
            {
                return null;
            }
            return ((DateTime)value).ToString(Db2SourceContext.DateFormat);
        }
        private static string DateTimeToString(object value)
        {
            if (!(value is DateTime))
            {
                return null;
            }
            return ((DateTime)value).ToString(Db2SourceContext.DateTimeFormat);
        }
        public object Parse(string value)
        {
            return _parse?.Invoke(value);
        }
        public string ValueToString(object value)
        {
            return _valueToString?.Invoke(value);
        }
        public static readonly DbTypeInfo[] DbTypeInfos = new DbTypeInfo[]
        {
            //new DbTypeInfo() {DbType = DbType.AnsiString, Name="AnsiString", _parse = ParseString, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Binary, Name="Binary", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Byte, Name="Byte", _parse = ParseByte, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Boolean, Name="Boolean", _parse = ParseBool, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Currency, Name="Currency", _parse = ParseInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Date, Name="日付", _parse = ParseDate, _valueToString = DateToString },
            new DbTypeInfo() {DbType = DbType.DateTime, Name="日付時刻", _parse = ParseDateTime, _valueToString = DateTimeToString },
            new DbTypeInfo() {DbType = DbType.Decimal, Name="実数(Decimal)", _parse = ParseDecimal, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Double, Name="実数(Double)", _parse = ParseDouble, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Guid, Name="Guid", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Int16, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Int32, Name="整数(32bit)", _parse = ParseInt32, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Int64, Name="整数(64bit)", _parse = ParseInt64, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Object, Name="Object", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.SByte, Name="Sbyte", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Single, Name="Single", _parse = ParseSingle, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.String, Name="文字列", _parse = ParseString, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Time, Name="Time", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.UInt16, Name="UInt16", _parse = ParseUInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.UInt32, Name="符号なし整数(32bit)", _parse = ParseUInt32, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.UInt64, Name="符号なし整数(64bit)", _parse = ParseUInt64, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.VarNumeric, Name="VarNumeric", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.AnsiStringFixedLength, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.StringFixedLength, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Xml, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.DateTime2, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.DateTimeOffset, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
        };
        private static Dictionary<DbType, DbTypeInfo> InitDbTypeToInto()
        {
            Dictionary<DbType, DbTypeInfo> ret = new Dictionary<DbType, DbTypeInfo>();
            foreach (DbTypeInfo info in DbTypeInfos)
            {
                ret.Add(info.DbType, info);
            }
            return ret;
        }
        public static readonly Dictionary<DbType, DbTypeInfo> DbTypeToDbTypeInfo = InitDbTypeToInto();
    }
    public partial class Parameter: /*NamedObject,*/ IComparable, IDbTypeDef
    {
        public StoredFunction Owner { get; private set; }
        public int Index { get; internal set; }
        public string Name { get; internal set; }
        public ParameterDirection Direction { get; set; }
        private static readonly string[] ParameterDirectionStr = { null, null, "out", "inout", null, null, "result" };
        public static string GetParameterDirectionStr(ParameterDirection direction)
        {
            int i = (int)direction;
            if (i < 0 || ParameterDirectionStr.Length <= i)
            {
                return null;
            }
            return ParameterDirectionStr[i];
        }
        public virtual string DirectionStr
        {
            get
            {
                return GetParameterDirectionStr(Direction);
            }
        }
        public string BaseType { get; set; }
        public int? DataLength { get; set; } = null;
        public int? Precision { get; set; } = null;
        public bool? WithTimeZone { get; set; }
        public bool IsSupportedType { get; set; }

        public void UpdateDataType()
        {
            _dataType = DbTypeDefUtil.ToTypeText(this);
        }
        private string _dataType;
        public string DataType
        {
            get
            {
                return _dataType;
            }
            set
            {
                if (_dataType == value)
                {
                    return;
                }
                //PropertyChangedEventArgs e = new PropertyChangedEventArgs("DataType", value, _dataType);
                _dataType = value;
                //OnPropertyChanged(e);
            }
        }

        public Type ValueType { get; set; }
        public string DefaultValue { get; set; }
        public string StringFormat
        {
            get
            {
                Dictionary<string, PropertyInfo> dict = Owner?.Context.BaseTypeToProperty;
                if (dict == null)
                {
                    return null;
                }
                PropertyInfo prop;
                if (!dict.TryGetValue(BaseType, out prop))
                {
                    return null;
                }
                if (prop == null)
                {
                    return null;
                }
                return (string)prop.GetValue(null);
            }
        }

        internal IDbDataParameter _dbParameter;
        public IDbDataParameter DbParameter
        {
            get
            {
                Owner.RequireDbCommand();
                return _dbParameter;
            }
            internal set
            {
                _dbParameter = value;
            }
        }
        public Parameter(StoredFunction owner) : base()
        {
            Owner = owner;
            owner.Parameters.Add(this);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Parameter))
            {
                return -1;
            }
            Parameter p = (Parameter)obj;
            int ret = Owner.CompareTo(p.Owner);
            if (ret != 0)
            {
                return ret;
            }
            ret = Index - p.Index;
            if (ret != 0)
            {
                return ret;
            }
            //ret = string.Compare(Identifier, p.Identifier);
            ret = string.Compare(Name, p.Name);
            return ret;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Parameter))
            {
                return false;
            }
            Parameter p = (Parameter)obj;
            //return ((Owner == null && p.Owner == null) || (Owner != null && Owner.Equals(p.Owner))) && (Identifier == p.Identifier);
            return ((Owner == null && p.Owner == null) || (Owner != null && Owner.Equals(p.Owner))) && (Name == p.Name);
        }
        public override int GetHashCode()
        {
            //return ((Owner != null) ? Owner.GetHashCode() : 0) + Identifier.GetHashCode();
            return ((Owner != null) ? Owner.GetHashCode() : 0) + Name.GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class ColumnCollection: IList<Column>, IList
    {
        private Selectable _owner;
        private List<Column>[] _list = null;
        private Column[] _hiddenColumns = null;
        private Column[] _allColumns = null;
        private Dictionary<string, Column>[] _nameToColumn = null;

        public ColumnCollection(Selectable owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            _list = null;
            _hiddenColumns = null;
            _allColumns = null;
            _nameToColumn = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<Column>[] { new List<Column>(), new List<Column>() };
            List<Column> hidden = new List<Column>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }
            foreach (Column c in _owner.Schema.Columns)
            {
                if (c.Table == null)
                {
                    continue;
                }
                if (!c.Table.Equals(_owner))
                {
                    continue;
                }
                if (c.IsHidden)
                {
                    hidden.Add(c);
                    continue;
                }
                if ((c.Generation & Column.ColumnGeneration.New) != 0)
                {
                    _list[0].Add(c);
                }
                if ((c.Generation & Column.ColumnGeneration.Old) != 0)
                {
                    _list[1].Add(c);
                }
            }
            _list[0].Sort();
            _list[1].Sort();
            hidden.Sort();
            _hiddenColumns = hidden.ToArray();
        }
        private void RequireAllColumns()
        {
            if (_allColumns != null)
            {
                return;
            }
            RequireItems();
            List<Column> l = new List<Column>();
            l.AddRange(_hiddenColumns);
            l.AddRange(_list[0]);
            l.Sort();
            _allColumns = l.ToArray();
        }
        private void RequireNameToColumn()
        {
            if (_nameToColumn != null)
            {
                return;
            }
            _nameToColumn = new Dictionary<string, Column>[] { new Dictionary<string, Column>(), new Dictionary<string, Column>() };
            RequireItems();
            foreach (Column c in _list[0])
            {
                _nameToColumn[0].Add(c.Name, c);
            }
            foreach (Column c in _list[1])
            {
                _nameToColumn[1].Add(c.Name, c);
            }
        }
        //public Column this[int index] {
        //    get {
        //        RequireItems();
        //        return _list[0][index];
        //    }
        //}
        //public Column this[string name] {
        //    get {
        //        if (string.IsNullOrEmpty(name)) {
        //            return null;
        //        }
        //        RequireNameToColumn();
        //        Column ret;
        //        if (!_nameToColumn[0].TryGetValue(name, out ret)) {
        //            return null;
        //        }
        //        return ret;
        //    }
        //}
        public Column[] HiddenColumns
        {
            get
            {
                RequireItems();
                return _hiddenColumns;
            }
        }
        public Column[] AllColumns
        {
            get
            {
                RequireAllColumns();
                return _allColumns;
            }
        }
        public Column this[int index]
        {
            get
            {
                RequireItems();
                return _list[0][index];
            }
        }
        public Column this[int index, bool isOld]
        {
            get
            {
                RequireItems();
                return _list[isOld ? 1 : 0][index];
            }
        }
        public Column this[string name]
        {
            get
            {
                return this[name, false];
            }
        }
        public Column this[string name, bool isOld]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToColumn();
                Column ret;
                if (!_nameToColumn[isOld ? 1 : 0].TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool ColumnNameChanging(Column column, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (column != null && column.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void ColumnNameChanged(Column column)
        {
            _nameToColumn = null;
        }
        #region ICollection<Column>の実装
        public int Count
        {
            get
            {
                RequireItems();
                return _list[0].Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list[0]).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list[0]).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                return ((IList)_list[0])[index];
            }

            set
            {
                RequireItems();
                ((IList)_list[0])[index] = value;
            }
        }

        Column IList<Column>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[0][index];
            }

            set
            {
                RequireItems();
                _list[0][index] = value;
            }
        }

        public void Add(Column item)
        {
            if (!ColumnNameChanging(null, item.Name))
            {
                throw new ColumnNameException(item.Name);
            }
            RequireItems();
            if ((item.Generation & Column.ColumnGeneration.New) != 0)
            {
                _list[0].Add(item);
            }
            if ((item.Generation & Column.ColumnGeneration.Old) != 0)
            {
                _list[1].Add(item);
            }
            _nameToColumn = null;
        }
        int IList.Add(object value)
        {
            Column item = value as Column;
            if (!ColumnNameChanging(null, item.Name))
            {
                throw new ColumnNameException(item.Name);
            }
            RequireItems();
            int ret = -1;
            if ((item.Generation & Column.ColumnGeneration.New) != 0)
            {
                ret = ((IList)_list).Add(item);
            }
            if ((item.Generation & Column.ColumnGeneration.Old) != 0)
            {
                _list[1].Add(item);
            }
            _nameToColumn = null;
            return ret;
        }

        public void Clear()
        {
            if (_list != null)
            {
                _list[0]?.Clear();
            }
            _nameToColumn = null;
        }

        public void Clear(bool isOld)
        {
            if (_list != null)
            {
                _list[isOld ? 1 : 0]?.Clear();
            }
            _nameToColumn = null;
        }

        public bool Contains(Column item)
        {
            RequireItems();
            return _list[0].Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Column[] array, int arrayIndex)
        {
            RequireItems();
            _list[0].CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list[0]).CopyTo(array, index);
        }

        public IEnumerator<Column> GetEnumerator()
        {
            RequireItems();
            return _list[0].GetEnumerator();
        }

        public bool Remove(Column item)
        {
            RequireItems();
            bool ret = _list[0].Remove(item);
            if (ret)
            {
                _nameToColumn = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list[0].Remove(value as Column);
            if (ret)
            {
                _nameToColumn = null;
            }
        }

        public bool Remove(Column item, bool isOld)
        {
            RequireItems();
            bool ret = _list[isOld ? 1 : 0].Remove(item);
            if (ret)
            {
                _nameToColumn = null;
            }
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list[0].GetEnumerator();
        }

        public int IndexOf(Column item)
        {
            return _list[0].IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list[0]).IndexOf(value);
        }

        public void Insert(int index, Column item)
        {
            _list[0].Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list[0]).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list[0].RemoveAt(index);
        }
        #endregion
    }

    public sealed class ConstraintCollection: IList<Constraint>, IList
    {
        private Selectable _owner;
        private List<Constraint> _list = null;
        private Dictionary<string, Constraint> _nameToConstraint = null;

        public ConstraintCollection(Selectable owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            _list = null;
            _nameToConstraint = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<Constraint>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }

            foreach (Constraint c in _owner.Schema.Constraints)
            {
                if (c == null)
                {
                    continue;
                }
                if (c.Table == null)
                {
                    continue;
                }
                if (!c.Table.Equals(_owner))
                {
                    continue;
                }
                _list.Add(c);
            }
            _list.Sort();
        }
        private void RequireNameToConstraint()
        {
            if (_nameToConstraint != null)
            {
                return;
            }
            _nameToConstraint = new Dictionary<string, Constraint>();
            RequireItems();
            foreach (Constraint c in _list)
            {
                _nameToConstraint.Add(c.Name, c);
            }
        }
        public Constraint this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }
        }
        public Constraint this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToConstraint();
                Constraint ret;
                if (!_nameToConstraint.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool ConstraintNameChanging(Constraint Constraint, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (Constraint != null && Constraint.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void ConstraintNameChanged(Constraint Constraint)
        {
            _nameToConstraint = null;
        }
        #region ICollection<Constraint>の実装
        public int Count
        {
            get
            {
                RequireItems();
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                ((IList)_list)[index] = value;
            }
        }

        Constraint IList<Constraint>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }

            set
            {
                RequireItems();
                _list[index] = value;
            }
        }

        public void Add(Constraint item)
        {
            RequireItems();
            _list.Add(item);
            _nameToConstraint = null;
        }
        int IList.Add(object value)
        {
            Constraint item = value as Constraint;
            RequireItems();
            int ret = -1;
            ret = ((IList)_list).Add(item);
            _nameToConstraint = null;
            return ret;
        }

        public void Clear()
        {
            _list?.Clear();
            _nameToConstraint = null;
        }

        public bool Contains(Constraint item)
        {
            RequireItems();
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Constraint[] array, int arrayIndex)
        {
            RequireItems();
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<Constraint> GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public bool Remove(Constraint item)
        {
            RequireItems();
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToConstraint = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list.Remove(value as Constraint);
            if (ret)
            {
                _nameToConstraint = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public int IndexOf(Constraint item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, Constraint item)
        {
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }
    public sealed class ReferedForeignKeyCollection : IList<ForeignKeyConstraint>, IList
    {
        private Table _owner;
        private List<ForeignKeyConstraint> _list = null;
        private Dictionary<string, ForeignKeyConstraint> _nameToConstraint = null;

        public ReferedForeignKeyCollection(Table owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            _list = null;
            _nameToConstraint = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<ForeignKeyConstraint>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }

            foreach (Constraint c in _owner.Schema.Constraints)
            {
                ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                if (fc == null)
                {
                    continue;
                }                
                if (fc.ReferenceConstraint == null || fc.ReferenceConstraint.Table == null)
                {
                    continue;
                }
                if (!fc.ReferenceConstraint.Table.Equals(_owner))
                {
                    continue;
                }
                _list.Add(fc);
            }
            _list.Sort();
        }
        private void RequireNameToConstraint()
        {
            if (_nameToConstraint != null)
            {
                return;
            }
            _nameToConstraint = new Dictionary<string, ForeignKeyConstraint>();
            RequireItems();
            foreach (ForeignKeyConstraint c in _list)
            {
                _nameToConstraint.Add(c.Name, c);
            }
        }
        public Constraint this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }
        }
        public ForeignKeyConstraint this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToConstraint();
                ForeignKeyConstraint ret;
                if (!_nameToConstraint.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool ConstraintNameChanging(Constraint Constraint, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (Constraint != null && Constraint.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void ConstraintNameChanged(Constraint Constraint)
        {
            _nameToConstraint = null;
        }
        #region ICollection<Constraint>の実装
        public int Count
        {
            get
            {
                RequireItems();
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                ((IList)_list)[index] = value;
            }
        }

        ForeignKeyConstraint IList<ForeignKeyConstraint>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }

            set
            {
                RequireItems();
                _list[index] = value;
            }
        }

        public void Add(ForeignKeyConstraint item)
        {
            RequireItems();
            _list.Add(item);
            _nameToConstraint = null;
        }
        int IList.Add(object value)
        {
            ForeignKeyConstraint item = value as ForeignKeyConstraint;
            RequireItems();
            int ret = -1;
            ret = ((IList)_list).Add(item);
            _nameToConstraint = null;
            return ret;
        }

        public void Clear()
        {
            _list?.Clear();
            _nameToConstraint = null;
        }

        public bool Contains(ForeignKeyConstraint item)
        {
            RequireItems();
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(ForeignKeyConstraint[] array, int arrayIndex)
        {
            RequireItems();
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<ForeignKeyConstraint> GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public bool Remove(ForeignKeyConstraint item)
        {
            RequireItems();
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToConstraint = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list.Remove(value as ForeignKeyConstraint);
            if (ret)
            {
                _nameToConstraint = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public int IndexOf(ForeignKeyConstraint item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, ForeignKeyConstraint item)
        {
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }

    public sealed class TriggerCollection: IList<Trigger>, IList
    {
        private SchemaObject _owner;
        private List<Trigger> _list = null;
        private Dictionary<string, Trigger> _nameToTrigger = null;

        public TriggerCollection(SchemaObject owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            _list = null;
            _nameToTrigger = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<Trigger>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }
            foreach (Trigger t in _owner.Schema.Triggers)
            {
                if (t == null)
                {
                    continue;
                }
                if (t.Table == null)
                {
                    continue;
                }
                if (!t.Table.Equals(_owner))
                {
                    continue;
                }
                _list.Add(t);
            }
            _list.Sort();
        }
        private void RequireNameToTrigger()
        {
            if (_nameToTrigger != null)
            {
                return;
            }
            _nameToTrigger = new Dictionary<string, Trigger>();
            RequireItems();
            foreach (Trigger c in _list)
            {
                _nameToTrigger.Add(c.Name, c);
            }
        }
        public Trigger this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }
        }
        public Trigger this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToTrigger();
                Trigger ret;
                if (!_nameToTrigger.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool TriggerNameChanging(Trigger Trigger, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (Trigger != null && Trigger.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void TriggerNameChanged(Trigger Trigger)
        {
            _nameToTrigger = null;
        }
        #region ICollection<Trigger>の実装
        public int Count
        {
            get
            {
                RequireItems();
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                ((IList)_list)[index] = value;
            }
        }

        Trigger IList<Trigger>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }

            set
            {
                RequireItems();
                _list[index] = value;
            }
        }

        public void Add(Trigger item)
        {
            RequireItems();
            _list.Add(item);
            _nameToTrigger = null;
        }
        int IList.Add(object value)
        {
            Trigger item = value as Trigger;
            RequireItems();
            int ret = -1;
            ret = ((IList)_list).Add(item);
            _nameToTrigger = null;
            return ret;
        }

        public void Clear()
        {
            _list?.Clear();
            _nameToTrigger = null;
        }

        public bool Contains(Trigger item)
        {
            RequireItems();
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Trigger[] array, int arrayIndex)
        {
            RequireItems();
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<Trigger> GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public bool Remove(Trigger item)
        {
            RequireItems();
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToTrigger = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list.Remove(value as Trigger);
            if (ret)
            {
                _nameToTrigger = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public int IndexOf(Trigger item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, Trigger item)
        {
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }

    public sealed class IndexCollection: IList<Index>, IList
    {
        private Selectable _owner;
        private List<Index> _list = null;
        private Dictionary<string, Index> _nameToIndex = null;

        public IndexCollection(Selectable owner)
        {
            _owner = owner;
        }

        public void Invalidate()
        {
            _list = null;
            _nameToIndex = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<Index>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }
            foreach (Index c in _owner.Schema.Indexes)
            {
                if (c == null)
                {
                    continue;
                }
                if (c.Table == null)
                {
                    continue;
                }
                if (!c.Table.Equals(_owner))
                {
                    continue;
                }
                _list.Add(c);
            }
            _list.Sort();
        }
        private void RequireNameToIndex()
        {
            if (_nameToIndex != null)
            {
                return;
            }
            _nameToIndex = new Dictionary<string, Index>();
            RequireItems();
            foreach (Index c in _list)
            {
                _nameToIndex.Add(c.Name, c);
            }
        }
        public Index this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }
        }
        public Index this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToIndex();
                Index ret;
                if (!_nameToIndex.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool IndexNameChanging(Index Index, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (Index != null && Index.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void IndexNameChanged(Index Index)
        {
            _nameToIndex = null;
        }
        #region ICollection<Index>の実装
        public int Count
        {
            get
            {
                RequireItems();
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                ((IList)_list)[index] = value;
            }
        }

        Index IList<Index>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }

            set
            {
                RequireItems();
                _list[index] = value;
            }
        }

        public void Add(Index item)
        {
            RequireItems();
            _list.Add(item);
            _nameToIndex = null;
        }
        int IList.Add(object value)
        {
            Index item = value as Index;
            RequireItems();
            int ret = -1;
            ret = ((IList)_list).Add(item);
            _nameToIndex = null;
            return ret;
        }

        public void Clear()
        {
            _list?.Clear();
            _nameToIndex = null;
        }

        public bool Contains(Index item)
        {
            RequireItems();
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Index[] array, int arrayIndex)
        {
            RequireItems();
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<Index> GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public bool Remove(Index item)
        {
            RequireItems();
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToIndex = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list.Remove(value as Index);
            if (ret)
            {
                _nameToIndex = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public int IndexOf(Index item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, Index item)
        {
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }

    public abstract partial class SchemaObject: NamedObject, ICommentable, IComparable
    {
        private Schema _schema;
        private string _name;
        public Db2SourceContext Context { get; private set; }
        public abstract string GetSqlType();
        public abstract string GetExportFolderName();

        public string Prefix { get; set; }
        public string Owner { get; set; }
        public Schema Schema { get { return _schema; } }
        public string SchemaName { get { return _schema?.Name; } }
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                {
                    return;
                }
                string old = _name;
                _name = value;
                NameChanged(old);
            }
        }
        protected virtual void NameChanged(string oldValue)
        {
            UpdateIdenfitier();
        }

        public virtual string DisplayName
        {
            get
            {
                return _name;
            }
        }
        protected internal virtual void UpdateIdenfitier()
        {
            Identifier = _name;
        }

        public string FullName
        {
            get
            {
                string s = SchemaName;
                if (!string.IsNullOrEmpty(s))
                {
                    s = s + ".";
                }
                return s + Name;
            }
        }
        public string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Name, baseSchemaName);
        }

        public override bool IsModified()
        {
            if (base.IsModified())
            {
                return true;
            }
            if (_comment != null)
            {
                return _comment.IsModified();
            }
            return false;
        }

        private Comment _comment;
        public Comment Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (_comment == value)
                {
                    return;
                }
                CommentChangedEventArgs e = new CommentChangedEventArgs(value);
                _comment = value;
                OnCommentChanged(e);
            }
        }

        public string CommentText
        {
            get
            {
                return Comment?.Text;
            }
            set
            {
                if (Comment == null)
                {
                    Comment = new Comment(Context, SchemaName, Name, value, false);
                    Comment.Link();
                }
                Comment.Text = value;
            }
        }

        public string SqlDef { get; set; } = null;
        public TriggerCollection Triggers { get; private set; }

        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
        protected internal void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        public event EventHandler<CommentChangedEventArgs> CommentChanged;
        void ICommentable.OnCommentChanged(CommentChangedEventArgs e)
        {
            CommentChanged?.Invoke(this, e);
        }

        protected internal void OnCommentChanged(CommentChangedEventArgs e)
        {
            CommentChanged?.Invoke(this, e);
        }

        public event EventHandler<CollectionOperationEventArgs<string>> UpdateColumnChanged;
        protected internal void OnUpdateColumnChanged(CollectionOperationEventArgs<string> e)
        {
            UpdateColumnChanged?.Invoke(this, e);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal SchemaObject(Db2SourceContext context, string owner, string schema, string objectName, Schema.CollectionIndex index) : base(context.RequireSchema(schema).GetCollection(index))
        {
            Context = context;
            Owner = owner;
            _schema = context.RequireSchema(schema);
            Name = objectName;
            Triggers = new TriggerCollection(this);
        }

        public override void Release()
        {
            Schema.GetCollection(GetCollectionIndex()).Remove(this);
            foreach (Trigger t in Triggers)
            {
                t.Release();
            }
        }

        public virtual void InvalidateColumns() { }
        public virtual void InvalidateConstraints() { }
        public virtual void InvalidateTriggers()
        {
            Triggers.Invalidate();
        }
        public virtual Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Objects;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is SchemaObject))
            {
                return false;
            }
            SchemaObject sc = (SchemaObject)obj;
            return Schema.Equals(sc.Schema) && (Identifier == sc.Identifier);
        }
        public override int GetHashCode()
        {
            return ((Schema != null) ? Schema.GetHashCode() : 0) + (string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode());
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", GetSqlType(), DisplayName);
        }

        public override int CompareTo(object obj)
        {
            if (!(obj is SchemaObject))
            {
                return -1;
            }
            SchemaObject sc = (SchemaObject)obj;
            int ret = Schema.CompareTo(sc.Schema);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(Identifier, sc.Identifier);
            return ret;
        }
    }

    public partial class Sequence: SchemaObject
    {
        public override string GetSqlType()
        {
            return "SEQUENCE";
        }
        public override string GetExportFolderName()
        {
            return "Sequence";
        }

        public string StartValue { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
        public string Increment { get; set; }
        public bool IsCycled { get; set; }
        public int Cache { get; set; } = 1;
        public string OwnedSchema { get; set; }
        public string OwnedTable { get; set; }
        public string OwnedColumn { get; set; }

        internal Sequence(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }
    }

    public abstract class Selectable: SchemaObject
    {
        public event EventHandler<ColumnPropertyChangedEventArgs> ColumnPropertyChanged;
        protected internal void OnColumnPropertyChanged(ColumnPropertyChangedEventArgs e)
        {
            ColumnPropertyChanged?.Invoke(this, e);
        }
        public ColumnCollection Columns { get; private set; }
        internal Selectable(Db2SourceContext context, string owner, string schema, string tableName) : base(context, owner, schema, tableName, Schema.CollectionIndex.Objects)
        {
            Columns = new ColumnCollection(this);
        }

        public override void Release()
        {
            base.Release();
            foreach (Column c in Columns)
            {
                c.Release();
            }
            Comment.Release();
        }

        public override void InvalidateColumns()
        {
            Columns.Invalidate();
        }
        public override bool IsModified()
        {
            if (base.IsModified())
            {
                return true;
            }
            for (int i = 0; i < Columns.Count; i++)
            {
                Column newCol = Columns[i, false];
                if (newCol.IsModified())
                {
                    return true;
                }
                Column oldCol = Columns[i, true];
                if (newCol.CompareTo(oldCol) != 0)
                {
                    return true;
                }
            }
            return false;
        }
        public string GetSelectSQL(string where, string orderBy, int? limit, bool includesHidden, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            string delim = "  ";
            buf.AppendLine("select");
            int w = 0;
            foreach (Column c in Columns.AllColumns)
            {
                if (!includesHidden && c.IsHidden)
                {
                    continue;
                }
                if (!c.IsSupportedType)
                {
                    continue;
                }
                buf.Append(delim);
                w += delim.Length;
                if (80 <= w)
                {
                    buf.AppendLine();
                    buf.Append("  ");
                    w = 2;
                }
                string s = c.EscapedName;
                buf.Append(s);
                w += s.Length;
                delim = ", ";
            }
            buf.AppendLine();
            buf.Append("from ");
            buf.AppendLine(EscapedIdentifier(null));
            whereOffset = buf.Length;
            if (!string.IsNullOrEmpty(where))
            {
                buf.Append("where ");
                whereOffset = buf.Length;
                buf.AppendLine(where);
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                buf.Append("order by ");
                buf.AppendLine(orderBy);
            }
            if (limit.HasValue)
            {
                buf.Append("limit ");
                buf.Append(limit.Value);
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public string GetSelectSQL(string where, string orderBy, int? limit, bool includesHidden)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, includesHidden, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, bool includesHidden, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            if (0 < where.Length)
            {
                buf.AppendLine(where[0]);
                for (int i = 1; i < where.Length; i++)
                {
                    buf.Append("  ");
                    buf.AppendLine(where[i]);
                }
            }
            return GetSelectSQL(buf.ToString(), orderBy, limit, includesHidden, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, bool includesHidden)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, includesHidden, out whereOffset);
        }
        public string GetSelectSQL(string where, string[] orderBy, int? limit, bool includesHidden)
        {
            StringBuilder bufO = new StringBuilder();
            if (0 < orderBy.Length)
            {
                bufO.Append(orderBy[0]);
                for (int i = 1; i < orderBy.Length; i++)
                {
                    bufO.Append(", ");
                    bufO.Append(orderBy[i]);
                }
            }
            return GetSelectSQL(where, bufO.ToString(), limit, includesHidden);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, bool includesHidden, out int whereOffset)
        {
            StringBuilder bufW = new StringBuilder();
            if (0 < where.Length)
            {
                bufW.AppendLine(where[0]);
                for (int i = 1; i < where.Length; i++)
                {
                    bufW.Append("  ");
                    bufW.AppendLine(where[i]);
                }
            }
            StringBuilder bufO = new StringBuilder();
            if (0 < orderBy.Length)
            {
                bufO.Append(orderBy[0]);
                for (int i = 1; i < orderBy.Length; i++)
                {
                    bufO.Append(", ");
                    bufO.Append(orderBy[i]);
                }
            }
            return GetSelectSQL(bufW.ToString().TrimEnd(), bufO.ToString(), limit, includesHidden, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, bool includesHidden)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, includesHidden, out whereOffset);
        }
    }
    public partial class Table: Selectable
    {
        public override string GetSqlType()
        {
            return "TABLE";
        }
        public override string GetExportFolderName()
        {
            return "Table";
        }
        public ConstraintCollection Constraints { get; private set; }
        public IndexCollection Indexes { get; private set; }
        public string TablespaceName { get; set; }
        public string[] ExtraInfo { get; set; }
        public Regex TemporaryNamePattern { get; set; }
        public ReferedForeignKeyCollection ReferFrom { get; private set; }
        public List<ForeignKeyConstraint> ReferTo
        {
            get
            {
                List<ForeignKeyConstraint> l = new List<ForeignKeyConstraint>();
                foreach (Constraint c in Constraints)
                {
                    ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                    if (fc != null)
                    {
                        l.Add(fc);
                    }
                }
                return l;
            }
        }
        public override void InvalidateConstraints()
        {
            Constraints.Invalidate();
            _primaryKey = null;
            _firstCandidateKey = null;
        }
        public void InvalidateIndexes()
        {
            Indexes.Invalidate();
        }

        private KeyConstraint _primaryKey;
        private KeyConstraint _firstCandidateKey;
        private void UpdatePrimaryKey()
        {
            if (_firstCandidateKey != null)
            {
                return;
            }
            _primaryKey = null;
            _firstCandidateKey = null;
            foreach (Constraint c in Constraints)
            {
                KeyConstraint k = c as KeyConstraint;
                if (k == null)
                {
                    continue;
                }
                if (k.ConstraintType == ConstraintType.Primary)
                {
                    _primaryKey = k;
                    _firstCandidateKey = k;
                    return;
                }
                if (k.ConstraintType == ConstraintType.Unique)
                {
                    if (_firstCandidateKey == null)
                    {
                        _firstCandidateKey = k;
                    }
                }
            }
        }
        public KeyConstraint PrimaryKey
        {
            get
            {
                UpdatePrimaryKey();
                return _primaryKey;
            }
        }

        public KeyConstraint FirstCandidateKey
        {
            get
            {
                UpdatePrimaryKey();
                return _firstCandidateKey;
            }
        }

        public string GetKeyConditionSQL(string alias, string prefix, int indent)
        {
            string[] cond = GetKeyConditionSQL(alias);
            string spc = new string(' ', indent);
            StringBuilder buf = new StringBuilder();
            bool needAnd = false;
            foreach (string s in cond)
            {
                if (needAnd)
                {
                    buf.Append(spc);
                    buf.Append("  and ");
                }
                else
                {
                    buf.Append(spc);
                    buf.Append(prefix.TrimEnd());
                    buf.Append(' ');
                }
                buf.AppendLine(s);
                needAnd = true;
            }
            return buf.ToString();
        }
        public string[] GetKeyConditionSQL(string alias)
        {
            if (FirstCandidateKey == null)
            {
                return new string[0];
            }
            List<string> l = new List<string>();
            string a = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            string[] cols = FirstCandidateKey.Columns;
            if (cols != null)
            {
                foreach (string c in FirstCandidateKey.Columns)
                {
                    l.Add(string.Format("{0}{1} = :old_{2}", a, Context.GetEscapedIdentifier(c), c));
                }
            }
            return l.ToArray();
        }

        public string GetInsertSql(int indent, int charPerLine, string postfix)
        {
            return Context.GetInsertSql(this, indent, charPerLine, postfix);
        }
        public string GetUpdateSql(string where, int indent, int charPerLine, string postfix)
        {
            return Context.GetUpdateSql(this, where, indent, charPerLine, postfix);
        }
        public string GetDeleteSql(string where, int indent, int charPerLine, string postfix)
        {
            return Context.GetDeleteSql(this, where, indent, charPerLine, postfix);
        }

        internal Table(Db2SourceContext context, string owner, string schema, string tableName) : base(context, owner, schema, tableName)
        {
            Constraints = new ConstraintCollection(this);
            Indexes = new IndexCollection(this);
            ReferFrom = new ReferedForeignKeyCollection(this);
        }
    }
    public partial class View: Selectable
    {
        public override string GetSqlType()
        {
            return "VIEW";
        }
        public override string GetExportFolderName()
        {
            return "View";
        }
        private string _definition;
        private string _oldDefinition;
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }
        public override bool IsModified()
        {
            return _definition != _oldDefinition;
        }
        public string[] ExtraInfo { get; set; }
        internal View(Db2SourceContext context, string owner, string schema, string viewName, string defintion, bool isLoaded) : base(context, owner, schema, viewName)
        {
            _definition = defintion;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
    [Flags]
    public enum TriggerEvent
    {
        Unknown = 1,
        Insert = 2,
        Delete = 4,
        Truncate = 8,
        Update = 16,
    }
    public enum TriggerOrientation
    {
        Unknown,
        Statement,
        Row
    }
    public enum TriggerTiming
    {
        Unknown,
        Before,
        After,
        InsteadOf
    }
    public partial class Trigger: SchemaObject
    {
        public class StringCollection: IList<string>, IList
        {
            private Trigger _owner;
            private List<string> _items = new List<string>();

            public void AddRange(IEnumerable<string> collection)
            {
                _items.AddRange(collection);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.AddRange, -1, null, null));
            }

            #region interfaceの実装
            public string this[int index]
            {
                get
                {
                    return ((IList<string>)_items)[index];
                }

                set
                {
                    string sNew = value;
                    string sOld = _items[index];
                    ((IList<string>)_items)[index] = value;
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Update, index, sNew, sOld));
                }
            }

            object IList.this[int index]
            {
                get
                {
                    return ((IList)_items)[index];
                }

                set
                {
                    string sNew = (string)value;
                    string sOld = _items[index];
                    ((IList)_items)[index] = value;
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Update, index, sNew, sOld));
                }
            }

            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public bool IsFixedSize
            {
                get
                {
                    return false;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((IList)_items).IsSynchronized;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return ((IList)_items).SyncRoot;
                }
            }

            public int Add(object value)
            {
                int ret = ((IList)_items).Add(value);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, ret, (string)value, null));
                return ret;
            }

            public void Add(string item)
            {
                int ret = ((IList)_items).Add(item);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, ret, item, null));
            }

            public void Clear()
            {
                if (_items.Count == 0)
                {
                    return;
                }
                _items.Clear();
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Clear, -1, null, null));
            }

            public bool Contains(object value)
            {
                return _items.Contains((string)value);
            }

            public bool Contains(string item)
            {
                return _items.Contains(item);
            }

            public void CopyTo(Array array, int index)
            {
                ((IList)_items).CopyTo(array, index);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                ((IList<string>)_items).CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return ((IList<string>)_items).GetEnumerator();
            }

            public int IndexOf(object value)
            {
                return ((IList)_items).IndexOf(value);
            }

            public int IndexOf(string item)
            {
                return ((IList<string>)_items).IndexOf(item);
            }

            public void Insert(int index, object value)
            {
                ((IList)_items).Insert(index, value);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, index, (string)value, null));
            }

            public void Insert(int index, string item)
            {
                ((IList<string>)_items).Insert(index, item);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, index, item, null));
            }

            public void Remove(object value)
            {
                if (_items.Remove((string)value))
                {
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, -1, null, (string)value));
                }
            }

            public bool Remove(string item)
            {
                bool ret = _items.Remove(item);
                if (ret)
                {
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, -1, null, item));
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                string sOld = _items[index];
                _items.RemoveAt(index);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, index, null, sOld));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<string>)_items).GetEnumerator();
            }
            #endregion
            private void OnUpdateColumnChanged(CollectionOperationEventArgs<string> e)
            {
                _owner?.OnUpdateColumnChanged(e);
            }
            internal StringCollection(Trigger owner)
            {
                _owner = owner;
            }
        }
        public override string GetSqlType()
        {
            return "TRIGGER";
        }
        public override string GetExportFolderName()
        {
            return "Trigger";
        }
        private TriggerTiming _timing;
        private string _timingText;
        private TriggerEvent _event;
        private string _eventText;
        private StringCollection _updateEventColumns;
        private string _tableSchema;
        private string _tableName;
        private SchemaObject _table;
        private string _referencedTableName;
        private string _procedureSchema;
        private string _procedureName;
        private StoredFunction _procedure;
        private TriggerOrientation _orientation;
        private string _orientationText;
        private string _referenceNewTable;
        private string _referenceOldTable;
        private string _condition;
        private string _referenceNewRow;
        private string _referenceOldRow;
        private string _definition;
        private string _oldDefinition;

        public TriggerTiming Timing
        {
            get
            {
                return _timing;
            }
            set
            {
                if (_timing == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Timing", value, _timing);
                _timing = value;
                OnPropertyChanged(e);
            }
        }
        public string TimingText
        {
            get
            {
                return _timingText;
            }
            set
            {
                if (_timingText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TimingText", value, _timingText);
                _timingText = value;
                OnPropertyChanged(e);
            }
        }
        public TriggerEvent Event
        {
            get
            {
                return _event;
            }
            set
            {
                if (_event == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Event", value, _event);
                _event = value;
                OnPropertyChanged(e);
            }
        }
        public string EventText
        {
            get
            {
                return _eventText;
            }
            set
            {
                if (_eventText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("EventText", value, _eventText);
                _eventText = value;
                OnPropertyChanged(e);
            }
        }

        public StringCollection UpdateEventColumns
        {
            get
            {
                return _updateEventColumns;
            }
        }

        public string UpdateEventColumnsText
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                bool needComma = false;
                foreach (string s in UpdateEventColumns)
                {
                    if (needComma)
                    {
                        buf.Append(", ");
                    }
                    buf.Append(s);
                    needComma = true;
                }
                return buf.ToString();
            }
            set
            {
                string[] cols = value.Split(',');
                for (int i = 0; i < cols.Length; i++)
                {
                    cols[i] = cols[i].Trim();
                }
                _updateEventColumns.Clear();
                _updateEventColumns.AddRange(cols);
            }
        }

        public bool HasUpdateEventColumns()
        {
            return (0 < _updateEventColumns.Count);
        }
        public string GetUpdateEventColumnsSql(string prefix, string indent, ref int pos, ref int line, int softLimit, int hardLimit)
        {
            if (indent == null)
            {
                throw new ArgumentNullException("indent");
            }
            if (UpdateEventColumns.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder(prefix);
            int c = pos + (prefix != null ? prefix.Length : 0);
            bool needComma = false;
            foreach (string s in UpdateEventColumns)
            {
                if (needComma)
                {
                    buf.Append(',');
                    c++;
                    if (c < softLimit)
                    {
                        buf.Append(' ');
                        c++;
                    }
                    else
                    {
                        buf.AppendLine();
                        line++;
                        buf.Append(indent);
                        c = indent.Length;
                    }
                }
                if (hardLimit <= c + s.Length)
                {
                    buf.AppendLine();
                    line++;
                    buf.Append(indent);
                    c = indent.Length;
                }
                buf.Append(s);
                c += s.Length;
                needComma = true;
            }
            pos = c;
            return buf.ToString();
        }

        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context?.Objects[TableSchema, TableName];
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public SchemaObject Table
        {
            get
            {
                UpdateTable();
                return _table;
            }
            set
            {
                if (_table == value)
                {
                    return;
                }
                _table = value;
                if (_table == null)
                {
                    return;
                }
                PropertyChangedEventArgs e1 = null;
                PropertyChangedEventArgs e2 = null;
                if (_tableSchema != _table.SchemaName)
                {
                    e1 = new PropertyChangedEventArgs("TableSchema", _table.SchemaName, _tableSchema);
                    _tableSchema = _table.SchemaName;
                }
                if (_tableName != _table.Name)
                {
                    e2 = new PropertyChangedEventArgs("TableName", _table.Name, _tableName);
                    _tableSchema = _table.SchemaName;
                }
                if (e1 != null)
                {
                    OnPropertyChanged(e1);
                }
                if (e2 != null)
                {
                    OnPropertyChanged(e2);
                }
            }
        }
        public string TableSchema
        {
            get
            {
                return _tableSchema;
            }
            set
            {
                if (_tableSchema == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableSchema", value, _tableSchema);
                _tableSchema = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }
        public string TableName
        {
            get
            {
                return _tableName;
            }
            set
            {
                if (_tableName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableName", value, _tableName);
                _tableName = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }

        private void InvalidateProcedure()
        {
            _procedure = null;
        }
        private void UpdateProcedure()
        {
            if (_procedure != null)
            {
                return;
            }
            _procedure = Context?.StoredFunctions[_procedureSchema, _procedureName];
        }
        public StoredFunction Procedure
        {
            get
            {
                UpdateProcedure();
                return _procedure;
            }
        }
        public string ProcedureSchema
        {
            get { return _procedureSchema; }
            set
            {
                if (_procedureSchema == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ProcedureSchema", value, _procedureSchema);
                _procedureSchema = value;
                InvalidateProcedure();
                OnPropertyChanged(e);
            }
        }
        public string ProcedureName
        {
            get { return _procedureName; }
            set
            {
                if (_procedureName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ProcedureName", value, _procedureName);
                _procedureName = value;
                InvalidateProcedure();
                OnPropertyChanged(e);
            }
        }

        public string ReferencedTableName
        {
            get
            {
                return _referencedTableName;
            }
            set
            {
                if (_referencedTableName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferencedTableName", value, _referencedTableName);
                _referencedTableName = value;
                OnPropertyChanged(e);
            }
        }
        public TriggerOrientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if (_orientation == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Orientation", value, _orientation);
                _orientation = value;
                OnPropertyChanged(e);
            }
        }

        public string OrientationText
        {
            get
            {
                return _orientationText;
            }
            set
            {
                if (_orientationText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("OrientationText", value, _orientationText);
                _orientationText = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceNewTable
        {
            get
            {
                return _referenceNewTable;
            }
            set
            {
                if (_referenceNewTable == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceNewTable", value, _referenceNewTable);
                _referenceNewTable = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceOldTable
        {
            get
            {
                return _referenceOldTable;
            }
            set
            {
                if (_referenceOldTable == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceOldTable", value, _referenceOldTable);
                _referenceOldTable = value;
                OnPropertyChanged(e);
            }
        }
        public string Condition
        {
            get
            {
                return _condition;
            }
            set
            {
                if (_condition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Condition", value, _condition);
                _condition = value;
                OnPropertyChanged(e);
            }
        }

        public string ReferenceNewRow
        {
            get
            {
                return _referenceNewRow;
            }
            set
            {
                if (_referenceNewRow == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceNewRow", value, _referenceNewRow);
                _referenceNewRow = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceOldRow
        {
            get
            {
                return _referenceOldRow;
            }
            set
            {
                if (_referenceOldRow == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceOldRow", value, _referenceOldRow);
                _referenceOldRow = value;
                OnPropertyChanged(e);
            }
        }
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }
        public override bool IsModified()
        {
            return _definition != _oldDefinition;
        }

        public override Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Triggers;
        }
        public string[] ExtraInfo { get; set; }
        internal Trigger(Db2SourceContext context, string owner, string triggerSchema, string triggerName, string tableSchema, string tableName, string defintion, bool isLoaded) : base(context, owner, triggerSchema, triggerName, Schema.CollectionIndex.Triggers)
        {
            _updateEventColumns = new StringCollection(this);
            _tableSchema = tableSchema;
            _tableName = tableName;
            _definition = defintion;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
    public partial class StoredFunction: SchemaObject, IDbTypeDef
    {
        public class ParameterCollection: IList<Parameter>, IList
        {
            private StoredFunction _owner;
            private List<Parameter> _items;
            protected internal void OnParameterChantged(CollectionOperationEventArgs<Parameter> e)
            {
                _owner.OnParameterChantged(e);
            }
            internal ParameterCollection(StoredFunction owner)
            {
                _owner = owner;
                _items = new List<Parameter>();
            }
            #region Interfaceの実装

            public Parameter this[int index]
            {
                get
                {
                    return _items[index];
                }

                set
                {
                    if (_items[index] == value)
                    {
                        return;
                    }
                    CollectionOperationEventArgs<Parameter> e = new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Update, index, value, _items[index]);
                    _items[index] = value;
                    OnParameterChantged(e);
                }
            }

            object IList.this[int index]
            {
                get
                {
                    return ((IList)_items)[index];
                }

                set
                {
                    if (((IList)_items)[index] == value)
                    {
                        return;
                    }
                    CollectionOperationEventArgs<Parameter> e = new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Update, index, (Parameter)value, _items[index]);
                    ((IList)_items)[index] = value;
                    OnParameterChantged(e);
                }
            }

            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public bool IsFixedSize
            {
                get
                {
                    return ((IList)_items).IsFixedSize;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((IList<Parameter>)_items).IsReadOnly;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((IList)_items).IsSynchronized;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return ((IList)_items).SyncRoot;
                }
            }

            int IList.Add(object value)
            {
                int ret = ((IList)_items).Add(value);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, (Parameter)value, null));
                return ret;
            }

            public void Add(Parameter item)
            {
                int ret = ((IList)_items).Add(item);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, item, null));
            }

            public void Clear()
            {
                _items.Clear();
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Clear, -1, null, null));
            }

            public bool Contains(object value)
            {
                return ((IList)_items).Contains(value);
            }

            public bool Contains(Parameter item)
            {
                return ((IList<Parameter>)_items).Contains(item);
            }

            public void CopyTo(Array array, int index)
            {
                ((IList)_items).CopyTo(array, index);
            }

            public void CopyTo(Parameter[] array, int arrayIndex)
            {
                ((IList<Parameter>)_items).CopyTo(array, arrayIndex);
            }

            public IEnumerator<Parameter> GetEnumerator()
            {
                return ((IList<Parameter>)_items).GetEnumerator();
            }

            public int IndexOf(object value)
            {
                return ((IList)_items).IndexOf(value);
            }

            public int IndexOf(Parameter item)
            {
                return ((IList<Parameter>)_items).IndexOf(item);
            }

            public void Insert(int index, object value)
            {
                ((IList)_items).Insert(index, value);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, (Parameter)value, null));
            }

            public void Insert(int index, Parameter item)
            {
                ((IList<Parameter>)_items).Insert(index, item);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, item, null));
            }

            public void Remove(object value)
            {
                ((IList)_items).Remove(value);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, (Parameter)value));
            }

            public bool Remove(Parameter item)
            {
                bool ret = _items.Remove(item);
                if (ret)
                {
                    OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, item));
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                Parameter old = _items[index];
                _items.RemoveAt(index);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, index, null, old));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<Parameter>)_items).GetEnumerator();
            }
            #endregion
        }

        private string _internalName;
        private string _definition;
        private string _oldDefinition;
        public string BaseType { get; set; }
        public int? DataLength { get; set; }
        public int? Precision { get; set; }
        public bool? WithTimeZone { get; set; }
        public bool IsSupportedType { get; set; }
        public void UpdateDataType()
        {
            _dataType = DbTypeDefUtil.ToTypeText(this);
        }
        private string _dataType;
        public string DataType
        {
            get
            {
                return _dataType;
            }
            set
            {
                if (_dataType == value)
                {
                    return;
                }
                //PropertyChangedEventArgs e = new PropertyChangedEventArgs("DataType", value, _dataType);
                _dataType = value;
                //OnPropertyChanged(e);
            }
        }

        public Type ValueType { get; set; }
        public ParameterCollection Parameters { get; private set; }
        public event EventHandler<CollectionOperationEventArgs<Parameter>> ParameterChantged;
        protected void OnParameterChantged(CollectionOperationEventArgs<Parameter> e)
        {
            ParameterChantged?.Invoke(this, e);
        }
        public string Language { get; set; }
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }
        public virtual string HeaderDef
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(Name);
                buf.Append('(');
                bool needComma = false;
                foreach (Parameter p in Parameters)
                {
                    if (needComma)
                    {
                        buf.Append(',');
                    }
                    buf.Append(p.Name);
                    buf.Append(' ');
                    buf.Append(p.DataType);
                    needComma = true;
                }
                buf.Append(") return ");
                buf.Append(DataType);
                return buf.ToString();
            }
        }
        public override string DisplayName
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(Name);
                buf.Append('(');
                bool needComma = false;
                foreach (Parameter p in Parameters)
                {
                    if (needComma)
                    {
                        buf.Append(',');
                    }
                    string s = p.DirectionStr;
                    if (!string.IsNullOrEmpty(s))
                    {
                        buf.Append(s);
                        buf.Append(' ');
                    }
                    buf.Append(p.DataType);
                    needComma = true;
                }
                buf.Append(')');
                return buf.ToString();
            }
        }
        protected internal override void UpdateIdenfitier()
        {
            Identifier = _internalName;
        }
        public override bool IsModified()
        {
            return _definition != _oldDefinition;
        }
        public string[] ExtraInfo { get; set; }

        private IDbCommand _dbCommand;
        internal void RequireDbCommand()
        {
            if (_dbCommand != null)
            {
                return;
            }
            _dbCommand = Context.GetSqlCommand(this, null, null);
            for (int i = 0; i < _dbCommand.Parameters.Count; i++)
            {
                Parameters[i].DbParameter = _dbCommand.Parameters[i] as IDbDataParameter;
            }
        }
        public IDbCommand DbCommand
        {
            get
            {
                RequireDbCommand();
                return _dbCommand;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StoredFunction(Db2SourceContext context, string owner, string schema, string objectName, string internalName, string definition, bool isLoaded) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects)
        {
            Parameters = new ParameterCollection(this);
            _internalName = internalName;
            UpdateIdenfitier();
            _definition = definition;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
    public class Index: SchemaObject
    {
        private Table _table;
        private string _tableSchema;
        private string _tableName;
        //private string[] _columns;

        public bool IsUnique { get; set; }
        public bool IsImplicit { get; set; }
        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context?.Tables[TableSchema, TableName];
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public Table Table
        {
            get
            {
                UpdateTable();
                return _table;
            }
            set
            {
                if (_table == value)
                {
                    return;
                }
                _table = value;
                if (_table == null)
                {
                    return;
                }
                PropertyChangedEventArgs e1 = null;
                PropertyChangedEventArgs e2 = null;
                if (_tableSchema != _table.SchemaName)
                {
                    e1 = new PropertyChangedEventArgs("TableSchema", _table.SchemaName, _tableSchema);
                    _tableSchema = _table.SchemaName;
                }
                if (_tableName != _table.Name)
                {
                    e2 = new PropertyChangedEventArgs("TableName", _table.Name, _tableName);
                    _tableSchema = _table.SchemaName;
                }
                if (e1 != null)
                {
                    OnPropertyChanged(e1);
                }
                if (e2 != null)
                {
                    OnPropertyChanged(e2);
                }
            }
        }
        public string TableSchema
        {
            get
            {
                return _tableSchema;
            }
            set
            {
                if (_tableSchema == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableSchema", value, _tableSchema);
                _tableSchema = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }
        public string TableName
        {
            get
            {
                return _tableName;
            }
            set
            {
                if (_tableName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableName", value, _tableName);
                _tableName = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }
        public string IndexType { get; set; }
        public string[] Columns { get; set; }
        //private string _definition;
        public override string GetSqlType()
        {
            return "INDEX";
        }
        public override string GetExportFolderName()
        {
            return "Index";
        }
        public override Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Indexes;
        }
        public Index(Db2SourceContext context, string owner, string schema, string indexName, string tableSchema, string tableName, string[] columns, string definition) : base(context, owner, schema, indexName, Schema.CollectionIndex.Indexes)
        {
            //_schema = context.RequireSchema(schema);
            //_name = indexName;
            _tableSchema = tableSchema;
            _tableName = tableName;
            Columns = columns;
            //_definition = definition;
        }
    }
    public abstract class Type_: SchemaObject
    {
        public override string GetSqlType()
        {
            return "TYPE";
        }
        public override string GetExportFolderName()
        {
            return "Type";
        }
        internal Type_(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }
    }
    public class BasicType: Type_
    {
        public string InputFunction { get; set; }
        public string OutputFunction { get; set; }
        public string ReceiveFunction { get; set; }
        public string SendFunction { get; set; }
        public string TypmodInFunction { get; set; }
        public string TypmodOutFunction { get; set; }
        public string AnalyzeFunction { get; set; }
        public string InternalLengthFunction { get; set; }
        public bool PassedbyValue { get; set; }
        public int Alignment { get; set; }
        public string Storage { get; set; }
        public string Like { get; set; }
        public string Category { get; set; }
        public bool Preferred { get; set; }
        public string Default { get; set; }
        public string Element { get; set; }
        public string Delimiter { get; set; }
        public bool Collatable { get; set; }
        internal BasicType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
    public class EnumType: Type_
    {
        public string[] Labels { get; set; }
        internal EnumType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
    public class ComplexType: Selectable
    {
        public override string GetSqlType()
        {
            return "TYPE";
        }
        public override string GetExportFolderName()
        {
            return "Type";
        }
        internal ComplexType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
    public class RangeType: Type_
    {
        public string Subtype { get; set; }
        public string SubtypeOpClass { get; set; }
        public string Collation { get; set; }
        public string CanonicalFunction { get; set; }
        public string SubtypeDiff { get; set; }
        internal RangeType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
}
