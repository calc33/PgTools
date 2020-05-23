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

        internal Sequence(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }
    }
}
