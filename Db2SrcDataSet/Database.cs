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
        //private string _name;
        private string _dbaUserName;
        private string _encoding;
        private string _defaultTablespace;
        private ConnectionInfo _connectionInfo;
        private string _version;
        private bool _isCurrent;
        //public string Name
        //{
        //    get
        //    {
        //        return _name;
        //    }
        //    set
        //    {
        //        if (_name == value)
        //        {
        //            return;
        //        }
        //        _name = value;
        //        OnPropertyChanged("Name");
        //    }
        //}
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

        private readonly Dictionary<string, object> _attributes = new Dictionary<string, object>();

        public Database(Db2SourceContext context, string objectName) : base(context, string.Empty, string.Empty, objectName, Schema.CollectionIndex.None)
        {
            Schema = null;
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

        //public int CompareTo(object obj)
        //{
        //    if (!(obj is Database))
        //    {
        //        return -1;
        //    }
        //    return string.Compare(Name, ((Database)obj).Name);
        //}

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
