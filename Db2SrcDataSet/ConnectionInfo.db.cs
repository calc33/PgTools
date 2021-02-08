using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace Db2Source
{
    public class StoreConnectionAttribute: Attribute
    {
        public string TableName { get; internal set; }
        public StoreConnectionAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
    partial class ConnectionInfo
    {
        public long? Id { get; internal set; }
        private static readonly string[] _keyPropertyNames = new string[] { "ServerName", "UserName" };
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
                l.Add(p);
            }
            return l.ToArray();
        }
        private static readonly PropertyInfo[] _keyProperties = InitKeyProperties();
        protected virtual PropertyInfo[] GetKeyProperties()
        {
            return _keyProperties;
        }
        private bool _reading = false;
        protected void KeyPropertyChanged()
        {
            if (_reading)
            {
                return;
            }
            Id = null;
        }

        internal string GetTableName()
        {
            return GetTableName(GetType());
        }
        internal static string GetTableName(Type type)
        {
            StoreConnectionAttribute attr = type.GetCustomAttribute(typeof(StoreConnectionAttribute)) as StoreConnectionAttribute;
            if (attr != null)
            {
                return attr.TableName.ToUpper();
            }
            return type.Name.ToUpper();
        }
        internal static string GetFieldName(PropertyInfo property)
        {
            string fld = property.Name;
            JsonPropertyAttribute attr = property.GetCustomAttribute(typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;
            if (attr != null && !string.IsNullOrEmpty(attr.PropertyName))
            {
                fld = attr.PropertyName;
            }
            return fld.ToUpper();
        }

        private static readonly byte[] Key = new byte[] {
            250, 151, 40, 155, 24, 116, 106, 193,
            81, 184, 33, 22, 49, 177, 163, 109,
            237, 245, 155, 252, 26, 230, 77, 146,
            89, 248, 13, 204, 27, 233, 170, 45 };
        private static readonly byte[] IV = new byte[] {
            36, 37, 252, 136, 216, 97, 24, 192,
            39, 90, 13, 76, 209, 162, 21, 170 };

        private static string CryptStr(Aes aes, byte[] data)
        {
            ICryptoTransform crypto = aes.CreateEncryptor();
            byte[] b = crypto.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(b);
        }

        private static byte[] DecryptStr(Aes aes, string data)
        {
            
            byte[] b = Convert.FromBase64String(data);
            ICryptoTransform crypto = aes.CreateDecryptor();
            return crypto.TransformFinalBlock(b, 0, b.Length);
        }
        private static byte[] DecryptStr(string data)
        {
            return DecryptStr(Aes, data);
        }

        private static Type _appType = null;
        private static Type GetAppType()
        {
            if (_appType != null)
            {
                return _appType;
            }
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name == "App")
                    {
                        _appType = t;
                        return _appType;
                    }
                }
            }
            return null;
        }

        private static Aes CreateAes()
        {
            Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            Type t = GetAppType();
            if (t == null)
            {
                return aes;
            }
            t = t.BaseType;
            MethodInfo m = t.GetMethod("GetResourceStream", BindingFlags.Static | BindingFlags.Public);
            object res = m.Invoke(null, new object[] { new Uri("pack://application:,,,/app.ico") });
            PropertyInfo p = res.GetType().GetProperty("Stream");
            Stream s = p.GetValue(res) as Stream;
            byte[] b = new byte[s.Length];
            s.Read(b, 0, b.Length);
            int n = b.Length / 2;
            SHA256 sha = SHA256.Create();
            aes.Key = sha.ComputeHash(b, 0, n);
            byte[] b2 = new byte[aes.IV.Length];
            Array.Copy(sha.ComputeHash(b, n, b.Length - n), b2, b2.Length);
            aes.IV = b2;
            RegistryKey reg = Registry.CurrentUser.CreateSubKey(@"Software\Db2Src", true);
            object k = reg.GetValue("Key");
            if (k == null)
            {
                Aes aes2 = Aes.Create();
                reg.SetValue("Key", CryptStr(aes, aes2.IV));
                aes.IV = aes2.IV;
            }
            else
            {
                aes.IV = DecryptStr(aes, k.ToString());
            }
            return aes;
        }
        private static Aes _aes = null;
        private static Aes Aes
        {
            get
            {
                if (_aes == null)
                {
                    _aes = CreateAes();
                }
                return _aes;
            }
        }

        /// <summary>
        /// ConnectionInfo.dbに保存するためのパスワード文字列
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCryptedPassword()
        {
            ICryptoTransform crypt = Aes.CreateEncryptor();
            byte[] b = Encoding.UTF8.GetBytes(Password);
            return Convert.ToBase64String(crypt.TransformFinalBlock(b, 0, b.Length));
        }

        private SQLiteCommand GetInsertSqlCommand(SQLiteConnection connection)
        {
            List<PropertyInfo> flds = new List<PropertyInfo>(GetStoredFields(GetType(), true));
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
                    bufF.Append(", ");
                    bufV.Append(", ");
                    object o = p.GetValue(this);
                    if (o == null)
                    {
                        o = DBNull.Value;
                    }
                    cmd.Parameters.Add(new SQLiteParameter("@" + f, o));
                }
                bufF.Append("PASSWORD, ");
                bufV.Append("@PASSWORD, ");
                string pass = GetCryptedPassword();
                cmd.Parameters.Add(new SQLiteParameter("@PASSWORD", string.IsNullOrEmpty(pass) ? (object)DBNull.Value : pass));

                bufF.Append("BKCOLOR, ");
                bufV.Append("@BKCOLOR, ");
                cmd.Parameters.Add(new SQLiteParameter("@BKCOLOR", BkColor));

                bufF.Append("LAST_MODIFIED");
                bufV.Append(DateTime.Now.ToOADate());

                StringBuilder buf = new StringBuilder();
                buf.Append("INSERT INTO ");
                buf.Append(GetTableName());
                buf.AppendLine(" (");
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
        internal SQLiteCommand GetDeleteSqlCommand(SQLiteConnection connection)
        {
            SQLiteCommand cmd = new SQLiteCommand(connection);
            try
            {
                StringBuilder buf = new StringBuilder();
                buf.Append("DELETE FROM ");
                buf.AppendLine(GetTableName());
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
        private void GetWhereClauseByKey(StringBuilder sql, SQLiteParameterCollection parameters)
        {
            PropertyInfo[] keys = GetKeyProperties();
            if (keys == null)
            {
                return;
            }
            string prefix = "WHERE ";
            foreach (PropertyInfo p in keys)
            {
                sql.Append(prefix);
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
                string f = GetFieldName(p);
                if (o is DBNull)
                {
                    sql.Append(f);
                    sql.AppendLine(" IS NULL");
                }
                else
                {
                    sql.Append(f);
                    sql.Append(" = @");
                    sql.Append(f);
                    sql.AppendLine();
                    parameters.Add(new SQLiteParameter("@" + f, o));
                }
                prefix = "  AND ";
            }
        }
        internal SQLiteCommand GetDeleteByKeySqlCommand(SQLiteConnection connection)
        {
            PropertyInfo[] keys = GetKeyProperties();
            if (keys == null)
            {
                return null;
            }
            SQLiteCommand cmd = new SQLiteCommand(connection);
            try
            {
                StringBuilder buf = new StringBuilder();
                buf.Append("DELETE FROM ");
                buf.AppendLine(GetTableName());
                GetWhereClauseByKey(buf, cmd.Parameters);
                cmd.CommandText = buf.ToString();
            }
            catch
            {
                cmd.Dispose();
                throw;
            }
            return cmd;
        }
        internal SQLiteCommand GetUpdateSqlCommand(SQLiteConnection connection)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("UPDATE ");
            buf.Append(GetTableName());
            buf.AppendLine(" SET");
            List<PropertyInfo> flds = new List<PropertyInfo>(GetStoredFields(GetType(), true));
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
                    buf.Append(',');
                    buf.AppendLine();
                    object o = p.GetValue(this);
                    if (o == null)
                    {
                        o = DBNull.Value;
                    }
                    cmd.Parameters.Add(new SQLiteParameter("@" + f, o));
                }
                buf.AppendLine("  PASSWORD = @PASSWORD,");
                string pass = GetCryptedPassword();
                cmd.Parameters.Add(new SQLiteParameter("@PASSWORD", string.IsNullOrEmpty(pass) ? (object)DBNull.Value : pass));

                buf.AppendLine("  BKCOLOR = @BKCOLOR,");
                cmd.Parameters.Add(new SQLiteParameter("@BKCOLOR", BkColor));
                buf.Append("  LAST_MODIFIED = ");
                buf.Append(DateTime.Now.ToOADate());
                buf.AppendLine();
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
        internal SQLiteCommand GetIdSqlCommand(SQLiteConnection connection)
        {
            PropertyInfo[] keys = GetKeyProperties();
            if (keys == null)
            {
                return null;
            }
            SQLiteCommand cmd = new SQLiteCommand(connection);
            try
            {
                StringBuilder buf = new StringBuilder();
                buf.Append("SELECT ID FROM ");
                buf.AppendLine(GetTableName());
                GetWhereClauseByKey(buf, cmd.Parameters);
                cmd.CommandText = buf.ToString();
            }
            catch
            {
                cmd.Dispose();
                throw;
            }
            return cmd;
        }
        private SQLiteCommand GetLastInsertRowIdCommand(SQLiteConnection connection)
        {
            return new SQLiteCommand("select last_insert_rowid()", connection);
        }
        private long ExecuteInsert(SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = GetInsertSqlCommand(connection))
            {
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = GetLastInsertRowIdCommand(connection))
            {
                object v = cmd.ExecuteScalar();
                Id = (long)v;
            }
            return Id.Value;
        }
        private void ExecuteUpdate(SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = GetUpdateSqlCommand(connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        internal void ExecuteDelete(SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = GetDeleteByKeySqlCommand(connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        private bool RecordExists(SQLiteConnection connection)
        {
            if (!Id.HasValue)
            {
                return false;
            }
            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT ID FROM {0} WHERE ID = @ID", GetTableName()), connection))
            {
                cmd.Parameters.Add(new SQLiteParameter("@ID", Id.Value));
                Id = (long?)cmd.ExecuteScalar();
            }
            return Id.HasValue;
        }
        private long? FindRecord(SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = GetIdSqlCommand(connection))
            {
                Id = (long?)cmd.ExecuteScalar();
            }
            return Id;
        }
        public void SaveChanges(SQLiteConnection connection)
        {
            if (!RecordExists(connection))
            {
                FindRecord(connection);
            }
            if (Id.HasValue)
            {
                ExecuteUpdate(connection);
            }
            else
            {
                ExecuteInsert(connection);
            }
        }
        public static PropertyInfo[] GetStoredFields(Type databaseType, bool excludePassword)
        {
            List<PropertyInfo> l = new List<PropertyInfo>();
            foreach (PropertyInfo p in databaseType.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
                {
                    continue;
                }
                if (excludePassword && (string.Compare(p.Name, "Password", true) == 0 || string.Compare(p.Name, "CryptedPassword", true) == 0))
                {
                    continue;
                }
                l.Add(p);
            }
            return l.ToArray();
        }
        public PropertyInfo[] GetStoredFields(bool excludePassword)
        {
            return GetStoredFields(GetType(), excludePassword);
        }
        public static PropertyInfo[] PrepareForReader(Type databaseType, SQLiteDataReader reader)
        {
            Dictionary<string, PropertyInfo> nameToProps = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo p in GetStoredFields(databaseType, false))
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
            _reading = true;
            try
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    PropertyInfo p = mapping[i];
                    if (p == null || !p.CanWrite)
                    {
                        continue;
                    }
                    object o = reader.GetValue(i);
                    if (o == null || o is DBNull)
                    {
                        p.SetValue(this, null);
                    }
                    else
                    {
                        Type t = p.PropertyType;
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            t = t.GetGenericArguments()[0];
                        }
                        p.SetValue(this, Convert.ChangeType(o, t));
                    }
                }
                if (!string.IsNullOrEmpty(Password))
                {
                    Password = Encoding.UTF8.GetString(DecryptStr(Password));
                }
                Name = GetDefaultName();
            }
            finally
            {
                _reading = false;
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
        private static readonly string[] SqliteDbTypeName = new string[] { "INTEGER", "REAL", "TEXT", "BLOB" };
        //private static readonly Dictionary<SqliteDbType, DbType> SqliteDbTypeToDbType = new Dictionary<SqliteDbType, DbType>()
        //{
        //    { SqliteDbType.Integer, DbType.Int64 },
        //    { SqliteDbType.Real, DbType.Double },
        //    { SqliteDbType.Text, DbType.String },
        //    { SqliteDbType.Blob, DbType.Binary }
        //};
        private static SqliteDbType GetSqliteDbType(PropertyInfo property)
        {
            Type t = property.PropertyType;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = t.GetGenericArguments()[0];
            }
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
        private static string GetSelectSql(Type databaseType, string condition)
        {
            string tbl = ConnectionInfo.GetTableName(databaseType);
            PropertyInfo[] flds = ConnectionInfo.GetStoredFields(databaseType, false);
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
            buf.Append(ConnectionInfo.GetTableName(databaseType));
            buf.AppendLine(" (");
            buf.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            foreach (PropertyInfo p in ConnectionInfo.GetStoredFields(databaseType, false))
            {
                if (string.Compare(p.Name, "ID", true) == 0)
                {
                    continue;
                }
                buf.Append("  ");
                buf.Append(ConnectionInfo.GetFieldName(p));
                buf.Append(" ");
                buf.Append(SqliteDbTypeName[(int)GetSqliteDbType(p)]);
                buf.AppendLine(",");
            }
            //buf.AppendLine("  BKCOLOR INTEGER,");
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
            PropertyInfo[] flds = ConnectionInfo.GetStoredFields(databaseType, false);
            List<string> l = new List<string>(flds.Length);
            foreach (PropertyInfo p in flds)
            {
                string fld = ConnectionInfo.GetFieldName(p);
                if (!dbFldDict.ContainsKey(fld))
                {
                    l.Add(string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}", databaseType.Name, fld, SqliteDbTypeName[(int)GetSqliteDbType(p)]));
                }
            }
            //if (!dbFldDict.ContainsKey("BKCOLOR"))
            //{
            //    l.Add(string.Format("ALTER TABLE {0} ADD COLUMN BKCOLOR INTEGER", databaseType.Name));
            //}
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
            foreach (Type t in ConnectionInfoTypes)
            {
                RequireDataTable(conn, t);
            }
            return conn;
        }
        private List<ConnectionInfo> LoadInternal()
        {
            if (!File.Exists(Path))
            {
                // 接続先情報がない場合はDB固有の接続情報を取得
                return LoadKnownConnections();
            }
            List<ConnectionInfo> l = new List<ConnectionInfo>();
            using (SQLiteConnection conn = RequireDatabase(Path))
            {
                foreach (Type t in ConnectionInfoTypes)
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
        private List<ConnectionInfo> LoadKnownConnections()
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>();
            foreach (Type t in ConnectionInfoTypes)
            {
                MethodInfo mi = t.GetMethod("GetKnownConnectionInfos", BindingFlags.Static | BindingFlags.Public);
                if (mi == null)
                {
                    continue;
                }
                ConnectionInfo[] infos = mi.Invoke(null, null) as ConnectionInfo[];
                if (infos == null)
                {
                    continue;
                }
                MergeByContent(l, infos);
            }
            return l;
        }
        private void SaveInternal()
        {
            using (SQLiteConnection conn = RequireDatabase(Path))
            {
                foreach (ConnectionInfo info in _list)
                {
                    info.SaveChanges(conn);
                }
            }
        }
        public void Delete(ConnectionInfo info)
        {
            using (SQLiteConnection conn = RequireDatabase(Path))
            {
                info.ExecuteDelete(conn);
            }
            Remove(info);
        }
    }
}
