using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlUser: User
    {
        public uint oid { get; set; }
        public bool CanCreateDb { get; set; }
        public bool IsSuperUser { get; set; }
        public bool Replication { get; set; }
        public bool BypassRowLevelSecurity { get; set; }
        public string[] Config { get; set; }

        public PgsqlUser(NamedCollection owner) : base(owner) { }

        public override object Clone()
        {
            PgsqlUser ret = (PgsqlUser)base.Clone();
            string[] cfg = ret.Config;
            if (cfg != null)
            {
                ret.Config = new string[cfg.Length];
                Array.Copy(cfg, ret.Config, cfg.Length);
            }
            return ret;
        }
    }
}
