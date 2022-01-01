using System;

namespace Db2Source
{
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
        public string OwnedSchemaName { get; set; }
        public string OwnedTableName { get; set; }
        public string OwnedColumnName { get; set; }

        private WeakReference<Column> _ownedColumn;
        private Column GetOwnedColumn()
        {
            Column col;
            if (_ownedColumn != null && _ownedColumn.TryGetTarget(out col))
            {
                return col;
            }
            col = Context.Selectables[OwnedSchemaName, OwnedTableName]?.Columns[OwnedColumnName];
            if (col == null)
            {
                return null;
            }
            _ownedColumn = new WeakReference<Column>(col);
            return col;
        }
        public Column OwnedColumn
        {
            get
            {
                return GetOwnedColumn();
            }
        }

        internal Sequence _backup;
        protected internal Sequence Backup(Column owner)
        {
            _backup = new Sequence(this);
            if (owner != null)
            {
                _backup._ownedColumn = new WeakReference<Column>(owner);
            }
            return _backup;
        }
        public override void Backup()
        {
            _backup = new Sequence(this);
        }
        protected internal void RestoreFrom(Sequence backup)
        {
            base.RestoreFrom(backup);
            StartValue = backup.StartValue;
            MinValue = backup.MinValue;
            MaxValue = backup.MaxValue;
            Increment = backup.Increment;
            IsCycled = backup.IsCycled;
            Cache = backup.Cache;
            OwnedSchemaName = backup.OwnedSchemaName;
            OwnedTableName = backup.OwnedTableName;
            OwnedColumnName = backup.OwnedColumnName;
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }

        public override void Release()
        {
            base.Release();
            Column col;
            if (_ownedColumn != null && _ownedColumn.TryGetTarget(out col) && col != null)
            {
                col.Sequence = null;
            }
            _ownedColumn = null;
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Sequence seq = (Sequence)obj;
            return StartValue == seq.StartValue
                && MinValue == seq.MinValue
                && MaxValue == seq.MaxValue
                && Increment == seq.Increment
                && IsCycled == seq.IsCycled
                && Cache == seq.Cache
                && OwnedSchemaName == seq.OwnedSchemaName
                && OwnedTableName == seq.OwnedTableName
                && OwnedColumnName == seq.OwnedColumnName;
        }

        internal Sequence(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }

        internal Sequence(Sequence basedOn) : base(basedOn)
        {
            StartValue = basedOn.StartValue;
            MinValue = basedOn.MinValue;
            MaxValue = basedOn.MaxValue;
            Increment = basedOn.Increment;
            IsCycled = basedOn.IsCycled;
            Cache = basedOn.Cache;
            OwnedSchemaName = basedOn.OwnedSchemaName;
            OwnedTableName = basedOn.OwnedTableName;
            OwnedColumnName = basedOn.OwnedColumnName;
        }
    }
}
