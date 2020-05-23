namespace Db2Source
{
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

    public class BasicType : Type_
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

    public class EnumType : Type_
    {
        public string[] Labels { get; set; }
        internal EnumType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }

    public class RangeType : Type_
    {
        public string Subtype { get; set; }
        public string SubtypeOpClass { get; set; }
        public string Collation { get; set; }
        public string CanonicalFunction { get; set; }
        public string SubtypeDiff { get; set; }
        internal RangeType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
}
