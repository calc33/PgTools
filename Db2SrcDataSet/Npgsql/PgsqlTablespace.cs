using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlTablespace: Tablespace
    {
        public uint Oid { get; set; }
        public string Owner { get; set; }
        public string[] Options { get; set; }

        public override void Backup()
        {
            _backup = new PgsqlTablespace(this);
        }

        //public override void Restore()
        //{
        //    base.Restore();
        //}

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            PgsqlTablespace ts = (PgsqlTablespace)obj;
            return Oid == ts.Oid
                && Owner == ts.Owner
                && ArrayEquals(Options, ts.Options);
        }

        public PgsqlTablespace(NamedCollection<Tablespace> owner) : base(owner) { }
        internal PgsqlTablespace(PgsqlTablespace basedOn) : base(basedOn)
        {
            Oid = basedOn.Oid;
            Owner = basedOn.Owner;
            Options = (string[])basedOn.Options.Clone();
        }

    }
}
