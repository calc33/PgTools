namespace Db2Source
{
    public class ComplexType: Selectable
    {
        public override string GetSqlType()
        {
            return "TYPE";
        }
        public override string GetExportFolderName()
        {
            return "Type";
        }

        public TypeReferenceCollection ReferFrom { get; } = new TypeReferenceCollection();

        internal ComplexType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
}
