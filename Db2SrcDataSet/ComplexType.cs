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


        public override void Backup()
        {
            if (_backup != null)
            {
                return;
            }
            _backup = new ComplexType(this);
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }

        internal ComplexType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal ComplexType(ComplexType basedOn) : base(basedOn) { }
    }
}
