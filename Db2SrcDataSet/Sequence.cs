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
        public string OwnedSchema { get; set; }
        public string OwnedTable { get; set; }
        public string OwnedColumn { get; set; }

        internal Sequence _backup;
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
            OwnedSchema = backup.OwnedSchema;
            OwnedTable = backup.OwnedTable;
            OwnedColumn = backup.OwnedColumn;
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
            Sequence seq = (Sequence)obj;
            return StartValue == seq.StartValue
                && MinValue == seq.MinValue
                && MaxValue == seq.MaxValue
                && Increment == seq.Increment
                && IsCycled == seq.IsCycled
                && Cache == seq.Cache
                && OwnedSchema == seq.OwnedSchema
                && OwnedTable == seq.OwnedTable
                && OwnedColumn == seq.OwnedColumn;
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
            OwnedSchema = basedOn.OwnedSchema;
            OwnedTable = basedOn.OwnedTable;
            OwnedColumn = basedOn.OwnedColumn;
        }
    }
}
