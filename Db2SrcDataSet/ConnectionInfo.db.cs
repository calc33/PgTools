using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Db2Source
{
    partial class ConnectionInfo
    {
        public long Id { get; internal set; }
        private static readonly string[] _keyPropertyNames = new string[] { "DatabaseType", "ServerName", "UserName" };
        private static PropertyInfo[] InitKeyProperties()
        {
            List<PropertyInfo> l = new List<PropertyInfo>();
            foreach (string k in _keyPropertyNames)
            {
                PropertyInfo p = typeof(ConnectionInfo).GetProperty(k, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                if (p == null)
                {
                    throw new ArgumentException(string.Format("{0}: プロパティが存在しません", k));
                }
            }
            return l.ToArray();
        }
        private static PropertyInfo[] _keyProperties = InitKeyProperties();
        protected virtual PropertyInfo[] GetKeyProperties()
        {
            return _keyProperties;
        }
        internal static string GetTableName(Type type)
        {
            return type.Name.ToUpper();
        }
        internal static string GetFieldName(PropertyInfo property)
        {
            return property.Name.ToUpper();
        }

        private SQLiteCommand GetInsertSqlCommand(SQLiteConnection connection)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("INSERT INTO ");
            buf.Append(GetTableName(GetType()));
            buf.Append(" (");
            buf.AppendLine();
            List<PropertyInfo> flds = new List<PropertyInfo>(GetStoredFields(GetType()));
            if (flds.Count == 0)
            {
                return null;
            }
            SQLiteCommand cmd = new SQLiteCommand(connection);
            try
            {
                StringBuilder bufF = new StringBuilder();
                StringBuilder bufV = new StringBuilder();
                bufF.Append("  ");
                bufV.Append("  ");
                int n = flds.Count - 1;
                for (int i = 0; i <= n; i++)
                {
                    PropertyInfo p = flds[i];
                    string f = GetFieldName(p);
                    bufF.Append(f);
                    bufV.Append("@");
                    bufV.Append(f);
                    if (i != n)
                    {
                        bufF.Append(", ");
                        bufV.Append(", ");
                    }
                    buf.AppendLine();
                    object o = p.GetValue(this);
                    if (o == null)
                    {
                        o = DBNull.Value;
                    }
                    cmd.Parameters.Add(new SQLiteParameter("@" + f, o));
                }
                buf.AppendLine(bufF.ToString());
                buf.AppendLine(") VALUES (");
                buf.AppendLine(bufV.ToString());
                buf.AppendLine(")");
                cmd.CommandText = buf.ToString();
            }
            catch
            {
                cmd.Dispose();
                throw;
            }
            return cmd;
        }
        private SQLiteCommand GetIdSqlCommand(SQLiteConnection connection)
        {
            PropertyInfo[] keys = GetKeyProperties();
            if (keys == null)
            {
                return null;
            }
            SQLiteCommand cmd = new SQLiteCommand(connection);
            StringBuilder buf = new StringBuilder();
            buf.Append("SELECT ID FROM ");
            buf.AppendLine(GetTableName(GetType()));
            int n = keys.Length - 1;
            for (int i = 0; i <= n; i++)
            {
                buf.Append((i == 0) ? "WHERE " : "  AND ");
                PropertyInfo p = keys[i];
                string f = GetFieldName(p);
                buf.Append(f);
                buf.Append(" = @");
                buf.Append(f);
                buf.AppendLine();
                object o = p.GetValue(this);
                if (o == null)
                {
                    if (p.PropertyType == typeof(string) || p.PropertyType.IsSubclassOf(typeof(string)))
                    {
                        o = string.Empty;
                    }
                    else
                    {
                        o = DBNull.Value;
                    }
                }
                cmd.Parameters.Add(new SQLiteParameter("@" + f, o));
            }
            cmd.CommandText = buf.ToString();
            return cmd;
        }
        private bool RecordExists(SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT ID FROM {0} WHERE ID = @ID", GetTableName(GetType())), connection))
            {
                cmd.Parameters.Add(new SQLiteParameter("@ID", Id));
                return (cmd.ExecuteScalar() != null);
            }
        }
        private SQLiteCommand GetUpdateSqlCommand(SQLiteConnection connection)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("UPDATE ");
            buf.Append(GetTableName(GetType()));
            buf.AppendLine(" SET");
            List<PropertyInfo> flds = new List<PropertyInfo>(GetStoredFields(GetType()));
            if (flds.Count == 0)
            {
                return null;
            }
            SQLiteCommand cmd = new SQLiteCommand(connection);
            try
            {
                int n = flds.Count - 1;
                for (int i = 0; i <= n; i++)
                {
                    PropertyInfo p = flds[i];
                    string f = GetFieldName(p);
                    buf.Append("  ");
                    buf.Append(f);
                    buf.Append(" = @");
                    buf.Append(f);
                    if (i != n)
                    {
                        buf.Append(',');
                    }
                    buf.AppendLine();
                    object o = p.GetValue(this);
                    if (o == null)
                    {
                        o = DBNull.Value;
                    }
                    cmd.Parameters.Add(new SQLiteParameter("@" + f, o));
                }
                buf.AppendLine("WHERE ID = @ID");
                cmd.Parameters.Add(new SQLiteParameter("@ID", Id));
                cmd.CommandText = buf.ToString();
            }
            catch
            {
                cmd.Dispose();
                throw;
            }
            return cmd;
        }
        public void SaveChanges(SQLiteConnection connection)
        {

        }
        public static PropertyInfo[] GetStoredFields(Type databaseType)
        {
            List<PropertyInfo> l = new List<PropertyInfo>();
            foreach (PropertyInfo p in databaseType.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (p.GetCustomAttributes<JsonIgnoreAttribute>().Count() != 0)
                {
                    continue;
                }
                if (string.Compare(p.Name, "Password") == 0 || string.Compare(p.Name, "CryptedPassword") == 0)
                {
                    continue;
                }
                l.Add(p);
            }
            return l.ToArray();
        }
        public PropertyInfo[] GetStoredFields()
        {
            return GetStoredFields(GetType());
        }
        public static PropertyInfo[] PrepareForReader(Type databaseType, SQLiteDataReader reader)
        {
            Dictionary<string, PropertyInfo> nameToProps = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo p in GetStoredFields(databaseType))
            {
                nameToProps.Add(GetFieldName(p), p);
            }
            List<PropertyInfo> props = new List<PropertyInfo>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                PropertyInfo p;
                string nm = reader.GetName(i);
                if (nameToProps.TryGetValue(nm, out p))
                {
                    props.Add(p);
                }
                else
                {
                    props.Add(null);
                }
            }
            return props.ToArray();
        }
        public void ReadFromReader(SQLiteDataReader reader, PropertyInfo[] mapping)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                PropertyInfo p = mapping[i];
                if (p == null || !p.CanWrite)
                {
                    continue;
                }
                p.SetValue(this, reader.GetValue(i));
            }
        }
    }
    public enum SqliteDbType
    {
        Integer,
        Real,
        Text,
        Blob
    }
    partial class ConnectionList
    {
        private static string[] SqliteDbTypeName = new string[] { "INTEGER", "REAL", "TEXT", "BLOB" };
        private static SqliteDbType GetDbType(PropertyInfo property)
        {
            Type t = property.PropertyType;
            if (t.IsPrimitive)
            {
                if (t == typeof(float) || t == typeof(double))
                {
                    return SqliteDbType.Real;
                }
                else
                {
                    return SqliteDbType.Integer;
                }
            }
            if (t == typeof(string) || t.IsSubclassOf(typeof(string)))
            {
                return SqliteDbType.Text;
            }
            throw new ArgumentException(string.Format("{0}: {1}型はサポートしていません", property.Name, t.Name));
        }
        //public static PropertyInfo[] GetStoredFields(Type databaseType)
        //{
        //    List<PropertyInfo> l = new List<PropertyInfo>();
        //    foreach (PropertyInfo p in databaseType.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance))
        //    {
        //        if (p.GetCustomAttributes<JsonIgnoreAttribute>().Count() != 0)
        //        {
        //            continue;
        //        }
        //        if (string.Compare(p.Name, "Password") == 0 || string.Compare(p.Name, "CryptedPassword") == 0)
        //        {
        //            continue;
        //        }
        //        l.Add(p);
        //    }
        //    return l.ToArray();
        //}
        private static string GetSelectSql(Type databaseType, string condition)
        {
            string tbl = databaseType.Name;
            PropertyInfo[] flds = ConnectionInfo.GetStoredFields(databaseType);
            if (flds == null || flds.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("SELECT ID");
            foreach (PropertyInfo p in flds)
            {
                buf.Append(", ");
                buf.Append(ConnectionInfo.GetFieldName(p));
            }
            buf.AppendLine();
            buf.Append("FROM ");
            buf.AppendLine(tbl);
            if (!string.IsNullOrEmpty(condition))
            {
                buf.Append("WHERE ");
                buf.Append(condition);
            }
            return buf.ToString();
        }
        private static string[] GetDbFieldNames(SQLiteConnection connection, Type databaseType)
        {
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT * FROM {0} WHERE 0=1", databaseType.Name), connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        List<string> l = new List<string>(reader.FieldCount);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            l.Add(reader.GetName(i));
                        }
                        return l.ToArray();
                    }
                }
            }
            catch
            {
                return new string[0];
            }
        }
        private static string GetCreateTableSql(Type databaseType)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("CREATE TABLE ");
            buf.Append(databaseType.Name);
            buf.AppendLine(" (");
            buf.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            foreach (PropertyInfo p in ConnectionInfo.GetStoredFields(databaseType))
            {
                buf.Append("  ");
                buf.Append(ConnectionInfo.GetFieldName(p));
                buf.Append(" ");
                buf.Append(SqliteDbTypeName[(int)GetDbType(p)]);
                buf.AppendLine(",");
            }
            buf.AppendLine("  LAST_MODIFIED REAL");
            buf.AppendLine(")");
            return buf.ToString();
        }
        private static string[] GetAlterTableSql(SQLiteConnection connection, Type databaseType)
        {
            string[] dbFlds = GetDbFieldNames(connection, databaseType);
            if (dbFlds == null || dbFlds.Length == 0)
            {
                return new string[] { GetCreateTableSql(databaseType) };
            }
            Dictionary<string, bool> dbFldDict = new Dictionary<string, bool>();
            foreach (string f in dbFlds)
            {
                dbFldDict.Add(f.ToUpper(), true);
            }
            PropertyInfo[] flds = ConnectionInfo.GetStoredFields(databaseType);
            List<string> l = new List<string>(flds.Length);
            foreach (PropertyInfo p in flds)
            {
                if (!dbFldDict.ContainsKey(ConnectionInfo.GetFieldName(p)))
                {
                    l.Add(string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}", databaseType.Name, p.Name, SqliteDbTypeName[(int)GetDbType(p)]));
                }
            }
            return l.ToArray();
        }
        private static void RequireDataTable(SQLiteConnection connection, Type databaseType)
        {
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(GetSelectSql(databaseType, "0=1"), connection))
                {
                    cmd.ExecuteNonQuery();
                    return;
                }
            }
            catch
            {
                using (SQLiteCommand cmd = new SQLiteCommand(connection)) {
                    foreach (string sql in GetAlterTableSql(connection, databaseType))
                    {
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public static SQLiteConnection RequireDatabase(string databasePath)
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = databasePath,
                FailIfMissing = false,
                ReadOnly = false
            };
            SQLiteConnection conn = new SQLiteConnection(builder.ToString());
            conn.Open();
            foreach (Type t in _connectionInfoClasses)
            {
                RequireDataTable(conn, t);
            }
            return conn;
        }
        private List<ConnectionInfo> LoadInternal()
        {
            if (!File.Exists(Path))
            {
                return null;
            }
            List<ConnectionInfo> l = new List<ConnectionInfo>();
            using (SQLiteConnection conn = RequireDatabase(Path))
            {
                foreach (Type t in _connectionInfoClasses)
                {
                    ConstructorInfo ctor = t.GetConstructor(Type.EmptyTypes);
                    if (ctor == null)
                    {
                        throw new ArgumentException(string.Format("{0}: コンストラクタが見つかりません", t.FullName));
                    }
                    using (SQLiteCommand cmd = new SQLiteCommand(GetSelectSql(t, null), conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            PropertyInfo[] props = ConnectionInfo.PrepareForReader(t, reader);
                            while (reader.Read())
                            {
                                ConnectionInfo obj = ctor.Invoke(null) as ConnectionInfo;
                                obj.ReadFromReader(reader, props);
                                l.Add(obj);
                            }
                        }
                    }
                }
            }
            return l;
        }
    }
}
