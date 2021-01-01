using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class NpgsqlConnectionInfo
    {
        private static readonly string[] _keyPropertyNames = new string[] { "ServerName", "ServerPort", "UserName" };
        private static PropertyInfo[] InitKeyProperties()
        {
            List<PropertyInfo> l = new List<PropertyInfo>();
            foreach (string k in _keyPropertyNames)
            {
                PropertyInfo p = typeof(NpgsqlConnectionInfo).GetProperty(k, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                if (p == null)
                {
                    throw new ArgumentException(string.Format("{0}: プロパティが存在しません", k));
                }
                l.Add(p);
            }
            return l.ToArray();
        }
        private static PropertyInfo[] _keyProperties = InitKeyProperties();
        protected override PropertyInfo[] GetKeyProperties()
        {
            return _keyProperties;
        }

    }
}
