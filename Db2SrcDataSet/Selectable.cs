using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Db2Source
{
    public class ColumnPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public Column Column { get; private set; }
        internal ColumnPropertyChangedEventArgs(object sender, PropertyChangedEventArgs e) : base(e.Property, e.NewValue, e.OldValue)
        {
            Column = sender as Column;
        }
    }

    public partial class Column : NamedObject, ICommentable, IComparable, IDbTypeDef
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
        public object EvalDefaultValue()
        {
            try
            {
                return Context.Eval(DefaultValue);
            }
            catch
            {
                return null;
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

        public ForeignKeyConstraint[] ForeignKeys
        {
            get
            {
                return (Table as Table)?.GetForeignKeysForColumn(Name);
            }
        }

        public HiddenLevel HiddenLevel { get; set; } = HiddenLevel.Visible;

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

    public sealed class ColumnCollection : IList<Column>, IList
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
                if (c.HiddenLevel != HiddenLevel.Visible)
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

    [Serializable]
    public class ColumnNameException : ArgumentException
    {
        public ColumnNameException(string name) : base(string.Format("列名\"{0}\"が重複します", name)) { }
    }

    public abstract class Selectable : SchemaObject
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
        public string GetColumnsSQL(string alias, HiddenLevel visibleLevel)
        {
            StringBuilder buf = new StringBuilder();
            string delim = "  ";
            int w = 0;
            string a = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            foreach (Column c in Columns.AllColumns)
            {
                if (visibleLevel < c.HiddenLevel)
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
                buf.Append(a);
                string s = c.EscapedName;
                buf.Append(s);
                w += s.Length;
                delim = ", ";
            }
            return buf.ToString();
        }
        public string GetSelectSQL(string alias, string where, string orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            buf.AppendLine("select");
            buf.AppendLine(GetColumnsSQL(alias, visibleLevel));
            buf.Append("from ");
            buf.Append(EscapedIdentifier(null));
            if (!string.IsNullOrEmpty(alias))
            {
                buf.Append(" as ");
                buf.Append(alias);
            }
            buf.AppendLine();
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
        public string GetSelectSQL(string alias, string where, string orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
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
            return GetSelectSQL(alias, buf.ToString(), orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
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
            return GetSelectSQL(alias, where, bufO.ToString(), limit, visibleLevel);
        }
        public string GetSelectSQL(string alias, string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
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
            return GetSelectSQL(alias, bufW.ToString().TrimEnd(), bufO.ToString(), limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }
    }
}
