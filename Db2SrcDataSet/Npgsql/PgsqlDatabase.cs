using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlDatabase : Database
    {
        public PgsqlSettingCollection Settings { get; } = new PgsqlSettingCollection();

        public PgsqlDatabase(Db2SourceContext context, string objectName) : base(context, objectName) { }
    }
}
