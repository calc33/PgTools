using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlTablespace: Tablespace
    {
        public static readonly string[] OptionKeys = new string[] { "seq_page_cost", "random_page_cost", "effective_io_concurrency", "maintenance_io_concurrency" };
        private uint _oid;
        private string _owner;
        private string[] _options;

        public uint Oid
        {
            get { return _oid; }
            set
            {
                if (_oid == value)
                {
                    return;
                }
                _oid = value;
                OnPropertyChanged("Oid");
            }
        }
        public string Owner
        {
            get { return _owner; }
            set
            {
                if (_owner == value)
                {
                    return;
                }
                _owner = value;
                OnPropertyChanged("Owner");
            }
        }
        public string[] Options
        {
            get { return _options; }
            set
            {
                if (object.Equals(_options, value))
                {
                    return;
                }
                _options = value;
                OnPropertyChanged("Options");
            }
        }

        //public override bool HasBackup()
        //{
        //    return _backup != null;
        //}

        public override void Backup(bool force)
        {
            if (!force && _backup != null)
            {
                return;
            }
            _backup = new PgsqlTablespace(null, this);
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

        public PgsqlTablespace(NamedCollection owner) : base(owner) { }
        public PgsqlTablespace(NamedCollection owner, PgsqlTablespace basedOn) : base(owner, basedOn)
        {
            Oid = basedOn.Oid;
            Owner = basedOn.Owner;
            Options = (string[])basedOn.Options.Clone();
        }

    }
}
