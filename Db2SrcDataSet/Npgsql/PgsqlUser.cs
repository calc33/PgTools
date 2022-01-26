using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlUser: User
    {
        private uint _oid;
        private bool _canLogin = true;
        private bool _isInherit = true;
        private bool _canCreateDb;
        private bool _canCreateRole;
        private bool _isSuperUser;
        private bool _replication;
        private bool _bypassRowLevelSecurity;
        private string[] _config;
        private int? _connectionLimit;

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
        public bool CanLogin
        {
            get { return _canLogin; }
            set
            {
                if (_canLogin == value)
                {
                    return;
                }
                _canLogin = value;
                OnPropertyChanged("CanLogin");
            }
        }
        public bool IsInherit
        {
            get { return _isInherit; }
            set
            {
                if (_isInherit == value)
                {
                    return;
                }
                _isInherit = value;
                OnPropertyChanged("IsInherit");
            }
        }
        public bool CanCreateDb
        {
            get { return _canCreateDb; }
            set
            {
                if (_canCreateDb == value)
                {
                    return;
                }
                _canCreateDb = value;
                OnPropertyChanged("CanCreateDb");
            }
        }

        public bool CanCreateRole
        {
            get { return _canCreateRole; }
            set
            {
                if (_canCreateRole == value)
                {
                    return;
                }
                _canCreateRole = value;
                OnPropertyChanged("CanCreateRole");
            }
        }
        public bool IsSuperUser
        {
            get { return _isSuperUser; }
            set
            {
                if (_isSuperUser == value)
                {
                    return;
                }
                _isSuperUser = value;
                OnPropertyChanged("IsSuperUser");
            }
        }
        public bool Replication
        {
            get { return _replication; }
            set
            {
                if (_replication == value)
                {
                    return;
                }
                _replication = value;
                OnPropertyChanged("Replication");
            }
        }
        public bool BypassRowLevelSecurity
        {
            get { return _bypassRowLevelSecurity; }
            set
            {
                if (_bypassRowLevelSecurity == value)
                {
                    return;
                }
                _bypassRowLevelSecurity = value;
                OnPropertyChanged("BypassRowLevelSecurity");
            }
        }
        public string[] Config
        {
            get { return _config; }
            set
            {
                if (object.Equals(_config, value))
                {
                    return;
                }
                _config = value;
                OnPropertyChanged("Config");
            }
        }
        public int? ConnectionLimit
        {
            get { return _connectionLimit; }
            set
            {
                if (_connectionLimit == value)
                {
                    return;
                }
                _connectionLimit = value;
                OnPropertyChanged("ConnectionLimit");
            }
        }

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
            _backup = new PgsqlUser(null, this);
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
        public PgsqlUser(NamedCollection owner, PgsqlUser basedOn) : base(owner, basedOn)
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
