using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Db2Source
{
    public enum ConstraintType
    {
        Primary,
        Unique,
        ForeignKey,
        Check
    }

    public interface IConstraint : IDb2SourceInfo
    {
        string Name { get; }
        ConstraintType ConstraintType { get; }
        string TableName { get; set; }
        Table Table { get; set; }
    }

    public abstract class Constraint : SchemaObject, IComparable, IConstraint
    {
        internal Constraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isNoName, bool deferrable, bool deferred) : base(context, owner, schema, name, Schema.CollectionIndex.Constraints)
        {
            _tableSchema = tableSchema;
            _tableName = tableName;
            _table = null;
            _isTemporaryName = isNoName;
            _deferrable = deferrable;
            _deferred = deferred;
        }

        public Constraint(Table owner, Constraint basedOn) : base(null, basedOn)
        {
            _tableSchema = basedOn.TableSchema;
            _tableName = basedOn.TableName;
            _table = owner;
            _isTemporaryName = basedOn.IsTemporaryName;
            _deferrable = basedOn.Deferrable;
            _deferred = basedOn.Deferred;
        }
        protected override string GetIdentifier()
        {
            //return Table?.Identifier + "+" + Name;
            return base.GetIdentifier();
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
        private bool _deferrable;
        private bool _deferred;

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
            InvalidateIdentifier();
            InvalidateIsTemporaryName();
        }
        public void InvalidateTable()
        {
            _table = null;
            InvalidateIdentifier();
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
        public bool Deferrable
        {
            get
            {
                return _deferrable;
            }
            set
            {
                if (_deferrable == value)
                {
                    return;
                }
                _deferrable = value;
                InvalidateTable();
            }
        }
        public bool Deferred
        {
            get
            {
                return _deferred;
            }
            set
            {
                if (_deferred == value)
                {
                    return;
                }
                _deferred = value;
                InvalidateTable();
            }
        }

        internal Constraint _backup;

        protected internal abstract Constraint Backup(Table owner, bool force);

        public override void Backup(bool force)
        {
            throw new NotImplementedException();
        }
        protected internal void RestoreFrom(Table owner, Constraint backup)
        {
            base.RestoreFrom(backup);
            _tableSchema = owner.SchemaName;
            _tableName = owner.Name;
            _table = owner;
            _isTemporaryName = backup.IsTemporaryName;
            _deferrable = backup.Deferrable;
            _deferred = backup.Deferred;
        }
        //public override void Restore()
        //{
        //    RestoreFrom(_backup);
        //}
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Constraint c = (Constraint)obj;
            return TableSchema == c.TableSchema
                && TableName == c.TableName
                && IsTemporaryName == c.IsTemporaryName
                && Deferrable == c.Deferrable
                && Deferred == c.Deferred;
        }
        public override bool IsModified
        {
            get
            {
                return (_backup != null) && !ContentEquals(_backup);
            }
        }

        protected override Comment NewComment(string commentText)
        {
            return new ConstraintComment(Context, SchemaName, TableName, Name, commentText, false);
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

    public abstract class ColumnsConstraint : Constraint
    {
        public string[] Columns { get; set; }
        internal ColumnsConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isNoName, bool deferrable, bool deferred)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName, deferrable, deferred) { }
        public ColumnsConstraint(Table owner, ColumnsConstraint basedOn) : base(owner, basedOn)
        {
            Columns = (string[])basedOn.Columns.Clone();
        }
    }

    public partial class KeyConstraint : ColumnsConstraint
    {
        private bool _isPrimary = false;
        public override ConstraintType ConstraintType { get { return _isPrimary ? ConstraintType.Primary : ConstraintType.Unique; } }
        public string[] ExtraInfo { get; set; }

        public KeyConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, bool isPrimary, bool isNoName, bool deferrable, bool deferred)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName, deferrable, deferred)
        {
            _isPrimary = isPrimary;
        }
        public KeyConstraint(Table owner, KeyConstraint basedOn): base(owner, basedOn)
        {
            _isPrimary = basedOn._isPrimary;
            ExtraInfo = (string[])basedOn.ExtraInfo.Clone();
        }
        public override bool HasBackup()
        {
            return _backup != null;
        }

        protected internal override Constraint Backup(Table owner, bool force)
        {
            if (!force && _backup != null)
            {
                return _backup;
            }
            _backup = new KeyConstraint(owner, this);
            return _backup;
        }
        internal void RestoreFrom(Table owner, KeyConstraint backup)
        {
            base.RestoreFrom(owner, backup);
            _isPrimary = backup._isPrimary;
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(Table, (KeyConstraint)_backup);
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            KeyConstraint c = (KeyConstraint)obj;
            return _isPrimary == c._isPrimary;
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
    public partial class ForeignKeyConstraint : ColumnsConstraint
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
                    return ReferenceConstraint?.TableName;
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

        public string Description
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                string prefix = "(";
                foreach (string c in Columns)
                {
                    buf.Append(prefix);
                    buf.Append(c);
                    prefix = ", ";
                }
                buf.Append(") => ");
                if (!string.IsNullOrEmpty(ReferenceSchemaName) && ReferenceSchemaName != SchemaName)
                {
                    buf.Append(ReferenceSchemaName);
                    buf.Append('.');
                }
                buf.Append(ReferenceTableName);
                if (ReferenceConstraint == null || ReferenceConstraint.ConstraintType != ConstraintType.Primary)
                {
                    prefix = "(";
                    foreach (string c in RefColumns)
                    {
                        buf.Append(prefix);
                        buf.Append(c);
                        prefix = ", ";
                    }
                    buf.Append(")");
                }
                return buf.ToString();
            }
        }

        public string FullDescription
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(TableName);
                string prefix = "(";
                foreach (string c in Columns)
                {
                    buf.Append(prefix);
                    buf.Append(c);
                    prefix = ", ";
                }
                buf.Append(") => ");
                if (!string.IsNullOrEmpty(ReferenceSchemaName) && ReferenceSchemaName != SchemaName)
                {
                    buf.Append(ReferenceSchemaName);
                    buf.Append('.');
                }
                buf.Append(ReferenceTableName);
                if (ReferenceConstraint == null || ReferenceConstraint.ConstraintType != ConstraintType.Primary)
                {
                    prefix = "(";
                    foreach (string c in RefColumns)
                    {
                        buf.Append(prefix);
                        buf.Append(c);
                        prefix = ", ";
                    }
                    buf.Append(")");
                }
                return buf.ToString();
            }
        }

        public ForeignKeyConstraint(Db2SourceContext context, string owner, string schema, string name, string tableSchema, string tableName, string refSchema, string refConstraint, ForeignKeyRule updateRule, ForeignKeyRule deleteRule, bool isNoName, bool deferrable, bool deferred)
            : base(context, owner, schema, name, tableSchema, tableName, isNoName, deferrable, deferred)
        {
            ReferenceSchemaName = refSchema;
            ReferenceConstraintName = refConstraint;
            UpdateRule = updateRule;
            DeleteRule = deleteRule;
        }

        public ForeignKeyConstraint(Table owner, ForeignKeyConstraint basedOn) : base(owner, basedOn)
        {
            ReferenceSchemaName = basedOn.ReferenceSchemaName;
            ReferenceConstraintName = basedOn.ReferenceConstraintName;
            UpdateRule = basedOn.UpdateRule;
            DeleteRule = basedOn.DeleteRule;
        }

        public override bool HasBackup()
        {
            return _backup != null;
        }

        protected internal override Constraint Backup(Table owner, bool force)
        {
            if (!force && _backup != null)
            {
                return _backup;
            }
            _backup = new ForeignKeyConstraint(owner, this);
            return _backup;
        }

        internal void RestoreFrom(Table owner, ForeignKeyConstraint backup)
        {
            base.RestoreFrom(owner, backup);
            ReferenceSchemaName = backup.ReferenceSchemaName;
            ReferenceConstraintName = backup.ReferenceConstraintName;
            UpdateRule = backup.UpdateRule;
            DeleteRule = backup.DeleteRule;
        }

        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(Table, (ForeignKeyConstraint)_backup);
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            ForeignKeyConstraint c = (ForeignKeyConstraint)obj;
            return ReferenceSchemaName == c.ReferenceSchemaName
                && ReferenceConstraintName == c.ReferenceConstraintName
                && UpdateRule == c.UpdateRule
                && DeleteRule == c.DeleteRule;
        }
    }

    public partial class CheckConstraint : Constraint
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
            : base(context, owner, schema, name, tableSchema, tableName, isNoName, false, false)
        {
            Condition = condition;
        }

        public CheckConstraint(Table owner, CheckConstraint basedOn) : base(owner, basedOn)
        {
            Condition = basedOn.Condition;
        }

        public override bool HasBackup()
        {
            return _backup != null;
        }

        protected internal override Constraint Backup(Table owner, bool force)
        {
            if (!force && _backup != null)
            {
                return _backup;
            }
            _backup = new CheckConstraint(owner, this);
            return _backup;
        }

        internal void RestoreFrom(Table owner, CheckConstraint backup)
        {
            base.RestoreFrom(owner, backup);
            Condition = backup.Condition;
        }

        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(Table, (CheckConstraint)_backup);
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            CheckConstraint c = (CheckConstraint)obj;
            return Condition == c.Condition;
        }
    }

    public sealed class ConstraintCollection : IList<Constraint>, IList
    {
        private readonly Selectable _owner;
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

        public void Dispose()
        {
            if (_list == null)
            {
                return;
            }
            Constraint[] l = _list.ToArray();
            foreach (Constraint c in l)
            {
                c.Dispose();
            }
        }

        public void Release()
        {
            if (_list == null)
            {
                return;
            }
            Constraint[] l = _list.ToArray();
            foreach (Constraint c in l)
            {
                c.Release();
            }
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
            int ret = ((IList)_list).Add(item);
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
}
