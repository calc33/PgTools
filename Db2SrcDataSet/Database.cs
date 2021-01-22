using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public partial class Database: IComparable
    {
        public string Name { get; set; }
        public string DbaUserName { get; set; }
        public string Encoding { get; set; }
        public string DefaultTablespace { get; set; }
        public ConnectionInfo ConnectionInfo { get; set; }
        public string Version { get; set; }
        public bool IsCurrent { get; set; }
        private Dictionary<string, object> _attributes = new Dictionary<string, object>();
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

        public int CompareTo(object obj)
        {
            if (!(obj is Database))
            {
                return -1;
            }
            return string.Compare(Name, ((Database)obj).Name);
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
    }
}
