using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class LockList : SchemaObject
    {
        public ILock[] GetLocks()
        {
            return Context.GetLocks(true);
        }

        public ILock[] GetLocks(IDbConnection connection)
        {
            return Context.GetLocks(true, connection);
        }

        public LockList(Db2SourceContext context, string objectName) : base(context, string.Empty, string.Empty, objectName, NamespaceIndex.Locks)
        {
        }

        public override NamespaceIndex GetCollectionIndex()
        {
            return NamespaceIndex.Locks;
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
