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
        private int[] _versionNum;
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
                _dbaUserName = value;
                OnPropertyChanged("DbaUserName");
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
                _encoding = value;
                OnPropertyChanged("Encoding");
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
                _defaultTablespace = value;
                OnPropertyChanged("DefaultTablespace");
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
                _connectionInfo = value;
                OnPropertyChanged("ConnectionInfo");
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
                _version = value;
                OnPropertyChanged("Version");
            }
        }

        public int[] VersionNum
        {
            get { return _versionNum; }
            set
            {
                if (_versionNum == value)
                {
                    return;
                }
                _versionNum = value;
                OnPropertyChanged("VersionNum");
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
                _isCurrent = value;
                OnPropertyChanged("IsCurrent");
            }
        }

        protected override string GetFullIdentifier()
        {
            return Name;
        }
        protected override string GetIdentifier()
        {
            return Name;
        }

        public virtual ConnectionInfo GetConnectionInfoFor(ConnectionInfo basedOn, string userName)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, object> _attributes = new Dictionary<string, object>();

        protected Database _backup;
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

        public Database(Database basedOn) : base(null, basedOn)
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
