using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Db2Source
{
    public class SQLTextAttribute: Attribute
    {
        public string SQLText { get; set; }
        public SQLTextAttribute(string sql) { SQLText = sql; }
        private static string GetSQLText<T>(T value) where T : Enum
        {
            string v = Enum.GetName(typeof(T), value);
            FieldInfo field = typeof(T).GetField(v);
            if (field == null)
            {
                return null;
            }
            foreach (SQLTextAttribute attr in field.GetCustomAttributes<SQLTextAttribute>())
            {
                return attr.SQLText;
            }
            return v;
        }
        public static Dictionary<T, string> GetValueToNameDict<T>() where T : Enum
        {
            Dictionary<T, string> dict = new Dictionary<T, string>();
            foreach (T v in Enum.GetValues(typeof(T)))
            {
                string s = GetSQLText(v);
                dict.Add(v, s);
            }
            return dict;
        }
        public static Dictionary<string, T> GetNameToValueDict<T>() where T : Enum
        {
            Dictionary<string, T> dict = new Dictionary<string, T>();
            foreach (T v in Enum.GetValues(typeof(T)))
            {
                string s = GetSQLText(v);
                dict.Add(s, v);
            }
            return dict;
        }
    }
    public enum SqliteDbType
    {
        [SQLText("INTEGER")]
        Integer,
        [SQLText("REAL")]
        Real,
        [SQLText("TEXT")]
        Text,
        [SQLText("BLOB")]
        Blob,
        [SQLText("NULL")]
        Null
    }
    public enum SqliteConstraintType
    {
        [SQLText("PRIMARY KEY")]
        Primary,
        [SQLText("UNIQUE")]
        Unique,
        //[SQLText("FOREIGN KEY")]
        //ForeignKey,
        //[SQLText("PRIMARY KEY")]
        //Check,
    }
    public class FieldDefinition
    {
        public string Name { get; set; }
        public SqliteDbType DbType { get; set; }
        public bool AutoIncrement { get; set; }
        public bool NotNull { get; set; }
        public string DefaultExpr { get; set; }

        public static string GetFieldName(PropertyInfo property)
        {
            string fld = property.Name;
            JsonPropertyNameAttribute attr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.Name))
            {
                fld = attr.Name;
            }
            return fld.ToUpper();
        }

        public FieldDefinition() { }
        public FieldDefinition(string name, string type)
        {
            Name = name;
            DbType = GetDbTypeByName(type);
        }
        public FieldDefinition(string name, SqliteDbType type)
        {
            Name = name;
            DbType = type;
        }
        public FieldDefinition(PropertyInfo property)
        {
            Name = GetFieldName(property);
            DbType = GetSqliteDbType(property);
            object v = property.GetCustomAttribute<DefaultValueAttribute>()?.Value;
            if (v != null)
            {
                DefaultExpr = v.ToString();
                if (property.PropertyType.IsAssignableFrom(typeof(string)))
                {
                    DefaultExpr = Db2SourceContext.ToLiteralStr(DefaultExpr);
                }
            }
        }

        public string GetSQLPart()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(Name);
            buf.Append(' ');
            buf.Append(GetDbTypeNameByType(DbType));
            if (NotNull)
            {
                buf.Append(" NOT NULL");
            }
            if (!string.IsNullOrEmpty(DefaultExpr))
            {
                buf.Append(" DEFAULT ");
                buf.Append(DefaultExpr);
            }
            if (AutoIncrement)
            {
                buf.Append(" PRIMARY KEY AUTOINCREMENT");
            }
            return buf.ToString();
        }

        public static readonly Dictionary<SqliteDbType, string> TypeToName = SQLTextAttribute.GetValueToNameDict<SqliteDbType>();
        public static readonly Dictionary<string, SqliteDbType> NameToType = SQLTextAttribute.GetNameToValueDict<SqliteDbType>();
        public static string GetDbTypeNameByType(SqliteDbType type)
        {
            string s;
            if (!TypeToName.TryGetValue(type, out s))
            {
                return null;
            }
            return s;
        }
        public static SqliteDbType GetDbTypeByName(string name)
        {
            SqliteDbType t;
            if (!NameToType.TryGetValue(name.ToUpper(), out t))
            {
                throw new ArgumentException("変換できない名前", "name");
            }
            return t;
        }
        public static SqliteDbType GetSqliteDbType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }
            if (type.IsPrimitive)
            {
                if (type == typeof(float) || type == typeof(double))
                {
                    return SqliteDbType.Real;
                }
                else
                {
                    return SqliteDbType.Integer;
                }
            }
            if (type == typeof(string) || type.IsSubclassOf(typeof(string)))
            {
                return SqliteDbType.Text;
            }
            throw new ArgumentException(string.Format("{0}型はサポートしていません", type.Name));
        }
        public static SqliteDbType GetSqliteDbType(PropertyInfo property)
        {
            try
            {
                return GetSqliteDbType(property.PropertyType);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(string.Format("{0}: {1}型はサポートしていません", property.Name, property.PropertyType.Name));
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, GetDbTypeNameByType(DbType));
        }
    }

    public abstract class ConstraintDefinition
    {
        public string Name { get; set; }
        public abstract string GetSQLPart();

        public ConstraintDefinition() { }
        public ConstraintDefinition(string name)
        {
            Name = name;
        }
    }
    public abstract class KeyConstraintDefinition : ConstraintDefinition
    {
        private bool _noSql = false;
        public abstract string ConstraintType();
        public string[] KeyColumns { get; set; }
        public override string GetSQLPart()
        {
            if (_noSql)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(Name))
            {
                buf.Append("CONSTRAINT ");
                buf.Append(Name);
                buf.Append(" ");
            }
            buf.Append(Name);
            buf.Append(' ');
            buf.Append(ConstraintType());
            if (KeyColumns != null && KeyColumns.Length != 0)
            {
                string prefix = " (";
                foreach (string col in KeyColumns)
                {
                    buf.Append(prefix);
                    buf.Append(col);
                    prefix = ", ";
                }
                buf.Append(")");
            }
            return buf.ToString();
        }

        public KeyConstraintDefinition(bool noSql) : base()
        {
            _noSql = noSql;
        }
        public KeyConstraintDefinition(string name, bool noSql) : base(name)
        {
            _noSql = noSql;
        }
        public KeyConstraintDefinition(string name, string[] keys, bool noSql) : base(name)
        {
            KeyColumns = keys;
            _noSql = noSql;
        }
    }

    public class PrimaryKeyConstraintDefinition : KeyConstraintDefinition
    {
        public override string ConstraintType()
        {
            return "PRIMARY KEY";
        }

        public PrimaryKeyConstraintDefinition(bool noSql) : base(noSql) { }
        public PrimaryKeyConstraintDefinition(string name, bool noSql) : base(name, noSql) { }
        public PrimaryKeyConstraintDefinition(string[] keys, bool noSql) : base(null, keys, noSql) { }
        public PrimaryKeyConstraintDefinition(string name, string[] keys, bool noSql) : base(name, keys, noSql) { }
    }
    public class UniqueConstraintDefinition : KeyConstraintDefinition
    {
        public override string ConstraintType()
        {
            return "UNIQUE";
        }

        public UniqueConstraintDefinition(bool noSql) : base(noSql) { }
        public UniqueConstraintDefinition(string name, bool noSql) : base(name, noSql) { }
        public UniqueConstraintDefinition(string name, string[] keys, bool noSql) : base(name, keys, noSql) { }
    }
    public class ForeignKeyConstraintDefinition: ConstraintDefinition
    {
        public string[] KeyColumns { get; set; }
        public string RefTable { get; set; }
        public string[] RefKeyColumns { get; set; }
        public string Action { get; set; }
        public override string GetSQLPart()
        {
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(Name))
            {
                buf.Append("CONSTRAINT ");
                buf.Append(Name);
                buf.Append(" ");
            }
            buf.Append(Name);
            buf.Append(" FOREIGN KEY");
            if (KeyColumns != null && KeyColumns.Length != 0)
            {
                string prefix = "(";
                foreach (string col in KeyColumns)
                {
                    buf.Append(prefix);
                    buf.Append(col);
                    prefix = ", ";
                }
                buf.Append(")");
            }
            buf.Append(" REFERENCES ");
            buf.Append(RefTable);
            if (RefKeyColumns != null && RefKeyColumns.Length != 0)
            {
                string prefix = "(";
                foreach (string col in RefKeyColumns)
                {
                    buf.Append(prefix);
                    buf.Append(col);
                    prefix = ", ";
                }
                buf.Append(")");
            }
            if (!string.IsNullOrEmpty(Action))
            {
                buf.Append(' ');
                buf.Append(Action);
            }
            return buf.ToString();
        }

        public ForeignKeyConstraintDefinition() : base() { }
        public ForeignKeyConstraintDefinition(string name) : base(name) { }
    }

    public class IndexDefinition
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string[] ColumnNames { get; set; }

        public string GetCreateSQL()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                return null;
            }
            if (ColumnNames == null || ColumnNames.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("CREATE INDEX ");
            buf.Append(Name);
            buf.Append(" ON ");
            buf.Append(TableName);
            string prefix = "(";
            foreach (string column in ColumnNames)
            {
                buf.Append(prefix);
                buf.Append(column);
                prefix = ", ";
            }
            buf.Append(")");
            return buf.ToString();
        }

        public void Apply(SQLiteConnection connection)
        {
            string sql = GetCreateSQL();
            if (string.IsNullOrEmpty(sql))
            {
                return;
            }
            using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    Logger.Default.Log(cmd.CommandText);
                    throw;
                }
            }
        }

        public IndexDefinition() { }
        public IndexDefinition(string name, string tableName, string[] columns)
        {
            Name = name;
            TableName = tableName;
            ColumnNames = columns;
        }
    }

    public class TableDefinition
    {
        public string Name { get; set; }
        public FieldDefinition[] Columns { get; set; }
        public ConstraintDefinition[] Constraints { get; set; }
        public IndexDefinition[] Indexes { get; set; }

        private string GetCreateTableSQL()
        {
            if (Columns.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append("CREATE TABLE ");
            buf.Append(Name);
            string prefix = " (";
            foreach (FieldDefinition col in Columns)
            {
                buf.AppendLine(prefix);
                buf.Append("  ");
                buf.Append(col.GetSQLPart());
                prefix = ",";
            }
            if (Constraints != null)
            {
                foreach (ConstraintDefinition cons in Constraints)
                {
                    string s = cons.GetSQLPart();
                    if (string.IsNullOrEmpty(s))
                    {
                        continue;
                    }
                    buf.AppendLine(prefix);
                    buf.Append("  ");
                    buf.Append(s);
                    prefix = ",";
                }
            }
            buf.AppendLine();
            buf.Append(")");
            return buf.ToString();
        }
        private FieldDefinition[] GetAdditionalColumns(TableDefinition before)
        {
            if (before == null || before.Columns == null || before.Columns.Length == 0)
            {
                return Columns;
            }
            Dictionary<string, FieldDefinition> beforeDict = new Dictionary<string, FieldDefinition>();
            foreach (FieldDefinition column in before.Columns)
            {
                beforeDict.Add(column.Name, column);
            }
            List<FieldDefinition> l = new List<FieldDefinition>();
            foreach (FieldDefinition column in Columns)
            {
                if (beforeDict.ContainsKey(column.Name))
                {
                    continue;
                }
                l.Add(column);
            }
            return l.ToArray();
        }
        private IndexDefinition[] GetAdditionalIndexes(TableDefinition before)
        {
            if (before == null || before.Indexes == null || before.Indexes.Length == 0)
            {
                return Indexes;
            }
            Dictionary<string, IndexDefinition> beforeDict = new Dictionary<string, IndexDefinition>();
            foreach (IndexDefinition index in before.Indexes)
            {
                beforeDict.Add(index.Name, index);
            }
            List<IndexDefinition> l = new List<IndexDefinition>();
            foreach (IndexDefinition index in Indexes)
            {
                if (beforeDict.ContainsKey(index.Name))
                {
                    continue;
                }
                l.Add(index);
            }
            return l.ToArray();
        }
        public void Apply(SQLiteConnection connection)
        {
            TableDefinition old = LoadTableDefinition(Name, connection);
            if (old == null)
            {
                string sql = GetCreateTableSQL();
                if (string.IsNullOrEmpty(sql))
                {
                    return;
                }
                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        Logger.Default.Log(cmd.CommandText);
                        throw;
                    }
                }
                if (Indexes != null)
                {
                    foreach (IndexDefinition index in Indexes)
                    {
                        index.Apply(connection);
                    }
                }
                return;
            }
            foreach (FieldDefinition column in GetAdditionalColumns(old))
            {
                string sql = string.Format("ALTER TABLE {0} ADD COLUMN {1}", Name, column.GetSQLPart());
                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        Logger.Default.Log(cmd.CommandText);
                        throw;
                    }
                }
            }
            IndexDefinition[] diff = GetAdditionalIndexes(old);
            if (diff != null)
            {
                foreach (IndexDefinition index in diff)
                {
                    index.Apply(connection);
                }
            }
        }

        public TableDefinition() { }

        internal TableDefinition(string tableName, SQLiteConnection connection)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            Name = tableName;
            using (DataTable table = connection.GetSchema("Columns", new string[] { null, null, tableName }))
            {
                List<FieldDefinition> l = new List<FieldDefinition>();
                foreach (DataRow row in table.Rows)
                {
                    FieldDefinition def = new FieldDefinition(row["COLUMN_NAME"].ToString(), row["DATA_TYPE"].ToString())
                    {
                        DefaultExpr = row["COLUMN_DEFAULT"].ToString(),
                        NotNull = (row["IS_NULLABLE"].ToString() == "No")
                    };
                    l.Add(def);
                }
                Columns = l.ToArray();
            }
            List<IndexDefinition> lIdx = new List<IndexDefinition>();
            Dictionary<string, IndexDefinition> dict = new Dictionary<string, IndexDefinition>();
            Dictionary<string, List<string>> dictCols = new Dictionary<string, List<string>>();
            using (DataTable table = connection.GetSchema("IndexColumns", new string[] { null, null, tableName }))
            {
                foreach (DataRow row in table.Rows)
                {
                    string idxName = row["INDEX_NAME"].ToString();
                    List<string> lCol;
                    if (!dictCols.TryGetValue(idxName, out lCol))
                    {
                        lCol = new List<string>();
                        dictCols[idxName] = lCol;
                    }
                    lCol.Add(row["COLUMN_NAME"].ToString());
                }
            }
            using (DataTable table = connection.GetSchema("Indexes", new string[] { null, null, tableName }))
            {
                foreach (DataRow row in table.Rows)
                {
                    if ((bool)row["UNIQUE"])
                    {
                        continue;
                    }
                    IndexDefinition index = new IndexDefinition()
                    {
                        Name = row["INDEX_NAME"].ToString(),
                        TableName = row["TABLE_NAME"].ToString(),
                    };
                    dict[index.Name] = index;
                    index.ColumnNames = dictCols[index.Name].ToArray();
                    lIdx.Add(index);
                }
            }
            Indexes = lIdx.ToArray();
        }
        public static TableDefinition LoadTableDefinition(string tableName, SQLiteConnection connection)
        {
            DataTable tbl = connection.GetSchema("Tables", new string[] { null, null, tableName });
            if (tbl.Rows.Count == 0)
            {
                return null;
            }
            try
            {
                return new TableDefinition(tableName, connection);
            }
            catch (SQLiteException)
            {
                return null;
            }
        }
    }
}
