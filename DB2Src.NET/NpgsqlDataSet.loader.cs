using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
//using System.ValueTuple.;
using System.Text;
using System.Text.RegularExpressions;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
//        private class NpgSqlTypeDefData
//        {
//#pragma warning disable 0649
//            public string data_type;
//            public int? character_maximum_length;
//            public int? character_octet_length;
//            public int? numeric_precision;
//            public int? numeric_precision_radix;
//            public int? numeric_scale;
//            public int? datetime_precision;
//            public string udt_name;
//#pragma warning restore 0649
//            private static void SetDbTypeDef(IDbTypeDef target, Type type, int? length, int? precision)
//            {
//                target.ValueType = type;
//                target.DataLength = length;
//                target.Precision = precision;
//            }

//            public void ApplyToDbTypeDef(IDbTypeDef target)
//            {
//                target.BaseType = data_type;
//                string bt = (udt_name != null) ? udt_name : data_type;
//                bt = (bt != null) ? bt.ToLower() : string.Empty;

//                //target.BaseType = udt_name != null ? udt_name : data_type;
//                target.IsSupportedType = true;
//                switch (bt)
//                {
//                    case "boolean":
//                        SetDbTypeDef(target, typeof(bool), null, null);
//                        break;
//                    case "smallint":
//                    case "integer":
//                    case "int2":
//                    case "int4":
//                        SetDbTypeDef(target, typeof(int), null, null);
//                        break;
//                    case "bigint":
//                    case "int8":
//                        SetDbTypeDef(target, typeof(long), null, null);
//                        break;
//                    case "real":
//                    case "float4":
//                        SetDbTypeDef(target, typeof(float), null, null);
//                        break;
//                    case "money":
//                        target.ValueType = typeof(decimal);
//                        break;
//                    case "text":
//                    case "citext":
//                    case "json":
//                    case "jsonb":
//                    case "xml":
//                        target.ValueType = typeof(string);
//                        break;
//                    case "character varying":
//                    case "nvarchar":
//                    case "character":
//                    case "char":
//                    case "\"char\"":
//                    case "name":
//                        SetDbTypeDef(target, typeof(string), character_maximum_length, null);
//                        break;
//                    case "double precision":
//                    case "numeric":
//                        SetDbTypeDef(target, typeof(double), numeric_precision, numeric_scale);
//                        break;
//                    case "point":
//                        SetDbTypeDef(target, typeof(NpgsqlPoint), null, null);
//                        break;
//                    case "lseg":
//                        SetDbTypeDef(target, typeof(NpgsqlLSeg), null, null);
//                        break;
//                    case "path":
//                        SetDbTypeDef(target, typeof(NpgsqlPath), null, null);
//                        break;
//                    case "polygon":
//                        SetDbTypeDef(target, typeof(NpgsqlPolygon), null, null);
//                        break;
//                    case "line":
//                        SetDbTypeDef(target, typeof(NpgsqlLine), null, null);
//                        break;
//                    case "circle":
//                        SetDbTypeDef(target, typeof(NpgsqlCircle), null, null);
//                        break;
//                    case "box":
//                        SetDbTypeDef(target, typeof(NpgsqlBox), null, null);
//                        break;
//                    case "bit":
//                        if (character_maximum_length == 1)
//                        {
//                            SetDbTypeDef(target, typeof(bool), null, null);
//                        }
//                        else
//                        {
//                            SetDbTypeDef(target, typeof(BitArray), character_maximum_length, null);
//                        }
//                        break;
//                    case "bit varying":
//                        SetDbTypeDef(target, typeof(BitArray), character_maximum_length, null);
//                        break;
//                    case "hstore":
//                        SetDbTypeDef(target, typeof(IDictionary<string, string>), null, null);
//                        break;
//                    case "uuid":
//                        SetDbTypeDef(target, typeof(Guid), null, null);
//                        break;
//                    //case "cidr":
//                    //    SetDbTypeDef(target, typeof(ValueTuple<IPAddress, int>), null, null);
//                    //    break;
//                    case "inet":
//                        SetDbTypeDef(target, typeof(IPAddress), null, null);
//                        break;
//                    case "macaddr":
//                        SetDbTypeDef(target, typeof(PhysicalAddress), null, null);
//                        break;
//                    case "tsquery":
//                        SetDbTypeDef(target, typeof(NpgsqlTsQuery), null, null);
//                        break;
//                    case "tsvector":
//                        SetDbTypeDef(target, typeof(NpgsqlTsVector), null, null);
//                        break;
//                    case "abstime":
//                    case "date":
//                        SetDbTypeDef(target, typeof(DateTime), null, null);
//                        break;
//                    case "timestamp":
//                    case "timestamp without time zone":
//                        target.BaseType = "timestamp";
//                        target.WithTimeZone = false;
//                        SetDbTypeDef(target, typeof(DateTime), datetime_precision, null);
//                        break;
//                    case "timestamp with time zone":
//                        target.BaseType = "timestamp";
//                        target.WithTimeZone = true;
//                        SetDbTypeDef(target, typeof(DateTime), datetime_precision, null);
//                        break;
//                    case "time":
//                    case "interval":
//                        SetDbTypeDef(target, typeof(TimeSpan), datetime_precision, null);
//                        break;
//                    case "time with time zone":
//                        target.BaseType = "time";
//                        target.WithTimeZone = true;
//                        target.ValueType = typeof(DateTimeOffset);
//                        break;
//                    case "bytea":
//                        SetDbTypeDef(target, typeof(byte[]), character_maximum_length, null);
//                        break;
//                    case "oid":
//                        SetDbTypeDef(target, typeof(uint), null, null);
//                        break;
//                    case "xid":
//                        SetDbTypeDef(target, typeof(uint), null, null);
//                        break;
//                    case "cid":
//                        SetDbTypeDef(target, typeof(uint), null, null);
//                        break;
//                    case "oidvector":
//                        SetDbTypeDef(target, typeof(uint[]), character_maximum_length, null);
//                        break;
//                    //case "geometry":
//                    //    target.ValueType = typeof(PostgisGeometry);
//                    //    break;
//                    case "record":
//                        SetDbTypeDef(target, typeof(object[]), character_maximum_length, null);
//                        break;
//                    //case "composite types":
//                    //    target.ValueType = typeof(T);
//                    //    break;
//                    //case "range subtypes":
//                    //    target.ValueType = typeof(NpgsqlTypes.NpgsqlRange<TElement>);
//                    //    break;
//                    //case "enum types":
//                    //    target.ValueType = typeof(TEnum);
//                    //    break;
//                    //case "array types":
//                    //case "anyarray":
//                    case "array":
//                        target.ValueType = typeof(string[]);
//                        break;
//                    //    target.ValueType = typeof(Array of child element type));
//                    //    break;
//                    case "void":
//                        SetDbTypeDef(target, null, null, null);
//                        target.IsSupportedType = false;
//                        break;
//                    //case "pg_node_tree":
//                    //case "regproc":
//                    //    target.DataLength = null;
//                    //    target.Precision = null;
//                    //    //ret.IsSupportedType = false;
//                    //    break;
//                    case "_xml":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_json":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_line":
//                        SetDbTypeDef(target, typeof(NpgsqlLine[]), null, null);
//                        break;
//                    case "_circle":
//                        SetDbTypeDef(target, typeof(NpgsqlCircle[]), null, null);
//                        break;
//                    case "_money":
//                        SetDbTypeDef(target, typeof(decimal[]), null, null);
//                        break;
//                    case "_bool":
//                        SetDbTypeDef(target, typeof(bool[]), null, null);
//                        break;
//                    case "_bytea":
//                        SetDbTypeDef(target, typeof(byte[][]), null, null);
//                        break;
//                    case "_char":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_name":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_int2":
//                        SetDbTypeDef(target, typeof(int[]), null, null);
//                        break;
//                    case "_int4":
//                        SetDbTypeDef(target, typeof(int[]), null, null);
//                        break;
//                    case "_text":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_oid":
//                        SetDbTypeDef(target, typeof(uint[]), null, null);
//                        break;
//                    case "_xid":
//                        SetDbTypeDef(target, typeof(uint[]), null, null);
//                        break;
//                    case "_cid":
//                        SetDbTypeDef(target, typeof(uint[]), null, null);
//                        break;
//                    case "_bpchar":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_varchar":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_int8":
//                        SetDbTypeDef(target, typeof(long[]), null, null);
//                        break;
//                    case "_point":
//                        SetDbTypeDef(target, typeof(NpgsqlPoint[]), null, null);
//                        break;
//                    case "_lseg":
//                        SetDbTypeDef(target, typeof(NpgsqlLSeg[]), null, null);
//                        break;
//                    case "_path":
//                        SetDbTypeDef(target, typeof(NpgsqlPath[]), null, null);
//                        break;
//                    case "_box":
//                        SetDbTypeDef(target, typeof(NpgsqlBox[]), null, null);
//                        break;
//                    case "_float4":
//                        SetDbTypeDef(target, typeof(float[]), null, null);
//                        break;
//                    case "_float8":
//                        SetDbTypeDef(target, typeof(double[]), null, null);
//                        break;
//                    case "_abstime":
//                        SetDbTypeDef(target, typeof(DateTime[]), null, null);
//                        break;
//                    case "_tinterval":
//                        SetDbTypeDef(target, typeof(TimeSpan[]), null, null);
//                        break;
//                    case "_polygon":
//                        SetDbTypeDef(target, typeof(NpgsqlPolygon[]), null, null);
//                        break;
//                    case "_macaddr":
//                        SetDbTypeDef(target, typeof(PhysicalAddress[]), null, null);
//                        break;
//                    case "_inet":
//                        SetDbTypeDef(target, typeof(IPAddress[]), null, null);
//                        break;
//                    case "_cstring":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_timestamp":
//                        SetDbTypeDef(target, typeof(DateTime[]), null, null);
//                        break;
//                    case "_date":
//                        SetDbTypeDef(target, typeof(DateTime[]), null, null);
//                        break;
//                    case "_time":
//                        SetDbTypeDef(target, typeof(TimeSpan[]), null, null);
//                        break;
//                    case "_timestamptz":
//                        SetDbTypeDef(target, typeof(TimeSpan[]), null, null);
//                        break;
//                    case "_interval":
//                        SetDbTypeDef(target, typeof(TimeSpan[]), null, null);
//                        break;
//                    case "_numeric":
//                        SetDbTypeDef(target, typeof(double[]), null, null);
//                        break;
//                    case "_timetz":
//                        SetDbTypeDef(target, typeof(TimeSpan[]), null, null);
//                        break;
//                    case "_bit":
//                        SetDbTypeDef(target, typeof(BitArray), null, null);
//                        break;
//                    case "_varbit":
//                        SetDbTypeDef(target, typeof(BitArray), null, null);
//                        break;
//                    case "_uuid":
//                        SetDbTypeDef(target, typeof(Guid[]), null, null);
//                        break;
//                    case "_tsvector":
//                        SetDbTypeDef(target, typeof(NpgsqlTsVector[]), null, null);
//                        break;
//                    case "_tsquery":
//                        SetDbTypeDef(target, typeof(NpgsqlTsQuery[]), null, null);
//                        break;
//                    case "_jsonb":
//                        SetDbTypeDef(target, typeof(string[]), null, null);
//                        break;
//                    case "_record":
//                        SetDbTypeDef(target, typeof(object[][]), null, null);
//                        break;
//                    default:
//                        target.DataLength = null;
//                        target.Precision = null;
//                        target.IsSupportedType = false;
//                        break;
//                }
//            }
//        }
//        private class ColumnData: NpgSqlTypeDefData
//        {
//#pragma warning disable 0649
//            public string table_catalog;
//            public string table_schema;
//            public string table_name;
//            public string column_name;
//            public int ordinal_position;
//            public string column_default;
//            public string is_nullable;
//            //public string data_type;
//            //public int? character_maximum_length;
//            //public int? character_octet_length;
//            //public int? numeric_precision;
//            //public int? numeric_precision_radix;
//            //public int? numeric_scale;
//            //public int? datetime_precision;
//            public int? interval_type;
//            public int? interval_precision;
//            public string character_set_catalog;
//            public string character_set_schema;
//            public string character_set_name;
//            public string collation_catalog;
//            public string collation_schema;
//            public string collation_name;
//            public string domain_catalog;
//            public string domain_schema;
//            public string domain_name;
//            public string udt_catalog;
//            public string udt_schema;
//            //public string udt_name;
//            public string scope_catalog;
//            public string scope_schema;
//            public string scope_name;
//            public string maximum_cardinality;
//            public string dtd_identifier;
//            public string is_self_referencing;
//            public string is_identity;
//            public string identity_generation;
//            public string identity_start;
//            public string identity_increment;
//            public string identity_maximum;
//            public string identity_minimum;
//            public string identity_cycle;
//            public string is_generated;
//            public string generation_expression;
//            public string is_updatable;
//#pragma warning restore 0649
//            public Column ToColumn(Db2SourceContext context)
//            {
//                if (context == null)
//                {
//                    throw new ArgumentNullException("context");
//                }
//                Column ret = new Column(context, table_schema);
//                ret.TableName = table_name;
//                ret.Index = ordinal_position;
//                ret.Name = column_name;
//                ApplyToDbTypeDef(ret);
//                ret.DefaultValue = column_default;
//                ret.NotNull = (is_nullable == "NO");
//                ret.UpdateDataType();
//                return ret;
//            }
//            public ColumnData() { }
//        }
//        private class TableData
//        {
//#pragma warning disable 0649
//            //"select c.oid, c.relname as tablename, c.relnamespace as schemaid, n.nspname schemaname,\r\n" +
//            //"  c.reltablespace as tablespaceid, t.spcname as tablespace,\r\n" +
//            //"  c.relowner as ownerid, pg_get_userbyid(c.relowner) as owner,\r\n" +
//            public uint? oid;
//            public string tablename;
//            public uint? schemaid;
//            public string schemaname;
//            public uint? tablespaceid;
//            public string tablespace;
//            public uint? ownerid;
//            public string owner;
//#pragma warning restore 0649

//            public Table ToTable(Db2SourceContext context)
//            {
//                if (context == null)
//                {
//                    throw new ArgumentNullException("context");
//                }
//                Table ret = new Table(context, owner, schemaname, tablename);
//                ret.TablespaceName = tablespace;
//                string s = (schemaid.HasValue && oid.HasValue) ? string.Format("^{0}_{1}_.+$", schemaid.Value, oid.Value) : string.Empty;
//                //string s = (string.IsNullOrEmpty(nspowner) && string.IsNullOrEmpty(relowner)) ? string.Format("^{0}_{1}_.+$", nspowner, relowner) : string.Empty;
//                ret.TemporaryNamePattern = new Regex(s);
//                return ret;
//            }
//        }
        //        private class UserData
        //        {
        //#pragma warning disable 0649
        //            public string usename;
        //            public int usesysid;
        //            public bool usecreatedb;
        //            public bool usesuper;
        //            public bool userepl;
        //            public bool usebypassrls;
        //#pragma warning restore 0649
        //            //public User ToUser(Db2SourceContext context)
        //            //{
        //            //    View ret = new View(context, schemaname, viewname);
        //            //    ret.Definition = definition;
        //            //    return ret;
        //            //}
        //        }

//        private class CommentData
//        {
//#pragma warning disable 0649
//            public string schemaname;
//            public string tablename;
//            public string columnname;
//            public string description;
//#pragma warning restore 0649
//            public Comment ToComment(Db2SourceContext context)
//            {
//                if (context == null)
//                {
//                    throw new ArgumentNullException("context");
//                }
//                if (!string.IsNullOrEmpty(columnname))
//                {
//                    ColumnComment ret = new ColumnComment(context, schemaname, tablename, columnname, description, true);
//                    ret.Link();
//                    return ret;
//                }
//                else
//                {
//                    Comment ret = new Comment(context, schemaname, tablename, description, true);
//                    ret.Link();
//                    return ret;
//                }
//            }
//        }

        //internal static FieldInfo[] CreateMapper(NpgsqlDataReader reader, Type typeInfo)
        //{
        //    FieldInfo[] ret = new FieldInfo[reader.FieldCount];
        //    for (int i = 0, n = reader.FieldCount; i < n; i++)
        //    {
        //        ret[i] = typeInfo.GetField(reader.GetName(i));
        //    }
        //    return ret;
        //}
        //internal static void ReadObject(object target, NpgsqlDataReader reader, FieldInfo[] fields)
        //{
        //    for (int i = 0, n = fields.Length; i < n; i++)
        //    {
        //        FieldInfo f = fields[i];
        //        if (f == null)
        //        {
        //            continue;
        //        }
        //        Type ft = f.FieldType;
        //        if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Nullable<>))
        //        {
        //            if (reader.IsDBNull(i))
        //            {
        //                f.SetValue(target, null);
        //                continue;
        //            }
        //            ft = ft.GetGenericArguments()[0];
        //        }

        //        if (ft == typeof(Int32))
        //        {
        //            f.SetValue(target, reader.GetInt32(i));
        //        }
        //        else if (ft == typeof(UInt32))
        //        {
                    
        //            f.SetValue(target, reader.GetFieldValue<UInt32>(i));
        //        }
        //        else if (ft == typeof(Int64))
        //        {
        //            f.SetValue(target, reader.GetInt64(i));
        //        }
        //        else if (ft == typeof(UInt64))
        //        {
        //            f.SetValue(target, reader.GetFieldValue<UInt32>(i));
        //        }
        //        else if (ft == typeof(bool))
        //        {
        //            f.SetValue(target, reader.GetBoolean(i));
        //        }
        //        else if (ft == typeof(char))
        //        {
        //            f.SetValue(target, reader.GetChar(i));
        //        }
        //        else if (ft == typeof(string))
        //        {
        //            if (reader.IsDBNull(i))
        //            {
        //                f.SetValue(target, null);
        //            }
        //            else
        //            {
        //                f.SetValue(target, reader.GetString(i));
        //            }
        //        }
        //        else if (ft.IsArray)
        //        {
        //            Type et = ft.GetElementType();
        //            if (reader.IsDBNull(i))
        //            {
        //                f.SetValue(target, null);
        //            }
        //            else if (et == typeof(byte))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<byte[]>(i));
        //            }
        //            else if (et == typeof(sbyte))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<sbyte[]>(i));
        //            }
        //            else if (et == typeof(Int16))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<Int16[]>(i));
        //            }
        //            else if (et == typeof(Int32))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<Int32[]>(i));
        //            }
        //            else if (et == typeof(UInt32))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<UInt32[]>(i));
        //            }
        //            else if (et == typeof(Int64))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<Int64[]>(i));
        //            }
        //            else if (et == typeof(UInt64))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<UInt64[]>(i));
        //            }
        //            else if (et == typeof(bool))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<bool[]>(i));
        //            }
        //            else if (et == typeof(string))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<string[]>(i));
        //            }
        //        }
        //        else if (ft.IsSubclassOf(typeof(IList)))
        //        {
        //            Type gt = ft.GetGenericTypeDefinition();
        //            if (gt == typeof(Int32))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<Int32[]>(i));
        //            }
        //            else if (gt == typeof(Int64))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<Int64[]>(i));
        //            }
        //            else if (gt == typeof(bool))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<bool[]>(i));
        //            }
        //            else if (gt == typeof(string))
        //            {
        //                f.SetValue(target, reader.GetFieldValue<string[]>(i));
        //            }
        //        }
        //        //else
        //        //{
        //        //    reader.GetFieldValue<ft>(i)
        //        //}
        //    }
        //}

        //private const string USERINFO_SQL =
        //    "select current_schema()";
        //private void LoadUserInfo(IDbConnection connection)
        //{
        //    NpgsqlConnection conn = connection as NpgsqlConnection;
        //    if (conn == null)
        //    {
        //        return;
        //    }
        //    DisableChangeLog();
        //    try
        //    {
        //        using (NpgsqlCommand cmd = new NpgsqlCommand(USERINFO_SQL, conn))
        //        {
        //            using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    CurrentSchema = reader.GetValue(0).ToString();
        //                }
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        EnableChangeLog();
        //    }
        //}

        //private const string COLUMN_SQL =
        //    "select * from information_schema.columns\r\n" +
        //    "where (table_schema = :schema or :schema is null)\r\n" +
        //    "  and (table_name = :object or :object is null)";
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="schema"></param>
        ///// <param name="tableIDbConnection"></param>
        ///// <param name=""></param>
        //public override void LoadColumn(string schema, string table, IDbConnection connection)
        //{
        //    NpgsqlConnection conn = connection as NpgsqlConnection;
        //    if (conn == null)
        //    {
        //        return;
        //    }
        //    DisableChangeLog();
        //    try
        //    {
        //        foreach (Schema sc in Schemas)
        //        {
        //            if (!string.IsNullOrEmpty(schema) && (sc.Name != schema))
        //            {
        //                continue;
        //            }
        //            for (int i = sc.Columns.Count - 1; 0 <= i; i--)
        //            {
        //                Column c = sc.Columns[i];
        //                if (string.IsNullOrEmpty(table) && (c.TableName != table))
        //                {
        //                    continue;
        //                }
        //                c.Release();
        //            }
        //        }
        //        using (NpgsqlCommand cmd = new NpgsqlCommand(COLUMN_SQL, conn))
        //        {
        //            cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
        //            cmd.Parameters.Add(new NpgsqlParameter("object", DbType.String));
        //            cmd.Parameters[0].Value = ToDbStr(schema, true);
        //            cmd.Parameters[1].Value = ToDbStr(table, true);
        //            using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                FieldInfo[] fields = CreateMapper(reader, typeof(ColumnData));
        //                while (reader.Read())
        //                {
        //                    ColumnData data = new ColumnData();
        //                    ReadObject(data, reader, fields);
        //                    data.ToColumn(this);
        //                }
        //            }
        //        }
        //        InvalidateColumns();
        //    }
        //    finally
        //    {
        //        EnableChangeLog();
        //    }
        //}

        //private const string TABLE_SQL =
        //    "select c.oid, c.relname as tablename, c.relnamespace as schemaid, n.nspname schemaname,\r\n" +
        //    "  c.reltablespace as tablespaceid, t.spcname as tablespace,\r\n" +
        //    "  c.relowner as ownerid, pg_get_userbyid(c.relowner) as owner\r\n" +
        //    "from pg_class c\r\n" +
        //    "  inner join pg_namespace n on c.relnamespace = n.oid\r\n" +
        //    "  left join pg_tablespace t on c.reltablespace = t.oid\r\n" +
        //    "where (c.relkind = 'r'::\"char\")\r\n" +
        //    "  and(not pg_is_other_temp_schema(n.oid))\r\n" +
        //    "  and(pg_has_role(c.relowner, 'USAGE'::text)\r\n" +
        //    "    or has_table_privilege(c.oid, 'SELECT, INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER'::text)\r\n" +
        //    "    or has_any_column_privilege(c.oid, 'SELECT, INSERT, UPDATE, REFERENCES'::text))";

        //public override void LoadTable(string schema, string table, IDbConnection connection)
        //{
        //    NpgsqlConnection conn = connection as NpgsqlConnection;
        //    if (conn == null)
        //    {
        //        return;
        //    }
        //    DisableChangeLog();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(table))
        //        {
        //            Tables.ReleaseAll(schema);
        //        }
        //        else
        //        {
        //            Table obj = Tables[schema, table];
        //            if (obj != null)
        //            {
        //                obj.Release();
        //            }
        //        }
        //        using (NpgsqlCommand cmd = new NpgsqlCommand(TABLE_SQL, conn))
        //        {
        //            cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
        //            cmd.Parameters.Add(new NpgsqlParameter("table", DbType.String));
        //            cmd.Parameters[0].Value = ToDbStr(schema, true);
        //            cmd.Parameters[1].Value = ToDbStr(table, true);
        //            using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                FieldInfo[] fields = CreateMapper(reader, typeof(TableData));
        //                while (reader.Read())
        //                {
        //                    TableData data = new TableData();
        //                    ReadObject(data, reader, fields);
        //                    data.ToTable(this);
        //                }
        //            }
        //        }
        //        InvalidateColumns();
        //        InvalidateConstraints();
        //    }
        //    finally
        //    {
        //        EnableChangeLog();
        //    }
        //}

//        private class ConstraintData
//        {
//#pragma warning disable 0649
//            public string constraint_schema;
//            public string constraint_name;
//            public string table_owner;
//            public string table_schema;
//            public string table_name;
//            public string constraint_type;
//            public string check_clause;
//            public string unique_constraint_schema;
//            public string unique_constraint_name;
//            public string match_option;
//            public string update_rule;
//            public string delete_rule;
//            public string temporary_prefix;
//#pragma warning restore 0649
//            private static Dictionary<string, ForeignKeyRule> InitStrToRule()
//            {
//                Dictionary<string, ForeignKeyRule> dict = new Dictionary<string, ForeignKeyRule>();
//                dict.Add("NO ACTION", ForeignKeyRule.NoAction);
//                dict.Add("RESTRICT", ForeignKeyRule.Restrict);
//                dict.Add("CASCADE", ForeignKeyRule.Cascade);
//                dict.Add("SET NULL", ForeignKeyRule.SetNull);
//                return dict;
//            }
//            private static readonly Dictionary<string, ForeignKeyRule> StrToRule = InitStrToRule();


//            public Constraint ToConstraint(Db2SourceContext context)
//            {
//                bool isNoName = constraint_name.StartsWith(temporary_prefix);
//                switch (constraint_type)
//                {
//                    case "PRIMARY KEY":
//                        return new KeyConstraint(context, table_owner, constraint_schema, constraint_name, table_schema, table_name, true, isNoName);
//                    case "UNIQUE":
//                        return new KeyConstraint(context, table_owner, constraint_schema, constraint_name, table_schema, table_name, false, isNoName);
//                    case "FOREIGN KEY":
//                        return new ForeignKeyConstraint(context, table_owner, constraint_schema, constraint_name, table_schema, table_name,
//                            unique_constraint_schema, unique_constraint_name, StrToRule[update_rule], StrToRule[delete_rule], isNoName);
//                    case "CHECK":
//                        return new CheckConstraint(context, table_owner, constraint_schema, constraint_name, table_schema, table_name, check_clause, isNoName);
//                }
//                return null;
//            }
//        }

//        private class ConstraintKeyColumnData
//        {
//#pragma warning disable 0649
//            public string constraint_schema;
//            public string constraint_name;
//            public string table_schema;
//            public string table_name;
//            public string column_name;
//            public int ordinal_position;
//            public int? position_in_unique_constraint;
//#pragma warning restore 0649
//        }

        //private const string CONSTRAINT_SQL =
        //    "select tc.constraint_schema, tc.constraint_name, tc.table_schema, tc.table_name, tc.constraint_type,\r\n" +
        //    "  cc.check_clause, rc.unique_constraint_schema, rc.unique_constraint_name, rc.match_option, rc.update_rule, rc.delete_rule,\r\n" +
        //    "  concat(cast(cls.relnamespace as text), '_', cast(cls.relfilenode as text), '_') as temporary_prefix\r\n" +
        //    "from information_schema.table_constraints tc\r\n" +
        //    "  left outer join information_schema.check_constraints cc on (tc.constraint_schema = cc.constraint_schema and tc.constraint_name = cc.constraint_name)\r\n" +
        //    "  left outer join information_schema.referential_constraints rc on (tc.constraint_schema = rc.constraint_schema and tc.constraint_name = rc.constraint_name)\r\n" +
        //    "  left outer join pg_catalog.pg_namespace ns on (tc.table_schema = ns.nspname)\r\n" +
        //    "  left outer join pg_catalog.pg_class cls on (ns.nspowner = cls.relowner and tc.table_name = cls.relname)\r\n" +
        //    "where (table_schema = :schema or :schema is null)\r\n" +
        //    "  and (table_name = :table or :table is null)\r\n" +
        //    "  and not (tc.constraint_type = 'CHECK' and tc.constraint_name like concat(cast(cls.relnamespace as text), '_', cast(cls.relfilenode as text), '_%_not_null'))";
        //"select tc.constraint_schema, tc.constraint_name, tc.table_schema, tc.table_name, tc.constraint_type,\r\n" +
        //"  cc.check_clause, rc.unique_constraint_schema, rc.unique_constraint_name, rc.match_option, rc.update_rule, rc.delete_rule\r\n" +
        //"from information_schema.table_constraints tc\r\n" +
        //"  left outer join information_schema.check_constraints cc on (tc.constraint_schema = cc.constraint_schema and tc.constraint_name = cc.constraint_name)\r\n" +
        //"  left outer join information_schema.referential_constraints rc on (tc.constraint_schema = rc.constraint_schema and tc.constraint_name = rc.constraint_name)\r\n" +
        //"where (table_schema = :schema or :schema is null)\r\n" +
        //"  and (table_name = :table or :table is null)\r\n" +
        //"  and not (tc.constraint_type = 'CHECK' and tc.constraint_name like '%_not_null')";
        //private const string KEY_CONS_COLUMN_SQL =
        //    "select constraint_schema, constraint_name, table_schema, table_name, column_name, ordinal_position, position_in_unique_constraint\r\n" +
        //    "from information_schema.key_column_usage\r\n" +
        //    "where (table_schema = :schema or :schema is null)\r\n" +
        //    "  and (table_name = :table or :table is null)\r\n" +
        //    "order by table_schema, table_name, constraint_schema, constraint_name, ordinal_position";
        //public override void LoadConstraint(string schema, string table, IDbConnection connection)
        //{
        //    NpgsqlConnection conn = connection as NpgsqlConnection;
        //    if (conn == null)
        //    {
        //        return;
        //    }
        //    using (NpgsqlCommand cmd = new NpgsqlCommand(CONSTRAINT_SQL, conn))
        //    {
        //        cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
        //        cmd.Parameters.Add(new NpgsqlParameter("table", DbType.String));
        //        cmd.Parameters[0].Value = ToDbStr(schema, true);
        //        cmd.Parameters[1].Value = ToDbStr(table, true);
        //        using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //        {
        //            FieldInfo[] fields = CreateMapper(reader, typeof(ConstraintData));
        //            while (reader.Read())
        //            {
        //                ConstraintData data = new ConstraintData();
        //                ReadObject(data, reader, fields);
        //                data.ToConstraint(this);
        //            }
        //        }
        //    }
        //    using (NpgsqlCommand cmd = new NpgsqlCommand(KEY_CONS_COLUMN_SQL, conn))
        //    {
        //        cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
        //        cmd.Parameters.Add(new NpgsqlParameter("table", DbType.String));
        //        cmd.Parameters[0].Value = ToDbStr(schema, true);
        //        cmd.Parameters[1].Value = ToDbStr(table, true);
        //        using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //        {
        //            FieldInfo[] fields = CreateMapper(reader, typeof(ConstraintKeyColumnData));
        //            ColumnsConstraint lastCons = null;
        //            List<string> cols = new List<string>();
        //            while (reader.Read())
        //            {
        //                ConstraintKeyColumnData data = new ConstraintKeyColumnData();
        //                ReadObject(data, reader, fields);
        //                if (lastCons == null)
        //                {
        //                    lastCons = Constraints[data.constraint_schema, data.constraint_name] as ColumnsConstraint;
        //                    cols = new List<string>();
        //                }
        //                else if (lastCons.SchemaName != data.constraint_schema || lastCons.Name != data.constraint_name)
        //                {
        //                    lastCons.Columns = cols.ToArray();
        //                    lastCons = Constraints[data.constraint_schema, data.constraint_name] as ColumnsConstraint;
        //                    cols = new List<string>();
        //                }
        //                cols.Add(data.column_name);
        //            }
        //            if (lastCons != null)
        //            {
        //                lastCons.Columns = cols.ToArray();
        //            }
        //        }
        //    }
        //    InvalidateConstraints();
        //}

//        private class TriggerData
//        {
//#pragma warning disable 0649
//            public string trigger_catalog;
//            public string trigger_schema;
//            public string trigger_name;
//            public string event_manipulation;
//            public string event_object_owner;
//            public string event_object_catalog;
//            public string event_object_schema;
//            public string event_object_table;
//            public int? action_order;
//            public string action_condition;
//            public string action_statement;
//            public string action_orientation;
//            public string action_timing;
//            public string action_reference_old_table;
//            public string action_reference_new_table;
//            public string action_reference_old_row;
//            public string action_reference_new_row;
//            public DateTime? created;
//#pragma warning restore 0649
//            private static Dictionary<string, TriggerTiming> InitStrToTriggerTiming()
//            {
//                Dictionary<string, TriggerTiming> dict = new Dictionary<string, TriggerTiming>();
//                dict.Add("AFTER", TriggerTiming.After);
//                dict.Add("BEFORE", TriggerTiming.Before);
//                dict.Add("INSTEAD OF", TriggerTiming.InsteadOf);
//                return dict;
//            }
//            private static readonly Dictionary<string, TriggerTiming> StrToTriggerTiming = InitStrToTriggerTiming();
//            private static TriggerTiming ToTriggerTiming(string value)
//            {
//                if (string.IsNullOrEmpty(value))
//                {
//                    return TriggerTiming.Unknown;
//                }
//                TriggerTiming t;
//                if (StrToTriggerTiming.TryGetValue(value.ToUpper(), out t))
//                {
//                    return t;
//                }
//                return TriggerTiming.Unknown;
//            }

//            private static string[] InitTriggerEventStr()
//            {
//                string[] ret = new string[32];
//                for (int i = 0; i < 32; i++)
//                {
//                    StringBuilder buf = new StringBuilder();
//                    bool needOr = false;
//                    if ((i & (int)TriggerEvent.Insert) != 0)
//                    {
//                        if (needOr)
//                        {
//                            buf.Append(" or ");
//                        }
//                        buf.Append("insert");
//                        needOr = true;
//                    }
//                    if ((i & (int)TriggerEvent.Delete) != 0)
//                    {
//                        if (needOr)
//                        {
//                            buf.Append(" or ");
//                        }
//                        buf.Append("delete");
//                        needOr = true;
//                    }
//                    if ((i & (int)TriggerEvent.Truncate) != 0)
//                    {
//                        if (needOr)
//                        {
//                            buf.Append(" or ");
//                        }
//                        buf.Append("truncate");
//                        needOr = true;
//                    }
//                    if ((i & (int)TriggerEvent.Update) != 0)
//                    {
//                        if (needOr)
//                        {
//                            buf.Append(" or ");
//                        }
//                        buf.Append("update");
//                        needOr = true;
//                    }
//                    ret[i] = buf.ToString();
//                }
//                return ret;
//            }
//            private static readonly string[] TriggerEventStr = InitTriggerEventStr();

//            private static Dictionary<string, TriggerEvent> InitStrToTriggerEvent()
//            {
//                Dictionary<string, TriggerEvent> dict = new Dictionary<string, Db2Source.TriggerEvent>();
//                dict.Add("INSERT", TriggerEvent.Insert);
//                dict.Add("DELETE", TriggerEvent.Delete);
//                dict.Add("TRUNCATE", TriggerEvent.Truncate);
//                dict.Add("UPDATE", TriggerEvent.Update);
//                return dict;
//            }
//            private static readonly Dictionary<string, TriggerEvent> StrToTriggerEvent = InitStrToTriggerEvent();
//            private static TriggerEvent ToTriggerEvent(string value)
//            {
//                if (string.IsNullOrEmpty(value))
//                {
//                    return TriggerEvent.Unknown;
//                }
//                TriggerEvent t;
//                if (StrToTriggerEvent.TryGetValue(value.ToUpper(), out t))
//                {
//                    return t;
//                }
//                return TriggerEvent.Unknown;
//            }

//            private static Dictionary<string, TriggerOrientation> InitStrToTriggerOrientation()
//            {
//                Dictionary<string, TriggerOrientation> dict = new Dictionary<string, TriggerOrientation>();
//                dict.Add("ROW", TriggerOrientation.Row);
//                dict.Add("STATEMENT", TriggerOrientation.Statement);
//                return dict;
//            }
//            private static readonly Dictionary<string, TriggerOrientation> StrToTriggerOrientation = InitStrToTriggerOrientation();
//            private TriggerOrientation ToTriggerOrientation(string value)
//            {
//                if (string.IsNullOrEmpty(value))
//                {
//                    return TriggerOrientation.Statement;
//                }

//                TriggerOrientation t;
//                if (StrToTriggerOrientation.TryGetValue(value.ToUpper(), out t))
//                {
//                    return t;
//                }
//                return TriggerOrientation.Unknown;
//            }

//            private static string GetTriggerEventStr(TriggerEvent events)
//            {
//                if ((int)events < 0 || TriggerEventStr.Length <= (int)events)
//                {
//                    return string.Empty;
//                }
//                return TriggerEventStr[(int)events];
//            }
//            public Trigger ToTrigger(Db2SourceContext context, Trigger trigger)
//            {
//                if ((trigger != null) && (trigger.SchemaName == trigger_schema) && (trigger.Name == trigger_name))
//                {
//                    trigger.Event |= ToTriggerEvent(event_manipulation);
//                    trigger.EventText = GetTriggerEventStr(trigger.Event);
//                    return trigger;
//                }
//                return new Trigger(context, event_object_owner, trigger_schema, trigger_name, event_object_schema, event_object_table, action_statement, true)
//                {
//                    TimingText = action_timing,
//                    Timing = ToTriggerTiming(action_timing),
//                    EventText = event_manipulation.ToLower(),
//                    Event = ToTriggerEvent(event_manipulation),
//                    OrientationText = action_orientation.ToLower(),
//                    Orientation = ToTriggerOrientation(action_orientation),
//                    ReferenceNewTable = action_reference_new_table,
//                    ReferenceOldTable = action_reference_old_table,
//                    Condition = action_condition,
//                    ReferenceNewRow = action_reference_new_row,
//                    ReferenceOldRow = action_reference_old_row,
//                };
//            }
//        }
//        private const string TRIGGER_SQL = "select \"trigger_catalog\", \"trigger_schema\", \"trigger_name\", event_manipulation, event_object_catalog,\r\n" +
//            "  event_object_schema, event_object_table, action_order, action_condition, action_statement,\r\n" +
//            "  action_orientation, action_timing, action_reference_old_table, action_reference_new_table,\r\n" +
//            "  action_reference_old_row, action_reference_new_row, created\r\n" +
//            "from information_schema.triggers\r\n" +
//            "where (event_object_schema = :schema or :schema is null)\r\n" +
//            "  and (event_object_table = :table or :table is null)\r\n" +
//            "order by \"trigger_schema\", \"trigger_name\", event_manipulation";

//        private class TriggerUpdateColumnData
//        {
//#pragma warning disable 0649
//            public string trigger_schema;
//            public string trigger_name;
//            public string event_object_catalog;
//            public string event_object_schema;
//            public string event_object_table;
//            public string event_object_column;
//#pragma warning restore 0649
//            public void AddToTrigger(Db2SourceContext dataSet)
//            {
//                Trigger trigger = dataSet.Triggers[trigger_schema, trigger_name];
//                if (trigger == null)
//                {
//                    return;
//                }
//                trigger.UpdateEventColumns.Add(event_object_column);
//            }
//        }
//        private const string TRIGGER_UPDATE_COLUMNS_SQL = "select uc.\"trigger_catalog\", uc.\"trigger_schema\", uc.\"trigger_name\",\r\n" +
//            "  uc.event_object_catalog, uc.event_object_schema, uc.event_object_table, uc.event_object_column, c.ordinal_position\r\n" +
//            "from information_schema.triggered_update_columns uc\r\n" +
//            "  inner join information_schema.columns c\r\n" +
//            "    on (uc.event_object_schema = c.table_schema and uc.event_object_table = c.table_name and uc.event_object_column = c.column_name)\r\n" +
//            "where (uc.event_object_schema = :schema or :schema is null)\r\n" +
//            "  and (uc.event_object_table = :table or :table is null)\r\n" +
//            "order by uc.\"trigger_schema\", uc.\"trigger_name\", uc.event_object_schema, uc.event_object_table, c.ordinal_position, uc.event_object_column";
//        public override void LoadTrigger(string schema, string table, IDbConnection connection)
//        {
//            NpgsqlConnection conn = connection as NpgsqlConnection;
//            if (conn == null)
//            {
//                return;
//            }
//            DisableChangeLog();
//            try
//            {
//                if (string.IsNullOrEmpty(table))
//                {
//                    Triggers.ReleaseAll(schema);
//                }
//                else
//                {
//                    SchemaObject obj = Objects[schema, table];
//                    if (obj != null)
//                    {
//                        obj.Release();
//                    }
//                }
//                using (NpgsqlCommand cmd = new NpgsqlCommand(TRIGGER_SQL, conn))
//                {
//                    cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                    cmd.Parameters.Add(new NpgsqlParameter("table", DbType.String));
//                    cmd.Parameters[0].Value = ToDbStr(schema, true);
//                    cmd.Parameters[1].Value = ToDbStr(table, true);
//                    Trigger prev = null;
//                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        FieldInfo[] fields = CreateMapper(reader, typeof(TriggerData));
//                        while (reader.Read())
//                        {
//                            TriggerData data = new TriggerData();
//                            ReadObject(data, reader, fields);
//                            prev = data.ToTrigger(this, prev);
//                        }
//                    }
//                }
//                InvalidateTriggers();
//                using (NpgsqlCommand cmd = new NpgsqlCommand(TRIGGER_UPDATE_COLUMNS_SQL, conn))
//                {
//                    cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                    cmd.Parameters.Add(new NpgsqlParameter("table", DbType.String));
//                    cmd.Parameters[0].Value = ToDbStr(schema, true);
//                    cmd.Parameters[1].Value = ToDbStr(table, true);
//                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        FieldInfo[] fields = CreateMapper(reader, typeof(TriggerUpdateColumnData));
//                        while (reader.Read())
//                        {
//                            TriggerUpdateColumnData data = new TriggerUpdateColumnData();
//                            ReadObject(data, reader, fields);
//                            data.AddToTrigger(this);
//                        }
//                    }
//                }
//            }
//            finally
//            {
//                EnableChangeLog();
//            }
//        }

//        private class ViewData
//        {
//#pragma warning disable 0649
//            public string schemaname;
//            public string viewname;
//            public string viewowner;
//            public string definition;
//#pragma warning restore 0649
//            public View ToView(Db2SourceContext context)
//            {
//                if (context == null)
//                {
//                    throw new ArgumentNullException("context");
//                }
//                View ret = new View(context, viewowner, schemaname, viewname, definition, true);
//                return ret;
//            }
//        }
//        private const string VIEW_SQL =
//            "select v.*\r\n" +
//            "from pg_views v\r\n" +
//            "  inner join information_schema.tables t on (v.schemaname = t.table_schema and v.viewname = t.table_name)\r\n" +
//            "where (v.schemaname = :schema or :schema is null)\r\n" +
//            "  and (v.viewname = :view or :view is null)";
//        public override void LoadView(string schema, string view, IDbConnection connection)
//        {
//            NpgsqlConnection conn = connection as NpgsqlConnection;
//            if (conn == null)
//            {
//                return;
//            }
//            DisableChangeLog();
//            try
//            {
//                if (string.IsNullOrEmpty(view))
//                {
//                    Views.ReleaseAll(schema);
//                }
//                else
//                {
//                    View obj = Views[schema, view];
//                    if (obj != null)
//                    {
//                        obj.Release();
//                    }
//                }
//                using (NpgsqlCommand cmd = new NpgsqlCommand(VIEW_SQL, conn))
//                {
//                    cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                    cmd.Parameters.Add(new NpgsqlParameter("view", DbType.String));
//                    cmd.Parameters[0].Value = ToDbStr(schema, true);
//                    cmd.Parameters[1].Value = ToDbStr(view, true);
//                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        FieldInfo[] fields = CreateMapper(reader, typeof(ViewData));
//                        while (reader.Read())
//                        {
//                            ViewData data = new ViewData();
//                            ReadObject(data, reader, fields);
//                            data.ToView(this);
//                        }
//                    }
//                }
//                InvalidateColumns();
//            }
//            finally
//            {
//                EnableChangeLog();
//            }
//        }

//        private const string USER_SQL =
//            "select * from pg_user";
//        public override void LoadUser(IDbConnection connection)
//        {
//            NpgsqlConnection conn = connection as NpgsqlConnection;
//            if (conn == null)
//            {
//                return;
//            }
//            DisableChangeLog();
//            try
//            {
//                //using (NpgsqlCommand cmd = new NpgsqlCommand(USER_SQL, conn))
//                //{
//                //    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                //    {
//                //        FieldInfo[] fields = CreateMapper(reader, typeof(UserData));
//                //        while (reader.Read())
//                //        {
//                //            UserData data = new UserData();
//                //            ReadObject(data, reader, fields);
//                //            data.ToView(this);
//                //        }
//                //    }
//                //}
//            }
//            finally
//            {
//                EnableChangeLog();
//            }
//        }

//        private const string COMMENT_SQL =
//            "select psat.schemaname, psat.relname as tablename, pa.attname as columnname, pd.description\r\n" +
//            "from pg_stat_all_tables psat\r\n" +
//            "  inner join pg_description pd on (psat.relid = pd.objoid)\r\n" +
//            "  left outer join pg_attribute pa on (pd.objoid = pa.attrelid and pd.objsubid= pa.attnum)\r\n" +
//            "where (psat.schemaname = :schema or :schema is null)\r\n" +
//            "  and (psat.relname = :object or :object is null)";
//        public override void LoadComment(string schema, string objectName, IDbConnection connection)
//        {
//            NpgsqlConnection conn = connection as NpgsqlConnection;
//            if (conn == null)
//            {
//                return;
//            }
//            DisableChangeLog();
//            try
//            {
//                using (NpgsqlCommand cmd = new NpgsqlCommand(COMMENT_SQL, conn))
//                {
//                    cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                    cmd.Parameters.Add(new NpgsqlParameter("object", DbType.String));
//                    cmd.Parameters[0].Value = ToDbStr(schema, true);
//                    cmd.Parameters[1].Value = ToDbStr(objectName, true);
//                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        FieldInfo[] fields = CreateMapper(reader, typeof(CommentData));
//                        while (reader.Read())
//                        {
//                            CommentData data = new CommentData();
//                            ReadObject(data, reader, fields);
//                            data.ToComment(this);
//                        }
//                    }
//                }
//            }
//            finally
//            {
//                EnableChangeLog();
//            }
//        }

//        private class RoutineData: NpgSqlTypeDefData
//        {
//#pragma warning disable 0649
//            public string specific_catalog;
//            public string specific_owner;
//            public string specific_schema;
//            public string specific_name;
//            public string routine_catalog;
//            public string routine_schema;
//            public string routine_name;
//            public string routine_type;
//            public string module_catalog;
//            public string module_schema;
//            public string module_name;
//            public string udt_catalog;
//            public string udt_schema;
//            //public string udt_name;
//            //public string data_type;
//            //public int? character_maximum_length;
//            //public int? character_octet_length;
//            public string character_set_catalog;
//            public string character_set_schema;
//            public string character_set_name;
//            public string collation_catalog;
//            public string collation_schema;
//            public string collation_name;
//            //public int? numeric_precision;
//            //public int? numeric_precision_radix;
//            //public int? numeric_scale;
//            //public int? datetime_precision;
//            public string interval_type;
//            public int? interval_precision;
//            public string type_udt_catalog;
//            public string type_udt_schema;
//            public string type_udt_name;
//            public string scope_catalog;
//            public string scope_schema;
//            public string scope_name;
//            public int? maximum_cardinality;
//            public string dtd_identifier;
//            public string routine_body;
//            public string routine_definition;
//            public string external_name;
//            public string external_language;
//            public string parameter_style;
//            public string is_deterministic;
//            public string sql_data_access;
//            public string is_null_call;
//            public string sql_path;
//            public string schema_level_routine;
//            public int? max_dynamic_result_sets;
//            public string is_user_defined_cast;
//            public string is_implicitly_invocable;
//            public string security_type;
//            public string to_sql_specific_catalog;
//            public string to_sql_specific_schema;
//            public string to_sql_specific_name;
//            public string as_locator;
//            public DateTime? created;
//            public DateTime? last_altered;
//            public string new_savepoint_level;
//            public string is_udt_dependent;
//            public string result_cast_from_data_type;
//            public string result_cast_as_locator;
//            public int? result_cast_char_max_length;
//            public int? result_cast_char_octet_length;
//            public string result_cast_char_set_catalog;
//            public string result_cast_char_set_schema;
//            //public string result_cast_character_set_name;
//            public string result_cast_collation_catalog;
//            public string result_cast_collation_schema;
//            public string result_cast_collation_name;
//            public int? result_cast_numeric_precision;
//            public int? result_cast_numeric_precision_radix;
//            public int? result_cast_numeric_scale;
//            public int? result_cast_datetime_precision;
//            public string result_cast_interval_type;
//            public int? result_cast_interval_precision;
//            public string result_cast_type_udt_catalog;
//            public string result_cast_type_udt_schema;
//            public string result_cast_type_udt_name;
//            public string result_cast_scope_catalog;
//            public string result_cast_scope_schema;
//            public string result_cast_scope_name;
//            public int? result_cast_maximum_cardinality;
//            public string result_cast_dtd_identifier;
//#pragma warning restore 0649
//            public StoredFunction ToStoredFunction(Db2SourceContext dataSet)
//            {
//                StoredFunction ret = new StoredFunction(dataSet, specific_owner, specific_schema, routine_name, specific_name, routine_definition, true);
//                ApplyToDbTypeDef(ret);
//                ret.Language = external_language?.ToLower();
//                ret.UpdateDataType();
//                return ret;
//            }
//        }

//        private class ParameterData : NpgSqlTypeDefData
//        {
//#pragma warning disable 0649
//            public string specific_catalog;
//            public string specific_owner;
//            public string specific_schema;
//            public string specific_name;
//            public int ordinal_position;
//            public string parameter_mode;
//            public string is_result;
//            public string as_locator;
//            public string parameter_name;
//            //public string data_type;
//            //public int? character_maximum_length;
//            //public int? character_octet_length;
//            public string character_set_catalog;
//            public string character_set_schema;
//            public string character_set_name;
//            public string collation_catalog;
//            public string collation_schema;
//            public string collation_name;
//            //public string numeric_precision;
//            //public string numeric_precision_radix;
//            //public string numeric_scale;
//            //public string datetime_precision;
//            public string interval_type;
//            public string interval_precision;
//            public string udt_catalog;
//            public string udt_schema;
//            //public string udt_name;
//            public string scope_catalog;
//            public string scope_schema;
//            public string scope_name;
//            public string maximum_cardinality;
//            public string dtd_identifier;
//            public string parameter_default;
//#pragma warning restore 0649

//            private static Dictionary<string, ParameterDirection> InitStrToParameterDirection()
//            {
//                Dictionary<string, ParameterDirection> dict = new Dictionary<string, ParameterDirection>();
//                dict.Add("IN", ParameterDirection.Input);
//                dict.Add("OUT", ParameterDirection.Output);
//                dict.Add("INOUT", ParameterDirection.InputOutput);
//                return dict;
//            }
//            private static readonly Dictionary<string, ParameterDirection> StrToParameterDirection = InitStrToParameterDirection();
//            private ParameterDirection GetParameterDirection()
//            {
//                ParameterDirection ret;
//                if (!StrToParameterDirection.TryGetValue(parameter_mode, out ret))
//                {
//                    return ParameterDirection.Input;
//                }
//                return ret;
//            }
//            public void AddToStoredFunction(Db2SourceContext dataSet)
//            {
//                StoredFunction fn = dataSet.StoredFunctions[specific_schema, specific_name];
//                if (fn == null)
//                {
//                    return;
//                }
//                Parameter p = new Parameter(fn);
//                p.Name = parameter_name;
//                p.Direction = GetParameterDirection();
//                p.Index = ordinal_position;
//                ApplyToDbTypeDef(p);
//                p.UpdateDataType();
//                p.DefaultValue = parameter_default;
//            }
//        }

//        private const string PARAMETER_SQL = "select\r\n" +
//            "  specific_catalog, specific_schema, \"specific_name\", ordinal_position, \"parameter_mode\", \r\n" +
//            "  is_result, as_locator, \"parameter_name\", data_type, character_maximum_length, \r\n" +
//            "  character_octet_length, \"character_set_catalog\", \"character_set_schema\", \"character_set_name\", \r\n" +
//            "  \"collation_catalog\", \"collation_schema\", \"collation_name\", numeric_precision, \r\n" +
//            "  numeric_precision_radix, numeric_scale, datetime_precision, interval_type, interval_precision, \r\n" +
//            "  udt_catalog, udt_schema, udt_name, \"scope_catalog\", \"scope_schema\", \"scope_name\", \r\n" +
//            "  maximum_cardinality, dtd_identifier, parameter_default\r\n" +
//            "from information_schema.\"parameters\"\r\n" +
//            "where (specific_schema = :schema or :schema is null)\r\n" +
//            "  and (\"specific_name\" = :object or :object is null)\r\n" +
//            "order by specific_catalog, specific_schema, \"specific_name\", ordinal_position";
//        private void LoadStoredFunctionParameters(string schema, string objectName, NpgsqlConnection connection)
//        {
//            using (NpgsqlCommand cmd = new NpgsqlCommand(PARAMETER_SQL, connection))
//            {
//                cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                cmd.Parameters.Add(new NpgsqlParameter("object", DbType.String));
//                cmd.Parameters[0].Value = ToDbStr(schema, true);
//                cmd.Parameters[1].Value = ToDbStr(objectName, true);
//                using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                {
//                    FieldInfo[] fields = CreateMapper(reader, typeof(ParameterData));
//                    while (reader.Read())
//                    {
//                        ParameterData data = new ParameterData();
//                        ReadObject(data, reader, fields);
//                        data.AddToStoredFunction(this);
//                    }
//                }
//            }
//        }

//        private const string ROUTINE_SQL = "select\r\n" +
//            "  specific_catalog, specific_schema, \"specific_name\", \"routine_catalog\", \"routine_schema\", \r\n" +
//            "  \"routine_name\", routine_type, module_catalog, module_schema, module_name, udt_catalog, \r\n" +
//            "  udt_schema, udt_name, data_type, character_maximum_length, character_octet_length, \r\n" +
//            "  \"character_set_catalog\", \"character_set_schema\", \"character_set_name\", \"collation_catalog\", \r\n" +
//            "  \"collation_schema\", \"collation_name\", numeric_precision, numeric_precision_radix, \r\n" +
//            "  numeric_scale, datetime_precision, interval_type, interval_precision, type_udt_catalog, \r\n" +
//            "  type_udt_schema, type_udt_name, \"scope_catalog\", \"scope_schema\", \"scope_name\", \r\n" +
//            "  maximum_cardinality, dtd_identifier, routine_body, routine_definition, external_name, \r\n" +
//            "  external_language, parameter_style, is_deterministic, sql_data_access, is_null_call, \r\n" +
//            "  sql_path, schema_level_routine, max_dynamic_result_sets, is_user_defined_cast, \r\n" +
//            "  is_implicitly_invocable, security_type, to_sql_specific_catalog, to_sql_specific_schema, \r\n" +
//            "  to_sql_specific_name, as_locator, created, last_altered, new_savepoint_level, \r\n" +
//            "  is_udt_dependent, result_cast_from_data_type, result_cast_as_locator, result_cast_char_max_length, \r\n" +
//            "  result_cast_char_octet_length, result_cast_char_set_catalog, result_cast_char_set_schema, \r\n" +
//            //"  result_cast_character_set_name, result_cast_collation_catalog, result_cast_collation_schema, \r\n" +
//            "  result_cast_collation_catalog, result_cast_collation_schema, \r\n" +
//            "  result_cast_collation_name, result_cast_numeric_precision, result_cast_numeric_precision_radix, \r\n" +
//            "  result_cast_numeric_scale, result_cast_datetime_precision, result_cast_interval_type, \r\n" +
//            "  result_cast_interval_precision, result_cast_type_udt_catalog, result_cast_type_udt_schema, \r\n" +
//            "  result_cast_type_udt_name, result_cast_scope_catalog, result_cast_scope_schema, \r\n" +
//            "  result_cast_scope_name, result_cast_maximum_cardinality, result_cast_dtd_identifier\r\n" +
//            "from information_schema.routines\r\n" +
//            "where (\"routine_schema\" = :schema or :schema is null)\r\n" +
//            "  and (\"routine_name\" = :object or :object is null)\r\n"+
//            "order by specific_catalog, specific_schema, \"specific_name\"";
//        public override void LoadStoredFunction(string schema, string objectName, IDbConnection connection)
//        {
//            NpgsqlConnection conn = connection as NpgsqlConnection;
//            if (conn == null)
//            {
//                return;
//            }
//            DisableChangeLog();
//            try
//            {
//                List<StoredFunction> list = new List<StoredFunction>();
//                using (NpgsqlCommand cmd = new NpgsqlCommand(ROUTINE_SQL, conn))
//                {
//                    cmd.Parameters.Add(new NpgsqlParameter("schema", DbType.String));
//                    cmd.Parameters.Add(new NpgsqlParameter("object", DbType.String));
//                    cmd.Parameters[0].Value = ToDbStr(schema, true);
//                    cmd.Parameters[1].Value = ToDbStr(objectName, true);
//                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        FieldInfo[] fields = CreateMapper(reader, typeof(RoutineData));
//                        while (reader.Read())
//                        {
//                            RoutineData data = new RoutineData();
//                            ReadObject(data, reader, fields);
//                            list.Add(data.ToStoredFunction(this));
//                        }
//                    }
//                }
//                if (string.IsNullOrEmpty(objectName))
//                {
//                    LoadStoredFunctionParameters(schema, objectName, conn);
//                }
//                else
//                {
//                    foreach (StoredFunction f in list)
//                    {
//                        LoadStoredFunctionParameters(f.SchemaName, f.Identifier, conn);
//                    }
//                }
//            }
//            finally
//            {
//                EnableChangeLog();
//            }
//        }
//        public override void LoadStoredProcedure(string schema, string objectName, IDbConnection connection) { }

        //public override void LoadSchema(IDbConnection connection)
        //{
        //    LoadSchema_Pg(connection as NpgsqlConnection);
        //}
        //public override void LoadSchema(IDbConnection connection)
        //{
        //    DisableChangeLog();
        //    try
        //    {
        //        Schemas.Clear();
        //        LoadUserInfo(connection);
        //        LoadTable(null, null, connection);
        //        LoadView(null, null, connection);
        //        LoadColumn(null, null, connection);
        //        LoadStoredFunction(null, null, connection);
        //        LoadComment(null, null, connection);
        //        LoadConstraint(null, null, connection);
        //        LoadTrigger(null, null, connection);
        //    }
        //    finally
        //    {
        //        EnableChangeLog();
        //    }
        //}
    }
}
