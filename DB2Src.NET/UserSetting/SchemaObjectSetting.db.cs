using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static Db2Source.NpgsqlDataSet.TableReturnType;

namespace Db2Source
{
    partial class SchemaObjectSetting: ICloneable
    {
        private static readonly TableDefinition[] UserConfigDefinitions = new TableDefinition[] {
            new TableDefinition()
            {
                Name = "SCHEMA_OBJECT",
                Columns = new FieldDefinition[]
                {
                    new FieldDefinition("ID", SqliteDbType.Integer) { NotNull = true },
                    new FieldDefinition("UNIQNAME", SqliteDbType.Text) { NotNull = true },
                    new FieldDefinition("NAME", SqliteDbType.Text) { NotNull = true },
                    new FieldDefinition("OBJECT_TYPE", SqliteDbType.Text) { NotNull = true }
                },
                Constraints = new ConstraintDefinition[]
                {
                    new PrimaryKeyConstraintDefinition(new string[]{ "ID" }, false),
                }
            },
            new TableDefinition()
            {
                Name = "TABLE_SETTING",
                Columns = new FieldDefinition[]
                {
                    new FieldDefinition("OBJECT_ID", SqliteDbType.Integer) { NotNull = true },
                    new FieldDefinition("USE_LIMIT", SqliteDbType.Integer) { NotNull = true, DefaultExpr = "1" },
                    new FieldDefinition("LIMIT_COUNT", SqliteDbType.Integer) { NotNull = true, DefaultExpr = "100" },
                    new FieldDefinition("AUTO_FETCH", SqliteDbType.Integer) { NotNull = true, DefaultExpr = "1" },
                    new FieldDefinition("ALIAS", SqliteDbType.Text) { NotNull = true, DefaultExpr= "'a'" },
                },
                Constraints = new ConstraintDefinition[]
                {
                    new PrimaryKeyConstraintDefinition(new string[]{ "OBJECT_ID" }, false),
                }
            },
            new TableDefinition()
            {
                Name = "TABLE_JOIN_SETTING",
                Columns = new FieldDefinition[]
                {
                    new FieldDefinition("OBJECT_ID", SqliteDbType.Integer) { NotNull = true },
                    new FieldDefinition("TABLE_JOIN", SqliteDbType.Text) { NotNull = true },
                    new FieldDefinition("REFERENCE_TABLE", SqliteDbType.Text) { NotNull = true },
                    new FieldDefinition("IS_SELECTED", SqliteDbType.Integer) { NotNull = true, DefaultExpr = "1" },
                    new FieldDefinition("ALIAS", SqliteDbType.Text) { NotNull = true, DefaultExpr= "'a'" },
                },
                Constraints = new ConstraintDefinition[]
                {
                    new PrimaryKeyConstraintDefinition(new string[]{ "OBJECT_ID", "TABLE_JOIN" }, false),
                }
            },
            new TableDefinition()
            {
                Name = "TABLE_CONDITION_HISTORY",
                Columns = new FieldDefinition[]
                {
                    new FieldDefinition("OBJECT_ID", SqliteDbType.Integer) { NotNull = true },
                    new FieldDefinition("CONDITION", SqliteDbType.Text) { NotNull = true },
                    new FieldDefinition("LAST_EXECUTED", SqliteDbType.Real) { NotNull = true },
                },
                Constraints = new ConstraintDefinition[]
                {
                    new PrimaryKeyConstraintDefinition(new string[]{ "OBJECT_ID", "CONDITION" }, false),
                }
            },
            new TableDefinition()
            {
                Name = "FUNCTION_SETTING",
                Columns = new FieldDefinition[]
                {
                    new FieldDefinition("OBJECT_ID", SqliteDbType.Integer) { NotNull = true },
                    new FieldDefinition("PARAM_VALUES", SqliteDbType.Text) { NotNull = true },
                },
                Constraints = new ConstraintDefinition[]
                {
                    new PrimaryKeyConstraintDefinition(new string[]{ "OBJECT_ID" }, false),
                }
            },
        };
        private static bool _isDefinitionChecked = false;

        private static void CheckTableDefinition(SQLiteConnection connection)
        {
            if (_isDefinitionChecked)
            {
                return;
            }
            try
            {
                foreach (TableDefinition def in UserConfigDefinitions)
                {
                    try
                    {
                        def.Apply(connection);
                    }
                    catch (Exception t)
                    {
                        Logger.Default.Log(t.ToString());
                    }
                }
            }
            finally
            {
                _isDefinitionChecked = true;
            }
        }

        public static SQLiteConnection NewConnection()
        {
            string path = System.IO.Path.Combine(Db2SourceContext.AppDataDir, "UserSetting.db");
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = path,
                FailIfMissing = false,
                ReadOnly = false
            };
            SQLiteConnection connection = new SQLiteConnection(builder.ToString());
            connection.Open();
            CheckTableDefinition(connection);
            return connection;
        }

        internal static object ExecuteScalar(SQLiteConnection connection, string sql, params ParameterDef[] parameters)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (ParameterDef def in parameters)
                {
                    def.AddParameter(cmd);
                }
                return cmd.ExecuteScalar();
            }
        }

        internal static SQLiteDataReader ExecuteReader(SQLiteConnection connection, string sql, params ParameterDef[] parameters)
        {
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (ParameterDef def in parameters)
                {
                    def.AddParameter(cmd);
                }
                return cmd.ExecuteReader();
            }
        }

        private static Dictionary<long, SchemaObjectSetting> BackingStore = new Dictionary<long, SchemaObjectSetting>();

        internal static void RegisterBackingStore(SchemaObjectSetting value)
        {
            SchemaObjectSetting obj = (SchemaObjectSetting)value.Clone();
            BackingStore[obj.Id] = obj;
        }
        internal static bool UnregisterBackingStore(SchemaObjectSetting value)
        {
            return BackingStore.Remove(value.Id);
        }

        internal static SchemaObjectSetting GetBackingStore(SchemaObjectSetting value)
        {
            if (BackingStore.TryGetValue(value.Id, out SchemaObjectSetting ret))
            {
                return ret;
            }
            return null;
        }

        internal SchemaObjectSetting(SchemaObject target, SQLiteConnection connection)
        {
            bool needDispose = false;
            SQLiteConnection conn = connection;
            if (connection == null)
            {
                needDispose = true;
                conn = NewConnection();
            }
            try
            {
                Id = RequireId(target, conn);
            }
            finally
            {
                if (needDispose)
                {
                    conn.Dispose();
                }
            }
            FullIdentifier = target.FullIdentifier;
            Name = target.FullName;
        }
        internal static long RequireId(SchemaObject target, SQLiteConnection connection)
        {
            
            object o = ExecuteScalar(connection, "SELECT ID FROM SCHEMA_OBJECT WHERE UNIQNAME = @UNIQNAME", new ParameterDef("UNIQNAME", DbType.String, target.FullIdentifier));
            if (o == null || o is DBNull)
            {
                o = ExecuteScalar(connection, "INSERT INTO SCHEMA_OBJECT (UNIQNAME, NAME, OBJECT_TYPE) VALUES (@UNIQNAME, @NAME, @OBJTYPE) RETURNING ID",
                    new ParameterDef("UNIQNAME", DbType.String, target.FullIdentifier), 
                    new ParameterDef("NAME", DbType.String, target.FullName),
                    new ParameterDef("OBJTYPE", DbType.String, target.GetSqlType()));
            }
            if (o == null || o is DBNull)
            {
                throw new InvalidCastException();
            }
            return Convert.ToInt64(o);
        }

        public abstract object Clone();
    }

    partial class SchemaObjectSetting<T>
    {
        internal SchemaObjectSetting(SchemaObject target, SQLiteConnection connection) : base(target, connection) { }
    }
    partial class TableSetting
    {
        private static PropertyBindingDef<TableSetting>[] KeyColumns = new PropertyBindingDef<TableSetting>[]
        {
            new PropertyBindingDef<TableSetting>("OBJECT_ID", DbType.Int64, "Id"),
        };

        private static PropertyBindingDef<TableSetting>[] Columns = new PropertyBindingDef<TableSetting>[]
        {
            new PropertyBindingDef<TableSetting>("USE_LIMIT", DbType.Int32, "UseLimit"),
            new PropertyBindingDef<TableSetting>("LIMIT_COUNT", DbType.Int32, "LimitCount"),
            new PropertyBindingDef<TableSetting>("AUTO_FETCH", DbType.Int32, "AutoFetch"),
            new PropertyBindingDef<TableSetting>("ALIAS", DbType.String, "Alias"),
        };


        public static TableSetting Require(Table target)
        {
            if (target == null)
            {
                return null;
            }
            using (SQLiteConnection conn = NewConnection())
            {
                TableSetting setting = new TableSetting(target, conn);
                RegisterBackingStore(setting);
                return setting;
            }
        }

        internal TableSetting(Selectable target, SQLiteConnection connection) : base(target, connection)
        {
            ObjectWrapper<TableSetting>.Read(this, "TABLE_SETTING", Columns, KeyColumns, connection);
            using (SQLiteDataReader reader = ExecuteReader(connection, "SELECT TABLE_JOIN, REFERENCE_TABLE, IS_SELECTED, ALIAS FROM TABLE_JOIN_SETTING WHERE OBJECT_ID = @ID", new ParameterDef("ID", DbType.Int64, Id)))
            {
                while (reader.Read())
                {
                    TableJoinSetting setting = new TableJoinSetting(reader);
                    TableJoinSettings.Add(setting);
                }
            }
        }
        
        private void SaveChanges()
        {
            using (SQLiteConnection conn = NewConnection())
            {
                TableSetting old = GetBackingStore(this) as TableSetting;
                ObjectWrapper<TableSetting>.Write(this, old, "TABLE_SETTING", Columns, KeyColumns, conn);
            }
            RegisterBackingStore(this);
        }

        public override object Clone()
        {
            TableSetting setting = (TableSetting)MemberwiseClone();
            return setting;
        }
    }

    partial class ViewSetting
    {
        private static PropertyBindingDef<ViewSetting>[] KeyColumns = new PropertyBindingDef<ViewSetting>[]
        {
            new PropertyBindingDef<ViewSetting>("OBJECT_ID", DbType.Int64, "Id"),
        };

        private static PropertyBindingDef<ViewSetting>[] Columns = new PropertyBindingDef<ViewSetting>[]
        {
            new PropertyBindingDef<ViewSetting>("USE_LIMIT", DbType.Int32, "UseLimit"),
            new PropertyBindingDef<ViewSetting>("LIMIT_COUNT", DbType.Int32, "LimitCount"),
            new PropertyBindingDef<ViewSetting>("AUTO_FETCH", DbType.Int32, "AutoFetch"),
            new PropertyBindingDef<ViewSetting>("ALIAS", DbType.String, "Alias"),
        };


        public static ViewSetting Require(View target)
        {
            if (target == null)
            {
                return null;
            }
            using (SQLiteConnection conn = NewConnection())
            {
                ViewSetting setting = new ViewSetting(target, conn);
                RegisterBackingStore(setting);
                return setting;
            }
        }

        internal ViewSetting(View target, SQLiteConnection connection) : base(target, connection)
        {
            ObjectWrapper<ViewSetting>.Read(this, "TABLE_SETTING", Columns, KeyColumns, connection);
            using (SQLiteDataReader reader = ExecuteReader(connection, "SELECT TABLE_JOIN, REFERENCE_TABLE, IS_SELECTED, ALIAS FROM TABLE_JOIN_SETTING WHERE OBJECT_ID = @ID", new ParameterDef("ID", DbType.Int64, Id)))
            {
                while (reader.Read())
                {
                    TableJoinSetting setting = new TableJoinSetting(reader);
                    TableJoinSettings.Add(setting);
                }
            }
        }
        private void SaveChanges()
        {
            using (SQLiteConnection conn = NewConnection())
            {
                ViewSetting old = GetBackingStore(this) as ViewSetting;
                ObjectWrapper<ViewSetting>.Write(this, old, "TABLE_SETTING", Columns, KeyColumns, conn);
            }
            RegisterBackingStore(this);
        }


        public override object Clone()
        {
            ViewSetting setting = (ViewSetting)MemberwiseClone();
            return setting;
        }
    }

    partial class TableJoinSetting
    {
        internal TableJoinSetting(SQLiteDataReader reader)
        {
            TableJoin = reader.GetString(0);
            ReferenceTable = reader.GetString(1);
            IsSelected = reader.GetBoolean(2);
            Alias = reader.GetString(3);
        }
    }

    partial class StoredProcedureSetting
    {
        private static PropertyBindingDef<StoredProcedureSetting>[] KeyColumns = new PropertyBindingDef<StoredProcedureSetting>[]
        {
            new PropertyBindingDef<StoredProcedureSetting>("OBJECT_ID", DbType.Int64, "Id"),
        };

        private static PropertyBindingDef<StoredProcedureSetting>[] Columns = new PropertyBindingDef<StoredProcedureSetting>[]
        {
            new PropertyBindingDef<StoredProcedureSetting>("PARAM_VALUES", DbType.String, "ParamValuesCSV"),
        };
        public static StoredProcedureSetting Require(StoredFunction target)
        {
            if (target == null)
            {
                return null;
            }
            using (SQLiteConnection conn = NewConnection())
            {
                StoredProcedureSetting setting = new StoredProcedureSetting(target, conn);
                RegisterBackingStore(setting);
                return setting;
            }
        }

        internal StoredProcedureSetting(StoredFunction target, SQLiteConnection connection) : base(target, connection)
        {
            ObjectWrapper<StoredProcedureSetting>.Read(this, "FUNCTION_SETTING", Columns, KeyColumns, connection);
        }
        private void SaveChanges()
        {
            using (SQLiteConnection conn = NewConnection())
            {
                StoredProcedureSetting old = GetBackingStore(this) as StoredProcedureSetting;
                ObjectWrapper<StoredProcedureSetting>.Write(this, old, "FUNCTION_SETTING", Columns, KeyColumns, conn);
            }
            RegisterBackingStore(this);
        }

        public override object Clone()
        {
            StoredProcedureSetting setting = (StoredProcedureSetting)MemberwiseClone();
            if (ParamValues != null)
            {
                setting.ParamValues = new string[ParamValues.Length];
                for (int i = 0, n = ParamValues.Length; i < n; i++)
                {
                    setting.ParamValues[i] = ParamValues[i];
                }
            }
            return setting;
        }
    }
}
