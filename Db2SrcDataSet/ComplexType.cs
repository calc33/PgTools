using System.ComponentModel;

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

        //public TypeReferenceCollection ReferFrom { get; } = new TypeReferenceCollection();


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
            _backup = new ComplexType(null, this);
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
        public ComplexType(NamedCollection owner, ComplexType basedOn) : base(owner, basedOn) { }
    }
}
