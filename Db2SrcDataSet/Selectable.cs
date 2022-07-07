using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;

namespace Db2Source
{
    public partial class Column : NamedObject, ICommentable, IComparable, IDbTypeDef
    {
        public string GetSqlType()
        {
            return "COLUMN";
        }
        internal Column _backup = null;
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
        protected override string GetFullIdentifier()
        {
            return Db2SourceContext.JointIdentifier(SchemaName, TableName, Name);
        }
        protected override string GetIdentifier()
        {
            return Db2SourceContext.JointIdentifier(TableName, Name);
        }
        private int _index;
        public int Index { get
            {
                return _index;
            }
            set
            {
                if (_index == value)
                {
                    return;
                }
                _index = value;
                OnPropertyChanged("Index");
            }
        }
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
                InvalidateIdentifier();
                OnPropertyChanged("TableName");
            }
        }

        public string EscapedName
        {
            get
            {
                return Context.GetEscapedIdentifier(Name, true);
            }
        }
        public string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, new string[] { TableName, Name }, baseSchemaName, true);
        }

        private void UpdateSelectable()
        {
            if (_owner != null)
            {
                return;
            }

            _owner = Context.Selectables[SchemaName, _tableName];
        }

        internal Column Backup(Selectable owner)
        {
            _backup = new Column(Table, this);
            if (Sequence != null)
            {
                _backup.Sequence = Sequence.Backup(this);
            }
            return _backup;
        }

        public override bool HasBackup()
        {
            return false;
        }

        public override void Backup(bool force)
        {
            //Backup(Table);
            throw new NotImplementedException();
        }
        protected internal void RestoreFrom(Column backup)
        {
            //Schema = backup.Schema;
            //Index = backup.Index;
            //TableName = backup.TableName;
            Name = backup.Name;
            BaseType = backup.BaseType;
            DataLength = backup.DataLength;
            Precision = backup.Precision;
            WithTimeZone = backup.WithTimeZone;
            IsSupportedType = backup.IsSupportedType;
            DataType = backup.DataType;
            ValueType = backup.ValueType;
            DefaultValue = backup.DefaultValue;
            NotNull = backup.NotNull;
            HiddenLevel = backup.HiddenLevel;
        }
        public override void Restore()
        {
            if (_backup != null)
            {
                RestoreFrom(_backup);
            }
            Comment.Restore();
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Column c = (Column)obj;
            return BaseType == c.BaseType
                && DataLength == c.DataLength
                && Precision == c.Precision
                && WithTimeZone == c.WithTimeZone
                && DataType == c.DataType
                && DefaultValue == c.DefaultValue
                && NotNull == c.NotNull
                && CommentText == c.CommentText;
        }
        public override bool IsModified
        {
            get
            {
                return (_backup != null) && !ContentEquals(_backup);
            }
        }

        void ICommentable.OnCommentChanged(CommentChangedEventArgs e)
        {
            OnPropertyChanged("CommentText");
        }

        protected void OnCommentChanged(CommentChangedEventArgs e)
        {
            OnPropertyChanged("CommentText");
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
            return FullIdentifier;
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
                InvalidateIdentifier();
                _owner?.Columns?.ColumnNameChanged(this);
                OnPropertyChanged("Name");
            }
        }
        private string _baseType;

        public string BaseType
        {
            get
            {
                return _baseType;
            }
            set
            {
                if (_baseType == value)
                {
                    return;
                }
                _baseType = value;
                OnPropertyChanged("BaseType");
            }
        }
        private int? _dataLength;
        public int? DataLength
        {
            get
            {
                return _dataLength;
            }
            set
            {
                if (_dataLength == value)
                {
                    return;
                }
                _dataLength = value;
                OnPropertyChanged("DataLength");
            }
        }
        private int? _precision;
        public int? Precision
        {
            get
            {
                return _precision;
            }
            set
            {
                if (_precision == value)
                {
                    return;
                }
                _precision = value;
                OnPropertyChanged("Precision");
            }
        }
        private bool? _withTimeZone;
        public bool? WithTimeZone
        {
            get
            {
                return _withTimeZone;
            }
            set
            {
                if (_withTimeZone == value)
                {
                    return;
                }
                _withTimeZone = value;
                OnPropertyChanged("WithTimeZone");
            }
        }

        public bool _isSupportedType;
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
                _dataType = value;
                OnPropertyChanged("DataType");
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
                //BecomeModifiedColumn();
                _defaultValue = value;
                OnPropertyChanged("DefaultValue");
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
                _notNull = value;
                OnPropertyChanged("NotNull");
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
                _comment = value;
                OnCommentChanged(new CommentChangedEventArgs(_comment));
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

        private Sequence _sequence;

        public Sequence Sequence
        {
            get
            {
                return _sequence;
            }
            set
            {
                _sequence = value;
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
            Context = context;
            Schema = context.RequireSchema(schema);
        }
        internal Column(Selectable owner, Column basedOn): base(null)
        {
            _owner = owner;
            Schema = basedOn.Schema;
            Index = basedOn.Index;
            TableName = basedOn.TableName;
            Name = basedOn.Name;
            BaseType = basedOn.BaseType;
            DataLength = basedOn.DataLength;
            Precision = basedOn.Precision;
            WithTimeZone = basedOn.WithTimeZone;
            IsSupportedType = basedOn.IsSupportedType;
            DataType = basedOn.DataType;
            ValueType = basedOn.ValueType;
            DefaultValue = basedOn.DefaultValue;
            NotNull = basedOn.NotNull;
            Comment = basedOn.Comment;
            HiddenLevel = basedOn.HiddenLevel;
        }
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                if (Schema != null)
                {
                    Schema.Columns.Remove(this);
                }
                Comment?.Dispose();
            }
            base.Dispose(disposing);
        }
        public override void Release()
        {
            base.Release();
            if (Schema != null)
            {
                Schema.Columns.Invalidate();
            }
            Comment?.Release();
        }
    }

    public sealed class ColumnCollection : IList<Column>, IList
    {
        private Selectable _owner;
        //private List<Column>[] _list = null;
        private List<Column> _list = null;
        private Column[] _hiddenColumns = null;
        private Column[] _allColumns = null;
        //private Dictionary<string, Column>[] _nameToColumn = null;
        private Dictionary<string, Column> _nameToColumn = null;

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
            //_list = new List<Column>[] { new List<Column>(), new List<Column>() };
            _list = new List<Column>();
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
                _list.Add(c);
                //if ((c.Generation & Column.ColumnGeneration.New) != 0)
                //{
                //    _list[0].Add(c);
                //}
                //if ((c.Generation & Column.ColumnGeneration.Old) != 0)
                //{
                //    _list[1].Add(c);
                //}
            }
            //_list[0].Sort();
            //_list[1].Sort();
            _list.Sort();
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
            //l.AddRange(_list[0]);
            l.AddRange(_list);
            l.Sort();
            _allColumns = l.ToArray();
        }
        private void RequireNameToColumn()
        {
            if (_nameToColumn != null)
            {
                return;
            }
            //_nameToColumn = new Dictionary<string, Column>[] { new Dictionary<string, Column>(), new Dictionary<string, Column>() };
            _nameToColumn = new Dictionary<string, Column>();
            RequireItems();
            //foreach (Column c in _list[0])
            //{
            //    _nameToColumn[0].Add(c.Name, c);
            //}
            //foreach (Column c in _list[1])
            //{
            //    _nameToColumn[1].Add(c.Name, c);
            //}
            foreach (Column c in _list)
            {
                _nameToColumn.Add(c.Name, c);
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

        public Column[] GetVisibleColumns(HiddenLevel visibleLevel)
        {
            List<Column> l = new List<Column>();
            foreach (Column c in AllColumns)
            {
                if (c.HiddenLevel <= visibleLevel)
                {
                    l.Add(c);
                }
            }
            return l.ToArray();
        }

        public Column this[int index]
        {
            get
            {
                RequireItems();
                //return _list[0][index];
                return _list[index];
            }
        }
        public Column this[int index, bool isOld]
        {
            get
            {
                RequireItems();
                //return _list[isOld ? 1 : 0][index];
                Column c = _list[index];
                if (isOld && c._backup != null)
                {
                    c = c._backup;
                }
                return c;
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
                //if (!_nameToColumn[isOld ? 1 : 0].TryGetValue(name, out ret))
                //{
                //    return null;
                //}
                if (!_nameToColumn.TryGetValue(name, out ret))
                {
                    return null;
                }
                if (isOld && ret._backup != null)
                {
                    ret = ret._backup;
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
                //return _list[0].Count;
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
                //return ((IList)_list[0]).SyncRoot;
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                //return ((IList)_list[0]).IsSynchronized;
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                RequireItems();
                //return ((IList)_list[0])[index];
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                //((IList)_list[0])[index] = value;
                ((IList)_list)[index] = value;
            }
        }

        Column IList<Column>.this[int index]
        {
            get
            {
                RequireItems();
                //return _list[0][index];
                return _list[index];
            }

            set
            {
                RequireItems();
                //_list[0][index] = value;
                _list[index] = value;
            }
        }

        public void Add(Column item)
        {
            if (!ColumnNameChanging(null, item.Name))
            {
                throw new ColumnNameException(item.Name);
            }
            RequireItems();
            //if ((item.Generation & Column.ColumnGeneration.New) != 0)
            //{
            //    _list[0].Add(item);
            //}
            //if ((item.Generation & Column.ColumnGeneration.Old) != 0)
            //{
            //    _list[1].Add(item);
            //}
            _list.Add(item);
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
            //if ((item.Generation & Column.ColumnGeneration.New) != 0)
            //{
            //    ret = ((IList)_list).Add(item);
            //}
            //if ((item.Generation & Column.ColumnGeneration.Old) != 0)
            //{
            //    _list[1].Add(item);
            //}
            ret = ((IList)_list).Add(item);
            _nameToColumn = null;
            return ret;
        }

        public void Clear()
        {
            _list.Clear();
            //if (_list != null)
            //{
            //    _list[0]?.Clear();
            //}
            _nameToColumn = null;
        }

        //public void Clear(bool isOld)
        //{
        //    if (_list != null)
        //    {
        //        _list[isOld ? 1 : 0]?.Clear();
        //    }
        //    _nameToColumn = null;
        //}

        public bool Contains(Column item)
        {
            RequireItems();
            //return _list[0].Contains(item);
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            //return ((IList)_list[0]).Contains(value);
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Column[] array, int arrayIndex)
        {
            RequireItems();
            //_list[0].CopyTo(array, arrayIndex);
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            //((IList)_list[0]).CopyTo(array, index);
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<Column> GetEnumerator()
        {
            RequireItems();
            //return _list[0].GetEnumerator();
            return _list.GetEnumerator();
        }

        public bool Remove(Column item)
        {
            RequireItems();
            //bool ret = _list[0].Remove(item);
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToColumn = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            //bool ret = _list[0].Remove(value as Column);
            bool ret = _list.Remove(value as Column);
            if (ret)
            {
                _nameToColumn = null;
            }
        }

        //public bool Remove(Column item, bool isOld)
        //{
        //    RequireItems();
        //    bool ret = _list[isOld ? 1 : 0].Remove(item);
        //    if (ret)
        //    {
        //        _nameToColumn = null;
        //    }
        //    return ret;
        //}

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            //return _list[0].GetEnumerator();
            return _list.GetEnumerator();
        }

        public int IndexOf(Column item)
        {
            //return _list[0].IndexOf(item);
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            //return ((IList)_list[0]).IndexOf(value);
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, Column item)
        {
            //_list[0].Insert(index, item);
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            //((IList)_list[0]).Insert(index, value);
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            //_list[0].RemoveAt(index);
            _list.RemoveAt(index);
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
        public class ColumnSequences : IEnumerable<Sequence>
        {
            internal class ColumnSequenceEnumerator : IEnumerator<Sequence>
            {
                private bool disposedValue;
                private Selectable _owner;
                private int _columnIndex = -1;
                public Sequence Current
                {
                    get
                    {
                        return _owner.Columns[_columnIndex].Sequence;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return _owner.Columns[_columnIndex].Sequence;
                    }
                }

                public bool MoveNext()
                {
                    _columnIndex++;
                    for (int n = _owner.Columns.Count; _columnIndex < n; _columnIndex++)
                    {
                        if (_owner.Columns[_columnIndex].Sequence != null)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    _columnIndex = -1;
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                            _columnIndex = -1;
                        }

                        // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                        // TODO: 大きなフィールドを null に設定します
                        disposedValue = true;
                    }
                }

                // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
                // ~ColumnSequenceEnumerator()
                // {
                //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
                //     Dispose(disposing: false);
                // }

                public void Dispose()
                {
                    // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }

                internal ColumnSequenceEnumerator(Selectable owner)
                {
                    _owner = owner;
                }
            }

            private Selectable _owner;

            public IEnumerator<Sequence> GetEnumerator()
            {
                return new ColumnSequenceEnumerator(_owner);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ColumnSequenceEnumerator(_owner);
            }
            internal ColumnSequences(Selectable owner)
            {
                _owner = owner;
            }
        }

        public ColumnCollection Columns { get; private set; }
        public IndexCollection Indexes { get; private set; }
        private ColumnSequences _columnSequences;
        public IEnumerable<Sequence> Sequences
        {
            get
            {
                return _columnSequences;
            }
        }

        internal Selectable(Db2SourceContext context, string owner, string schema, string tableName) : base(context, owner, schema, tableName, Schema.CollectionIndex.Objects)
        {
            Columns = new ColumnCollection(this);
            Indexes = new IndexCollection(this);
            _columnSequences = new ColumnSequences(this);
        }

        internal Selectable(NamedCollection owner, Selectable basedOn) : base(owner, basedOn)
        {
            Columns = new ColumnCollection(this);
            Indexes = new IndexCollection(this);
            _columnSequences = new ColumnSequences(this);
            basedOn.BackupColumns(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                foreach (Column c in Columns)
                {
                    c.Dispose();
                }
                Comment?.Dispose();
            }
            _columnSequences = null;
            base.Dispose(disposing);
        }
        public override void Release()
        {
            base.Release();
            foreach (Column c in Columns)
            {
                c.Release();
            }
            Comment?.Release();
        }

        public override void InvalidateColumns()
        {
            Columns.Invalidate();
        }

        public void InvalidateIndexes()
        {
            Indexes.Invalidate();
        }

        internal Selectable _backup;

        protected void BackupColumns(Selectable destination)
        {
            foreach (Column c in Columns)
            {
                destination.Columns.Add(c.Backup(destination));
            }
        }

        protected void RestoreFrom(Selectable backup)
        {
            foreach (Column c in Columns)
            {
                c.RestoreFrom(c._backup);
            }
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Selectable s = (Selectable)obj;
            if (Columns.Count != s.Columns.Count)
            {
                return false;
            }
            for (int i = 0; i < Columns.Count; i++)
            {
                if (!Columns[i].ContentEquals(s.Columns[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool IsModified
        {
            get
            {
                return (_backup != null) && !ContentEquals(_backup);
            }
        }
        public string GetColumnsSQL(string alias, IEnumerable<Column> columns)
        {
            StringBuilder buf = new StringBuilder();
            string delim = "  ";
            int w = 0;
            string a = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            foreach (Column c in columns)
            {
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

        public string GetColumnsSQL(string alias, HiddenLevel visibleLevel)
        {
            return GetColumnsSQL(alias, Columns.GetVisibleColumns(visibleLevel));
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
            int whereOffset;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder bufW = new StringBuilder();
            bool needIndent = false;
            foreach (string s in where)
            {
                if (needIndent)
                {
                    bufW.Append("  ");
                }
                bufW.AppendLine(s);
                needIndent = true;
            }
            return GetSelectSQL(alias, bufW.ToString(), orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
        {
            return GetSelectSQL(alias, where, StrUtil.DelimitedText(orderBy, ", "), limit, visibleLevel);
        }
        public string GetSelectSQL(string alias, string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder bufW = new StringBuilder();
            bool needIndent = false;
            foreach (string s in where)
            {
                if (needIndent)
                {
                    bufW.Append("  ");
                }
                bufW.AppendLine(s);
                needIndent = true;
            }
            return GetSelectSQL(alias, bufW.ToString().TrimEnd(), StrUtil.DelimitedText(orderBy, ", "), limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string alias, string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset;
            return GetSelectSQL(alias, where, orderBy, limit, visibleLevel, out whereOffset);
        }

        public long GetRecordCount(IDbConnection connection)
        {
            string sql = string.Format("select count(1) from {0}", EscapedIdentifier(null));
            using (IDbCommand cmd = Context.GetSqlCommand(sql, null, connection))
            {
                object o = cmd.ExecuteScalar();
                return Convert.ToInt64(o);
            }
        }
    }
}
