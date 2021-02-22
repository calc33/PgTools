using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlUser: User
    {
        public uint Oid { get; set; }
        public bool CanLogin { get; set; } = true;
        public bool IsInherit { get; set; } = true;
        public bool CanCreateDb { get; set; }
        public bool CanCreateRole { get; set; }
        public bool IsSuperUser { get; set; }
        public bool Replication { get; set; }
        public bool BypassRowLevelSecurity { get; set; }
        public string[] Config { get; set; }
        public int ConnectionLimit { get; set; } = -1;

        public override void Backup()
        {
            _backup = new PgsqlUser(this);
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            base.Restore();
            PgsqlUser u = (PgsqlUser)_backup;
            Oid = u.Oid;
            CanLogin = u.CanLogin;
            IsInherit = u.IsInherit;
            CanCreateDb = u.CanCreateDb;
            CanCreateRole = u.CanCreateRole;
            IsSuperUser = u.IsSuperUser;
            Replication = u.Replication;
            BypassRowLevelSecurity = BypassRowLevelSecurity;
            Config = (string[])u.Config.Clone();
            ConnectionLimit = u.ConnectionLimit;
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            PgsqlUser u = (PgsqlUser)_backup;
            return Oid == u.Oid
                && CanLogin == u.CanLogin
                && IsInherit == u.IsInherit
                && CanCreateDb == u.CanCreateDb
                && CanCreateRole == u.CanCreateRole
                && IsSuperUser == u.IsSuperUser
                && Replication == u.Replication
                && BypassRowLevelSecurity == BypassRowLevelSecurity
                && ArrayEquals(Config, u.Config)
                && ConnectionLimit == u.ConnectionLimit;
        }

        public PgsqlUser(NamedCollection owner) : base(owner) { }
        internal PgsqlUser(PgsqlUser basedOn): base(basedOn)
        {
            Oid = basedOn.Oid;
            CanLogin = basedOn.CanLogin;
            IsInherit = basedOn.IsInherit;
            CanCreateDb = basedOn.CanCreateDb;
            CanCreateRole = basedOn.CanCreateRole;
            IsSuperUser = basedOn.IsSuperUser;
            Replication = basedOn.Replication;
            BypassRowLevelSecurity = BypassRowLevelSecurity;
            Config = (string[])basedOn.Config.Clone();
            ConnectionLimit = basedOn.ConnectionLimit;
        }
    }
}
