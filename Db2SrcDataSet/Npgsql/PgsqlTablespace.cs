using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlTablespace: Tablespace
    {
        public uint oid { get; set; }
        public string Owner { get; set; }
        public string[] Options { get; set; }

        public PgsqlTablespace(NamedCollection<Tablespace> owner) : base(owner)
        {
            if (owner != null)
            {
                owner.Add(this);
            }
        }

        public override object Clone()
        {
            PgsqlTablespace ret = (PgsqlTablespace)base.Clone();
            string[] opt = ret.Options;
            if (opt != null)
            {
                ret.Options = new string[opt.Length];
                Array.Copy(opt, ret.Options, opt.Length);
            }
            return ret;
        }
    }
}
