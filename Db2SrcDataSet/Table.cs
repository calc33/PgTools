using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Db2Source
{

    public sealed class IndexCollection : IList<Index>, IList
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
        public ForeignKeyConstraint[] GetForeignKeysForColumn(string columnName)
        {
            List<ForeignKeyConstraint> l = new List<ForeignKeyConstraint>();
            foreach (ForeignKeyConstraint c in ReferTo)
            {
                foreach (string col in c.Columns)
                {
                    if (col == columnName)
                    {
                        l.Add(c);
                        break;
                    }
                }
            }
            return l.ToArray();
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
}
