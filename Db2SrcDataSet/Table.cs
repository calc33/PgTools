using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Db2Source
{

    public partial class Table: Selectable
    {
        public enum Kind
        {
            Table,
            ForeignTable
        }
        private static readonly string[] KindToSqlType = new string[] { "TABLE", "FOREIGN TABLE" };
        private Kind _tableKind = Kind.Table;
        public override string GetSqlType()
        {
            int k = (int)_tableKind;
            return KindToSqlType[(0 <= k && k < KindToSqlType.Length) ? k : 0];
        }
        public override string GetExportFolderName()
        {
            return "Table";
        }
        public ConstraintCollection Constraints { get; private set; }
        //public IndexCollection Indexes { get; private set; }
        public string TablespaceName { get; set; }
        public string[] ExtraInfo { get; set; }
        //public static Regex TemporaryNamePattern { get; set; }
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

        public bool IsForeignTable { get { return _tableKind == Kind.ForeignTable; } }
        public string ForeignServer { get; set; }
        public string[] ForeignTableOptions { get; set; }
        public bool IsPartitioned { get; set; }

        public string PartitionBound { get; set; }

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
            _backup = new Table(null, this);
        }

        protected internal void RestoreFrom(Table backup)
        {
            base.RestoreFrom(backup);
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }

        public string GetKeyConditionSQL(string alias, string prefix, int indent)
        {
            string[] cond = GetKeyConditionSQL(alias);
            string spc = Db2SourceContext.GetIndent(indent + 1);
            StringBuilder buf = new StringBuilder();
            bool needAnd = false;
            foreach (string s in cond)
            {
                if (needAnd)
                {
                    buf.Append(spc);
                    buf.Append("and ");
                }
                else
                {
                    buf.Append(prefix);
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
                return StrUtil.EmptyStringArray;
            }
            List<string> l = new List<string>();
            string a = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            string[] cols = FirstCandidateKey.Columns;
            if (cols != null)
            {
                foreach (string c in FirstCandidateKey.Columns)
                {
                    l.Add(string.Format("{0}{1} = :old_{2}", a, Context.GetEscapedIdentifier(c, true), c));
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
        public string GetUpsertSql(int indent, int charPerLine, string postfix)
        {
            return Context.GetInsertUpdateSql(this, indent, charPerLine, postfix);
        }
        public string GetMergeSql(int indent, int charPerLine, string postfix)
        {
            return Context.GetMergeSql(this, indent, charPerLine, postfix);
        }

        public string[] GetDropSql(string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return Context.GetDropSQL(this, prefix, postfix, indent, cascade, addNewline);
        }
        protected void BackupConstraints(Table destination, bool force)
        {
            foreach (Constraint c in Constraints)
            {
                destination.Constraints.Add(c.Backup(destination, force));
            }
        }

        internal Table(Db2SourceContext context, Kind kind, string owner, string schema, string tableName) : base(context, owner, schema, tableName)
        {
            _tableKind = kind;
            Constraints = new ConstraintCollection(this);
            ReferFrom = new ReferedForeignKeyCollection(this);
        }
        public Table(NamedCollection owner, Table basedOn): base(owner, basedOn)
        {
            Constraints = new ConstraintCollection(this);
            ReferFrom = new ReferedForeignKeyCollection(this);
            foreach (Constraint c in basedOn.Constraints)
            {
                Constraints.Add(c.Backup(this, true));
            }
            //Constraints
            //Indexes
            //TablespaceName
            //ExtraInfo
            //ReferFrom
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                Constraints.Dispose();
            }
            base.Dispose(disposing);
        }
        public override void Release()
        {
            base.Release();
            Constraints.Release();
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
        public ForeignKeyConstraint this[int index]
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
