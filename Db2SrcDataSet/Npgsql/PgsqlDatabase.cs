using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlDatabase : Database
    {
        public PgsqlSettingCollection Settings { get; private set; } = new PgsqlSettingCollection();

        public override void Backup()
        {
            _backup = new PgsqlDatabase(this);
        }
        protected void RestoreFrom(PgsqlDatabase backup)
        {
            base.RestoreFrom(backup);
            Settings = (PgsqlSettingCollection)backup.Settings.Clone();
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
            PgsqlDatabase db = (PgsqlDatabase)obj;
            return Settings.ContentEquals(db.Settings);
        }
        public PgsqlDatabase(Db2SourceContext context, string objectName) : base(context, objectName) { }
        internal PgsqlDatabase(PgsqlDatabase basedOn) : base(basedOn)
        {
            Settings = (PgsqlSettingCollection)basedOn.Settings.Clone();
        }
    }
}
