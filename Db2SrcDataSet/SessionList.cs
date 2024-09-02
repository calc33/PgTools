using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class SessionList: SchemaObject
    {
        public List<ISession> GetSessions()
        {
            return new List<ISession>(Context.GetSessions());
        }
        public SessionList(Db2SourceContext context, string objectName) : base(context, string.Empty, string.Empty, objectName, Schema.CollectionIndex.None)
        {
            Schema = null;
        }

        protected override int GetIdentifierDepth()
        {
            return 1;
        }

        public override bool HasBackup()
        {
            return false;
        }

        public override void Backup(bool force) { }
        public override string GetExportFolderName()
        {
            throw new NotImplementedException();
        }

        public override string GetSqlType()
        {
            throw new NotImplementedException();
        }

        public override void Restore() { }
    }
}
