using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
namespace Db2Source
{
    public class Index: SchemaObject
    {
        private Selectable _table;
        private string _tableSchema;
        private string _tableName;
        //private string[] _columns;
        private int _index;
        public int Index_
        {
            get
            {
                UpdateIndex_();
                return _index;
            }
            internal set
            {
                _index = value;
            }
        }
        public bool IsUnique { get; set; }
        public bool IsImplicit { get; set; }
        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context?.Objects[TableSchema, TableName] as Selectable;
            _table?.InvalidateIndexes();
        }
        private void UpdateIndex_()
        {
            Table?.Indexes?.RequireItems();
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public Selectable Table
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
                string p1 = null;
                string p2 = null;
                if (_tableSchema != _table.SchemaName)
                {
                    _tableSchema = _table.SchemaName;
                    p1 = "TableSchema";
                }
                if (_tableName != _table.Name)
                {
                    _tableName = _table.Name;
                    p2 = "TableName";
                }
                if (p1 != null)
                {
                    OnPropertyChanged(p1);
                }
                if (p2 != null)
                {
                    OnPropertyChanged(p2);
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
                _tableSchema = value;
                InvalidateTable();
                OnPropertyChanged("TableSchema");
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
                OnPropertyChanged("TableName");
            }
        }
        public string IndexType { get; set; }
        public string[] Columns { get; set; }
        public string ColumnText
        {
            get
            {
                return StrUtil.DelimitedText(Columns, ", ", "(", ")");
            }
        }
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

        internal Index _backup;
        public override bool HasBackup()
        {
            return _backup != null;
        }

        public override void Backup(bool force)
        {
            if (!force && _backup != null)
            {
                return;
            }
            _backup = new Index(null, this);
        }

        protected internal void RestoreFrom(Index backup)
        {
            base.RestoreFrom(backup);
            _tableSchema = backup.TableSchema;
            _tableName = backup.TableName;
            IsUnique = backup.IsUnique;
            IsImplicit = backup.IsImplicit;
            IndexType = backup.IndexType;
            Columns = (string[])backup.Columns.Clone();
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Index idx = (Index)obj;
            return _tableSchema == idx.TableSchema
                && _tableName == idx.TableName
                && IsUnique == idx.IsUnique
                && IsImplicit == idx.IsImplicit
                && IndexType == idx.IndexType
                && ArrayEquals<string>(Columns, idx.Columns);
        }
        public override bool IsModified
        {
            get
            {
                return (_backup != null) && ContentEquals(_backup);
            }
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
        public Index(NamedCollection owner, Index basedOn): base(owner, basedOn)
        {
            _tableSchema = basedOn.TableSchema;
            _tableName = basedOn.TableName;
            IsUnique = basedOn.IsUnique;
            IsImplicit = basedOn.IsImplicit;
            IndexType = basedOn.IndexType;
            Columns = (string[])basedOn.Columns.Clone();
        }
    }
    public sealed class IndexCollection : IList<Index>, IList
    {
        private Selectable _owner;
        private List<Index> _list = null;
        private Dictionary<string, Index> _nameToIndex = null;

        public IndexCollection(Selectable owner)
        {
            _owner = owner;
        }

        private object _updatingLock = new object();
        private bool _updating;
        public void Invalidate()
        {
            if (_updating)
            {
                return;
            }
            _list = null;
            _nameToIndex = null;
        }

        internal void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            lock (_updatingLock)
            {
                _updating = true;
                try
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
                    for (int i = 0, n = _list.Count; i < n; i++)
                    {
                        _list[i].Index_ = i + 1;
                    }
                }
                finally
                {
                    _updating = false;
                }
            }
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
            int ret = ((IList)_list).Add(item);
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
}
