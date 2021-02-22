using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public partial class Database: SchemaObject
    {
        private string _dbaUserName;
        private string _encoding;
        private string _defaultTablespace;
        private ConnectionInfo _connectionInfo;
        private string _version;
        private bool _isCurrent;
        public string DbaUserName
        {
            get
            {
                return _dbaUserName;
            }
            set
            {
                if (_dbaUserName == value)
                {
                    return;
                }
                string old = _dbaUserName;
                _dbaUserName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DbaUserName", _dbaUserName, old));
            }
        }

        public string Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                if (_encoding == value)
                {
                    return;
                }
                string old = _encoding;
                _encoding = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Encoding", _encoding, old));
            }
        }

        public string DefaultTablespace
        {
            get
            {
                return _defaultTablespace;
            }
            set
            {
                if (_defaultTablespace == value)
                {
                    return;
                }
                string old = _defaultTablespace;
                _defaultTablespace = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DefaultTablespace", _defaultTablespace, old));
            }
        }

        public ConnectionInfo ConnectionInfo
        {
            get
            {
                return _connectionInfo;
            }
            set
            {
                if (_connectionInfo == value)
                {
                    return;
                }
                ConnectionInfo old = _connectionInfo;
                _connectionInfo = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ConnectionInfo", _connectionInfo, old));
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (_version == value)
                {
                    return;
                }
                string old = _version;
                _version = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Version", _version, old));
            }
        }

        public bool IsCurrent
        {
            get
            {
                return _isCurrent;
            }
            set
            {
                if (_isCurrent == value)
                {
                    return;
                }
                bool old = _isCurrent;
                _isCurrent = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsCurrent", _isCurrent, old));
            }
        }

        protected override string GetIdentifier()
        {
            return Name;
        }

        private Dictionary<string, object> _attributes = new Dictionary<string, object>();

        protected Database _backup;
        public override void Backup()
        {
            _backup = new Database(this);
        }

        protected void RestoreFrom(Database backup)
        {
            base.RestoreFrom(backup);
            DbaUserName = backup.DbaUserName;
            Encoding = backup.Encoding;
            DefaultTablespace = backup.DefaultTablespace;
            ConnectionInfo = backup.ConnectionInfo;
            Version = backup.Version;
            IsCurrent = backup.IsCurrent;
            _attributes = new Dictionary<string, object>(backup._attributes);
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
            return DbaUserName == _backup.DbaUserName
                && Encoding == _backup.Encoding
                && DefaultTablespace == _backup.DefaultTablespace
                && ConnectionInfo.Equals(_backup.ConnectionInfo)
                && Version == _backup.Version
                && DictionaryEquals(_attributes, _backup._attributes);
        }

        public Database(Db2SourceContext context, string objectName) : base(context, string.Empty, string.Empty, objectName, Schema.CollectionIndex.None)
        {
            Schema = null;
        }

        internal Database(Database basedOn) : base(basedOn)
        {
            Schema = null;
            DbaUserName = basedOn.DbaUserName;
            Encoding = basedOn.Encoding;
            DefaultTablespace= basedOn.DefaultTablespace;
            ConnectionInfo = basedOn.ConnectionInfo;
            Version = basedOn.Version;
            IsCurrent = basedOn.IsCurrent;
            _attributes = new Dictionary<string, object>(basedOn._attributes);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        //}

        public IEnumerable<string> AttributeNames
        {
            get
            {
                return _attributes.Keys;
            }
        }
        public object Attribute(string key)
        {
            object o;
            if (!_attributes.TryGetValue(key, out o))
            {
                o = null;
            }
            return o;
        }
        public T Attribute<T>(string key)
        {
            object o = Attribute(key);
            return (T)Convert.ChangeType(o, typeof(T));
        }

        public void AddAttibute(string name, object value)
        {
            _attributes[name] = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Database))
            {
                return false;
            }
            return string.Equals(Name, ((Database)obj).Name);
        }

        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return 0;
            }
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public override string GetSqlType()
        {
            throw new NotImplementedException();
        }

        public override string GetExportFolderName()
        {
            throw new NotImplementedException();
        }
    }
}
