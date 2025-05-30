﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        private const string ERROR_CODE_PERMISSION_DENIED = "42501";  // 42501:エラー:テーブルのアクセス許可が拒否されました
        private static void LogDbCommand(string prefix, NpgsqlCommand command)
        {
            if (!IsSQLLoggingEnabled)
            {
                return;
            }
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                buf.AppendLine(prefix);
            }
            buf.AppendLine(command.CommandText);
            buf.Append("-- Parameters");
            foreach (NpgsqlParameter p in command.Parameters)
            {
                buf.AppendLine();
                buf.AppendFormat("--  {0} = {1}", p.ParameterName, p.Value);
            }
            LogSQL(buf.ToString());
        }
        public static readonly Dictionary<string, Type> TypeNameToType = new Dictionary<string, Type>()
        {
            { "boolean", typeof(bool) },
            { "bool", typeof(bool) },
            { "smallint", typeof(int) },
            { "integer", typeof(int) },
            { "int2", typeof(int) },
            { "int4", typeof(int) },
            { "bigint", typeof(long) },
            { "int8", typeof(long) },
            { "real", typeof(float) },
            { "float4", typeof(float) },
            { "money", typeof(decimal) },

            //可変長
            { "text", typeof(string) },
            { "citext", typeof(string) },
            { "json", typeof(string) },
            { "jsonb", typeof(string) },
            { "xml", typeof(string) },

            //固定長あり
            { "character varying", typeof(string) },
            { "nvarchar", typeof(string) },
            { "varchar", typeof(string) },
            { "character", typeof(string) },
            { "char", typeof(string) },
            { "\"char\"", typeof(string) },
            { "name", typeof(string) },
            //固定桁数あり
            { "double precision", typeof(double) },
            { "numeric", typeof(double) },

            { "point", typeof(NpgsqlPoint) },
            { "lseg", typeof(NpgsqlLSeg) },
            { "path", typeof(NpgsqlPath) },
            { "polygon", typeof(NpgsqlPolygon) },
            { "line", typeof(NpgsqlLine) },
            { "circle", typeof(NpgsqlCircle) },
            { "box", typeof(NpgsqlBox) },
            { "bit", typeof(BitArray) },

            // 固定桁数あり
            { "bit varying", typeof(BitArray) },
            { "hstore", typeof(IDictionary<string, string>) },
            { "uuid", typeof(Guid) },
            { "inet", typeof(IPAddress) },
            { "macaddr", typeof(PhysicalAddress) },
            { "tsquery", typeof(NpgsqlTsQuery) },
            { "tsvector", typeof(NpgsqlTsVector) },
            { "abstime", typeof(DateTime) },
            { "date", typeof(DateTime) },
            { "timestamp", typeof(DateTime) },
            { "timestamp without time zone", typeof(DateTime) },
            { "timestamp with time zone", typeof(DateTime) },
            { "time", typeof(TimeSpan) },
            { "interval", typeof(TimeSpan) },
            { "time with time zone", typeof(DateTimeOffset) },

            { "bytea", typeof(byte[]) },
            { "oid", typeof(uint) },
            { "xid", typeof(uint) },
            { "cid", typeof(uint) },

            // 固定長あり
            { "oidvector", typeof(uint[]) },
            { "record", typeof(object[]) },
            { "array", typeof(string[]) },
            { "void", null },

            //配列
            { "_xml", typeof(string[]) },
            { "_json", typeof(string[]) },
            { "_line", typeof(NpgsqlLine[]) },
            { "_circle", typeof(NpgsqlCircle[]) },
            { "_money", typeof(decimal[]) },
            { "_bool", typeof(bool[]) },
            { "_bytea", typeof(byte[][]) },
            { "_char", typeof(string[]) },
            { "_name", typeof(string[]) },
            { "_int2", typeof(int[]) },
            { "int2vector", typeof(short[]) },
            //{ "_int2vector", typeof(string[]) },
            { "_int4", typeof(int[]) },
            //{ "_regproc", typeof(string[]) },
            { "_text", typeof(string[]) },
            { "_oid", typeof(uint[]) },
            //{ "_tid", typeof(string[]) },
            { "_xid", typeof(uint[]) },
            { "_cid", typeof(uint[]) },
            //{ "_oidvector", typeof(string[]) },
            { "_bpchar", typeof(string[]) },
            { "_varchar", typeof(string[]) },
            { "_int8", typeof(long[]) },
            { "_point", typeof(NpgsqlPoint[]) },
            { "_lseg", typeof(NpgsqlLSeg[]) },
            { "_path", typeof(NpgsqlPath[]) },
            { "_box", typeof(NpgsqlBox[]) },
            { "_float4", typeof(float[]) },
            { "_float8", typeof(double[]) },
            { "_abstime", typeof(DateTime[]) },
            //{ "_reltime", typeof(TimeSpan[]) },
            { "_tinterval", typeof(TimeSpan[]) },
            { "_polygon", typeof(NpgsqlPolygon[]) },
            //{ "_aclitem", typeof(string[]) },
            { "_macaddr", typeof(PhysicalAddress[]) },
            { "_inet", typeof(IPAddress[]) },
            //{ "_cidr", typeof(string[]) },
            { "_cstring", typeof(string[]) },
            { "_timestamp", typeof(DateTime[]) },
            { "_date", typeof(DateTime[]) },
            { "_time", typeof(TimeSpan[]) },
            { "_timestamptz", typeof(TimeSpan[]) },
            { "_interval", typeof(TimeSpan[]) },
            { "_numeric", typeof(double[]) },
            { "_timetz", typeof(TimeSpan[]) },
            { "_bit", typeof(BitArray) },
            { "_varbit", typeof(BitArray) },
            //{ "_refcursor", typeof(string[]) },
            //{ "_regprocedure", typeof(string[]) },
            //{ "_regoper", typeof(string[]) },
            //{ "_regoperator", typeof(string[]) },
            //{ "_regclass", typeof(string[]) },
            //{ "_regtype", typeof(string[]) },
            //{ "_regrole", typeof(string[]) },
            //{ "_regnamespace", typeof(string[]) },
            { "_uuid", typeof(Guid[]) },
            //{ "_pg_lsn", typeof(string[]) },
            { "_tsvector", typeof(NpgsqlTsVector[]) },
            //{ "_gtsvector", typeof(NpgsqlTsVector[]) },
            { "_tsquery", typeof(NpgsqlTsQuery[]) },
            //{ "_regconfig", typeof(string[]) },
            //{ "_regdictionary", typeof(string[]) },
            { "_jsonb", typeof(string[]) },
            //{ "_txid_snapshot", typeof(string[]) },
            //{ "_int4range", typeof(int[]) },
            //{ "_numrange", typeof(double[]) },
            //{ "_tsrange", typeof(string[]) },
            //{ "_tstzrange", typeof(string[]) },
            //{ "_daterange", typeof(string[]) },
            //{ "_int8range", typeof(string[]) },
            { "_record", typeof(object[][]) },
        };

        internal static FieldInfo[] CreateMapper(NpgsqlDataReader reader, Type typeInfo)
        {
            FieldInfo[] ret = new FieldInfo[reader.FieldCount];
            for (int i = 0, n = reader.FieldCount; i < n; i++)
            {
                ret[i] = typeInfo.GetField(reader.GetName(i));
            }
            return ret;
        }
        private delegate object GetProc(NpgsqlDataReader reader, int index);
        private static Dictionary<Type, GetProc> PrimitiveProcs = new Dictionary<Type, GetProc>()
        {
            { typeof(sbyte), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<sbyte>(index); } },
            { typeof(byte), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<byte>(index); } },
            { typeof(short), (NpgsqlDataReader reader, int index) => { return reader.GetInt16(index); } },
            { typeof(ushort), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<ushort>(index); } },
            { typeof(int), (NpgsqlDataReader reader, int index) => { return reader.GetInt32(index); } },
            { typeof(uint), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<uint>(index); } },
            { typeof(long), (NpgsqlDataReader reader, int index) => { return reader.GetInt64(index); } },
            { typeof(ulong), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<ulong>(index); } },
            { typeof(bool), (NpgsqlDataReader reader, int index) => { return reader.GetBoolean(index); } },
            { typeof(char), (NpgsqlDataReader reader, int index) => { return reader.GetChar(index); } },
            { typeof(string), (NpgsqlDataReader reader, int index) => { return reader.IsDBNull(index) ? null : reader.GetString(index); } },
            { typeof(float), (NpgsqlDataReader reader, int index) => { return reader.GetFloat(index); } },
            { typeof(double), (NpgsqlDataReader reader, int index) => { return reader.GetDouble(index); } },
            { typeof(decimal), (NpgsqlDataReader reader, int index) => { return reader.GetDecimal(index); } },
        };
        private static Dictionary<Type, GetProc> ArrayProcs = new Dictionary<Type, GetProc>()
        {
            { typeof(sbyte), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<sbyte[]>(index); } },
            { typeof(byte), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<byte[]>(index); } },
            { typeof(short), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<short[]>(index); } },
            { typeof(ushort), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<ushort[]>(index); } },
            { typeof(int), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<int[]>(index); } },
            { typeof(uint), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<uint[]>(index); } },
            { typeof(long), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<long[]>(index); } },
            { typeof(ulong), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<ulong[]>(index); } },
            { typeof(bool), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<bool[]>(index); } },
            { typeof(char), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<char[]>(index); } },
            { typeof(string), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<string[]>(index); } },
            { typeof(float), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<float[]>(index); } },
            { typeof(double), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<double[]>(index); } },
            { typeof(decimal), (NpgsqlDataReader reader, int index) => { return reader.GetFieldValue<decimal[]>(index); } },
        };
        internal static void ReadObject(object target, NpgsqlDataReader reader, FieldInfo[] fields)
        {
            for (int i = 0, n = fields.Length; i < n; i++)
            {
                FieldInfo f = fields[i];
                if (f == null)
                {
                    continue;
                }
                Type ft = f.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (reader.IsDBNull(i))
                    {
                        f.SetValue(target, null);
                        continue;
                    }
                    ft = ft.GetGenericArguments()[0];
                }

                GetProc proc;
                if (PrimitiveProcs.TryGetValue(ft, out proc))
                {
                    f.SetValue(target, proc.Invoke(reader, i));
                    continue;
                }
                if (ft.IsArray)
                {
                    Type et = ft.GetElementType();
                    if (reader.IsDBNull(i))
                    {
                        f.SetValue(target, null);
                        continue;
                    }
                    if (ArrayProcs.TryGetValue(et, out proc))
                    {
                        f.SetValue(target, proc.Invoke(reader, i));
                        continue;
                    }
                }
                if (ft.IsSubclassOf(typeof(IList)))
                {
                    Type gt = ft.GetGenericTypeDefinition();
                    if (reader.IsDBNull(i))
                    {
                        f.SetValue(target, null);
                        continue;
                    }
                    if (ArrayProcs.TryGetValue(gt, out proc))
                    {
                        f.SetValue(target, proc.Invoke(reader, i));
                        continue;
                    }
                }
                //else
                //{
                //    reader.GetFieldValue<ft>(i)
                //}
            }
        }

        /// <summary>
        /// SQL文を小文字にする
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string NormalizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return sql;
            }
            int i = 0;
            int n = sql.Length;
            StringBuilder buf = new StringBuilder(sql);
            for (; i < n; i++)
            {
                if (char.IsSurrogate(buf[i]))
                {
                    i++;
                    continue;
                }
                switch (buf[i])
                {
                    case '\'':
                        // 文字列中は変換しない
                        for (i++; i < n && buf[i] != '\''; i++) ;
                        break;
                    case '"':
                        // クオートされてる中は変換しない
                        for (i++; i < n && buf[i] != '"'; i++) ;
                        break;
                    case ':':
                        // バインド変数内は変換しない
                        for (i++; i < n && !char.IsWhiteSpace(buf[i]); i++)
                        {
                            if (buf[i] == '"')
                            {
                                for (i++; i < n && buf[i] != '"'; i++) ;
                            }
                        }
                        break;
                    case '-':
                        if (buf[i + 1] == '-')
                        {
                            // コメントの中は変換しない
                            for (i += 2; i < n && buf[i] != '\r' && buf[i] != '\n'; i++) ;
                        }
                        break;
                    case '/':
                        if (buf[i + 1] == '*')
                        {
                            // コメントの中は変換しない
                            i++;
                            while (i < n)
                            {
                                for (i++; i < n && buf[i] != '*'; i++) ;
                                if (i < n && buf[i + 1] == '/')
                                {
                                    i++;
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        buf[i] = NormalizeIdentifierChar(buf[i]);
                        break;
                }
            }
            return buf.ToString();
        }

        internal static string ToIdentifier(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder(names[0]);
            for (int i = 1, n = names.Length; i < n; i++)
            {
                buf.Append('.');
                buf.Append(names[i]);
            }
            return buf.ToString();
        }

        internal class PgObjectCollection<T> : IReadOnlyList<T> where T : PgObject, new()
        {
            private readonly List<T> _items = new List<T>();
            private Dictionary<uint, T> _oidToItem = new Dictionary<uint, T>();
            private Dictionary<string, T> _nameToItem = null;

            public void Fill(string sql, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                            while (reader.Read())
                            {
                                T obj = new T();
                                ReadObject(obj, reader, mapper);
                                if (obj.oid != 0)
                                {
                                    T old;
                                    if (_oidToItem.TryGetValue(obj.oid, out old))
                                    {
                                        _items.Remove(old);
                                    }
                                    _oidToItem[obj.oid] = obj;
                                }
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            public void Fill(string sql, NpgsqlParameter[] parameters, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.Parameters.AddRange(parameters);
                    LogDbCommand("PgObjectCollection<T>.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                            while (reader.Read())
                            {
                                T obj = new T();
                                ReadObject(obj, reader, mapper);
                                if (obj.oid != 0)
                                {
                                    T old;
                                    if (_oidToItem.TryGetValue(obj.oid, out old))
                                    {
                                        _items.Remove(old);
                                    }
                                    _oidToItem[obj.oid] = obj;
                                }
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void Join(string sql, NpgsqlConnection connection)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.Join", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                            int oidPos = -1;
                            for (int p = 0; p < mapper.Length; p++)
                            {
                                FieldInfo fi = mapper[p];
                                if (fi.Name == "oid")
                                {
                                    oidPos = p;
                                    break;
                                }
                            }
                            if (oidPos == -1)
                            {
                                throw new ApplicationException("oid not found");
                            }
                            while (reader.Read())
                            {
                                UInt32 oid = reader.GetFieldValue<UInt32>(oidPos);
                                T obj = FindByOid(oid);
                                if (obj != null)
                                {
                                    ReadObject(obj, reader, mapper);
                                }
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void JoinToDict<T2>(string sql, FieldInfo dictField, string fieldname, NpgsqlConnection connection)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.JoinToDict", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            int oidPos = -1;
                            int indexPos = -1;
                            int fieldPos = -1;
                            for (int p = 0; p < reader.FieldCount; p++)
                            {
                                string nm = reader.GetName(p);
                                if (nm == "oid")
                                {
                                    oidPos = p;
                                }
                                else if (nm == "n")
                                {
                                    indexPos = p;
                                }
                                else if (nm == fieldname)
                                {
                                    fieldPos = p;
                                }
                            }
                            if (oidPos == -1)
                            {
                                throw new ApplicationException("field \"oid\" not found");
                            }
                            if (indexPos == -1)
                            {
                                throw new ApplicationException("field \"n\" not found");
                            }
                            if (fieldPos == -1)
                            {
                                throw new ApplicationException(string.Format("field \"{0}\" not found", fieldname));
                            }
                            while (reader.Read())
                            {
                                UInt32 oid = reader.GetFieldValue<UInt32>(oidPos);
                                T obj = FindByOid(oid);
                                if (obj == null)
                                {
                                    continue;
                                }
                                int i = reader.GetInt32(indexPos);
                                Dictionary<int, T2> dict = (Dictionary<int, T2>)dictField.GetValue(obj);
                                dict[i] = reader.GetFieldValue<T2>(fieldPos);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }

            public void BeginFillReference(WorkingData working)
            {
                _nameToItem = null;
                foreach (T obj in _items)
                {
                    obj.BeginFillReference(working);
                }
            }

            public void UpdateNameToItem()
            {
                Dictionary<string, T> dict = new Dictionary<string, T>();
                foreach (T obj in _items)
                {
                    string id = obj.GetIdentifier(true);
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    dict[id] = obj;
                }
                _nameToItem = dict;
            }

            public void EndFillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.EndFillReference(working);
                }
            }
            public void FillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.FillReference(working);
                }
            }

            public T FindByOid(uint oid)
            {
                T ret;
                if (!_oidToItem.TryGetValue(oid, out ret))
                {
                    return null;
                }
                return ret;
            }

            public T FindByName(string identifier)
            {
                if (string.IsNullOrEmpty(identifier))
                {
                    return null;
                }
                if (_nameToItem == null)
                {
                    throw new ApplicationException("FindByName() can not call before EndFillReference()");
                }
                T obj;
                if (!_nameToItem.TryGetValue(identifier, out obj))
                {
                    return null;
                }
                return obj;
            }


            public PgObjectCollection(string sql, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, working, connection, false);
            }

            public PgObjectCollection(string sql, NpgsqlParameter[] parameters, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, parameters, working, connection, false);
            }

            public PgObjectCollection() { }

            #region IReadOnlyList の実装
            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }

            public T this[int index] { get { return _items[index]; } }
            #endregion
        }
        internal class PgOidSubidCollection<T> : IReadOnlyList<T> where T : PgOidSubid, new()
        {
            private readonly List<T> _items = new List<T>();
            private Dictionary<ulong, T> _oidNumToItem = new Dictionary<ulong, T>();
            private Dictionary<string, T> _nameToItem = null;

            private static ulong ToKey(uint oid, int subid)
            {
                return (((ulong)oid) << 32) | (uint)subid;
            }

            //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void Fill(string sql, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                //InvalidateDictionary();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgOidSubidCollection<T>.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                            while (reader.Read())
                            {
                                T obj = new T();
                                ReadObject(obj, reader, mapper);
                                if (obj.Oid != 0)
                                {
                                    ulong id = ToKey(obj.Oid, obj.Subid);
                                    T old;
                                    if (_oidNumToItem.TryGetValue(id, out old))
                                    {
                                        _items.Remove(old);
                                    }
                                    _oidNumToItem[id] = obj;
                                }
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
                _nameToItem = null;
                foreach (T obj in _items)
                {
                    obj.BeginFillReference(working);
                }
            }

            public void UpdateNameToItem()
            {
                Dictionary<string, T> dict = new Dictionary<string, T>();
                foreach (T obj in _items)
                {
                    string id = obj.GetIdentifier();
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    dict[id] = obj;
                }
                _nameToItem = dict;
            }

            public void EndFillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.EndFillReference(working);
                }
                UpdateNameToItem();
            }
            public void FillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.FillReference(working);
                }
            }

            public T FindByOidNum(uint oid, int num)
            {
                //RequireDictionary();
                T ret;
                ulong v = ToKey(oid, num);
                if (!_oidNumToItem.TryGetValue(v, out ret))
                {
                    return null;
                }
                return ret;
            }

            public T FindByName(string identifier)
            {
                if (string.IsNullOrEmpty(identifier))
                {
                    return null;
                }
                if (_nameToItem == null)
                {
                    throw new ApplicationException("FindByName() can not call before EndFillReference()");
                }
                T obj;
                if (!_nameToItem.TryGetValue(identifier, out obj))
                {
                    return null;
                }
                return obj;
            }

            public void RemoveByOid(uint oid)
            {
                for (int i = _items.Count - 1; 0 <= i; i--)
                {
                    T obj = _items[i];
                    if (obj.Oid == oid)
                    {
                        _items.RemoveAt(i);
                        _oidNumToItem.Remove(ToKey(obj.Oid, obj.Subid));
                    }
                }
            }
            public PgOidSubidCollection(string sql, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, working, connection, false);
            }
            public PgOidSubidCollection() { }

            #region IReadOnlyList の実装
            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }

            public T this[int index] { get { return _items[index]; } }
            #endregion
        }

        /// <summary>
        /// SQLのwhere句に条件式を追加する
        /// 1. baseSql中の"where"を検索してその直後に条件を追加する
        /// 2. 1.に該当しない場合、baseSql中の"order"を検索してその直前に条件を追加する
        /// 3. 2.に該当しない場合、baseSqlの後に条件を追加する
        /// いずれの場合も適宜 where や and を追加する
        /// </summary>
        /// <param name="baseSql"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static string AddCondition(string baseSql, string condition)
        {
            int i = baseSql.LastIndexOf("where", StringComparison.CurrentCultureIgnoreCase);
            if (i != -1)
            {
                i += 5;
                return baseSql.Substring(0, i) + " " + condition + " and " + baseSql.Substring(i);
            }
            i = baseSql.LastIndexOf("order", StringComparison.CurrentCultureIgnoreCase);
            if (i != -1)
            {
                return baseSql.Substring(0, i) + " where " + condition + " " + baseSql.Substring(i);
            }
            return baseSql + " where " + condition;
        }

        internal abstract class PgObject : IComparable, IComparable<PgObject>
        {
            //public static PgObjectCollection<PgObject> DefaultStore;
#pragma warning disable 0649
            public uint oid;
#pragma warning restore 0649
            //public abstract string Name { get; }
            public WeakReference<NamedObject> Generated;

            public List<PgObject> DependBy = new List<PgObject>();
            public string Extension;

            public virtual void BeginFillReference(WorkingData working) { }
            public virtual void EndFillReference(WorkingData working) { }
            public abstract void FillReference(WorkingData working);
            public abstract string GetIdentifier(bool fullName);
            public void TrimDependBy()
            {
                DependBy.Sort();
                for (int i = DependBy.Count - 1; 0 < i; i--)
                {
                    if (DependBy[i].CompareTo(DependBy[i - 1]) == 0)
                    {
                        DependBy.RemoveAt(i);
                    }
                }
            }
            public PgObject() { }

            public override bool Equals(object obj)
            {
                if (!(obj is PgObject pgObj))
                {
                    return false;
                }
                return oid == pgObj.oid;
            }

            public override int GetHashCode()
            {
                return oid.GetHashCode();
            }

            public int CompareTo(PgObject other)
            {
                return oid.CompareTo(other.oid);
            }

            public int CompareTo(object obj)
            {
                if (!(obj is PgObject pgObj))
                {
                    return -1;
                }
                return oid.CompareTo(pgObj.oid);
            }
        }
        internal class PgNamespace : PgObject
        {
#pragma warning disable 0649
            public string nspname;
            public uint nspowner;
            public string ownername;
#pragma warning restore 0649

            //public override string Name { get { return nspname; } }
            public PgNamespace() : base() { }
            public static PgObjectCollection<PgNamespace> Namespaces = null;
            public static PgObjectCollection<PgNamespace> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgNamespace> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgNamespace>(DataSet.Properties.Resources.PgNamespace_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgNamespace_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgNamespace> Load(WorkingData working, NpgsqlConnection connection)
            {
                Namespaces = new PgObjectCollection<PgNamespace>(DataSet.Properties.Resources.PgNamespace_SQL, working, connection);
                return Namespaces;
            }
            public static PgObjectCollection<PgNamespace> Load(WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgNamespace_SQL, string.Format("oid = {0}::oid", oid));
                Namespaces = new PgObjectCollection<PgNamespace>(sql, working, connection);
                return Namespaces;
            }
            public override void FillReference(WorkingData working)
            {
            }
            public override string GetIdentifier(bool fullName)
            {
                return nspname;
            }

            public Schema ToSchema(NpgsqlDataSet context)
            {
                if (Generated != null && Generated.TryGetTarget(out var s))
                {
                    return s as Schema;
                }

                Schema schema = new Schema(context, nspname)
                {
                    Owner = ownername
                };
                context.IsHiddenSchema(schema.Name);
                Generated = new WeakReference<NamedObject>(schema);
                return schema;
            }

            public override string ToString()
            {
                return string.Format("{0}({1})", nspname, oid);
            }
        }
        internal class PgClass : PgObject
        {
#pragma warning disable 0649
            public string relname;
            public uint relnamespace;
            //public uint reltype;
            //public uint reloftype;
            //public uint relowner;
            //public uint relam;
            //public uint relfilenode;
            public uint reltablespace;
            //public int relpages;
            //public float reltuples;
            //public int relallvisible;
            //public uint reltoastrelid;
            //public bool relhasindex;
            //public bool relisshared;
            //public char relpersistence;
            public char relkind;
            //public int relnatts;
            //public int relchecks;
            //public bool relhasoids;
            //public bool relhaspkey;
            //public bool relhasrules;
            //public bool relhastriggers;
            //public bool relhassubclass;
            //public bool relrowsecurity;
            //public bool relforcerowsecurity;
            //public bool relispopulated;
            //public char relreplident;
            //public uint relfrozenxid;
            //public uint relminmxid;
            //public string relacl;
            //public string reloptions;
            public bool relispartition;
            public string partitionbound;
            public string ownername;

            public string viewdef;

            public uint indrelid;
            public int indnatts;
            public bool indisunique;
            public bool indisprimary;
            public bool indisexclusion;
            public bool indimmediate;
            public bool indisclustered;
            public bool indisvalid;
            public bool indcheckxmin;
            public bool indisready;
            public bool indislive;
            public bool indisreplident;
            public short[] indkey;
            public uint[] indcollation;
            public uint[] indclass;
            public short[] indoption;
            public string indexdef;
            public string indextype;
            public long start_value;
            public long maximum_value;
            public long minimum_value;
            public long increment;
            public bool cycle_option;
            public string owned_schema;
            public string owned_table;
            public string owned_field;
            public string[] ftoptions;
            public string srvname;
            public string[] srvoptions;
            public string fdwname;
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgClass IndexTable;
            public PgTablespace TableSpace;
            //public PgClass OwnedTable;
            public PgAttribute OwnedColumn;
            public bool IsImplicit;
            public string[] IndexColumns;
            public List<PgAttribute> Columns = new List<PgAttribute>();
            public List<PgConstraint> Constraints = new List<PgConstraint>();
            public List<PgClass> Indexes = new List<PgClass>();

            //public override string Name { get { return string.Format("{0}_{1}:{2}", relnamespace, relname, relkind); } }
            public PgClass() : base() { }
            public static PgObjectCollection<PgClass> Classes = null;
            public static PgClass[] EmptyArray = new PgClass[0];
            public static PgObjectCollection<PgClass> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgClass> store)
            {
                string sql = (10 <= connection.PostgreSqlVersion.Major) ? DataSet.Properties.Resources.PgClass_SQL : DataSet.Properties.Resources.PgClass9_SQL;
                PgObjectCollection<PgClass> l;
                if (store == null)
                {
                    l = new PgObjectCollection<PgClass>(sql, working, connection);
                }
                else
                {
                    l = store;
                    l.Fill(sql, working, connection, false);
                }
                l.Join(DataSet.Properties.Resources.PgClass_VIEWDEFSQL, connection);
                l.Join(DataSet.Properties.Resources.PgClass_INDEXSQL, connection);
                l.Join(DataSet.Properties.Resources.PgClass_SEQUENCESQL, connection);
                l.Join(DataSet.Properties.Resources.PgForeignTable_SQL, connection);
                return l;
            }
            public static PgObjectCollection<PgClass> Load(WorkingData working, NpgsqlConnection connection)
            {
                Classes = new PgObjectCollection<PgClass>(DataSet.Properties.Resources.PgClass_SQL, working, connection);
                return Classes;
            }
            public static void FillByOid(PgObjectCollection<PgClass> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgClass_SQL, string.Format("oid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
                sql = AddCondition(DataSet.Properties.Resources.PgClass_VIEWDEFSQL, string.Format("oid = {0}::oid", oid));
                store.Join(sql, connection);
                sql = AddCondition(DataSet.Properties.Resources.PgClass_INDEXSQL, string.Format("i.indexrelid = {0}::oid", oid));
                store.Join(sql, connection);
                sql = AddCondition(DataSet.Properties.Resources.PgClass_SEQUENCESQL, string.Format("c.oid = {0}::oid", oid));
                store.Join(sql, connection);
            }
            public static uint[] GetRelatedOid(NpgsqlConnection connection, uint oid)
            {
                List<uint> oids = new List<uint>();
                using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgClass_RELATEDSQL, connection))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("oid", NpgsqlDbType.Oid) { Value = oid });
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                oids.Add((uint)reader.GetValue(0));
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
                return oids.ToArray();
            }
            public static uint? GetOid(NpgsqlConnection connection, string schema, string name, char relkind)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgClassOid_SQL, connection))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("schema", schema));
                    cmd.Parameters.Add(new NpgsqlParameter("name", name));
                    cmd.Parameters.Add(new NpgsqlParameter("kind", relkind));
                    try
                    {
                        object ret = cmd.ExecuteScalar();
                        if (ret == null || ret is DBNull)
                        {
                            return null;
                        }
                        return (uint)ret;
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED)
                    {
                        return null;
                    }
                }
            }
            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Schema?.nspname, relname) : relname;
            }
            public override void BeginFillReference(WorkingData working)
            {
                Columns.Clear();
                Constraints.Clear();
                IndexTable = null;
                IndexColumns = null;
                if (!string.IsNullOrEmpty(viewdef))
                {
                    string s = viewdef.TrimEnd();
                    if (s.EndsWith(";"))
                    {
                        s = s.Substring(0, s.Length - 1);
                    }
                    viewdef = s;
                }
            }
            public override void EndFillReference(WorkingData working)
            {
                PgAttribute a;
                switch (relkind)
                {
                    case 'i':
                        PgClass tbl = working.PgClasses.FindByOid(indrelid);
                        if (tbl == null)
                        {
                            return;
                        }
                        tbl.Indexes.Add(this);
                        if (indkey != null)
                        {
                            foreach (int i in indkey)
                            {
                                a = working.PgAttributes.FindByOidNum(indrelid, i);
                                Columns.Add(a);
                            }
                        }
                        break;
                    case 'S':
                        //OwnedTable = working.PgClasses.FindByName(ToIdentifier(owned_schema, owned_table));
                        OwnedColumn = working.PgAttributes.FindByName(ToIdentifier(owned_schema, owned_table, owned_field));
                        if (OwnedColumn != null)
                        {
                            OwnedColumn.Sequence = this;
                        }
                        break;
                }
            }
            public override void FillReference(WorkingData working)
            {
                Schema = working.PgNamespaces.FindByOid(relnamespace);
                if (relkind == 'i')
                {
                    IndexTable = working.PgClasses.FindByOid(indrelid);
                    IndexColumns = new string[indkey.Length];
                    for (int i = 0; i < indkey.Length; i++)
                    {
                        PgAttribute a = working.PgAttributes.FindByOidNum(indrelid, indkey[i]);
                        IndexColumns[i] = a?.attname;
                    }
                }
                TableSpace = working.PgTablespaces.FindByOid(reltablespace);
            }

            public View ToView(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (View)o;
                }
                if (relkind != 'v' && relkind != 'm')
                {
                    return null;
                }
                View.Kind k = (relkind == 'm') ? View.Kind.MarerializedView : View.Kind.View;
                View v = new View(context, k, ownername, Schema?.nspname, relname, viewdef, true)
                {
                    Extension = Extension
                };
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (c.HiddenLevel == HiddenLevel.Visible)
                    {
                        c.Index = i++;
                    }
                }
                v.InvalidateColumns();
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                v.DependBy = l.ToArray();
                Generated = new WeakReference<NamedObject>(v);
                return v;
            }

            public Table ToTable(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (Table)o;
                }
                if ("fprt".IndexOf(relkind) == -1)
                {
                    return null;
                }
                Table.Kind k = (relkind == 'f') ? Table.Kind.ForeignTable : Table.Kind.Table;
                Table t = new Table(context, k, ownername, Schema?.nspname, relname)
                {
                    TablespaceName = TableSpace?.spcname,
                    IsPartitioned = relispartition,
                    PartitionBound = partitionbound,
                    ForeignServer = srvname,
                    ForeignTableOptions = ftoptions,
                    Extension = Extension
                };
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (c.HiddenLevel == HiddenLevel.Visible)
                    {
                        c.Index = i++;
                    }
                }
                t.InvalidateColumns();
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                t.DependBy = l.ToArray();
                Generated = new WeakReference<NamedObject>(t);
                return t;
            }

            public ComplexType ToComplexType(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (ComplexType)o;
                }
                if (relkind != 'c')
                {
                    return null;
                }
                ComplexType t = new ComplexType(context, ownername, Schema?.nspname, relname)
                {
                    Extension = Extension
                };
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (c.HiddenLevel == HiddenLevel.Visible)
                    {
                        c.Index = i++;
                    }
                }
                t.InvalidateColumns();
                Generated = new WeakReference<NamedObject>(t);
                return t;
            }

            public Index ToIndex(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (Index)o;
                }
                if (relkind != 'i' && relkind != 'I')
                {
                    return null;
                }
                Index idx = new Index(context, ownername, Schema?.nspname, relname, IndexTable?.Schema?.nspname, IndexTable?.relname, IndexColumns, indexdef)
                {
                    IsUnique = indisunique,
                    IsImplicit = IsImplicit,
                    IndexType = indextype,
                    SqlDef = NormalizeSql(indexdef),
                    Extension = Extension
                };
                Generated = new WeakReference<NamedObject>(idx);
                return idx;
            }
            public Sequence ToSequence(NpgsqlDataSet context)
            {
                NamedObject o;
                Sequence seq;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    seq = (Sequence)o;
                    return seq;
                }
                if (relkind != 'S')
                {
                    return null;
                }
                seq = new Sequence(context, ownername, Schema?.nspname, relname)
                {
                    StartValue = start_value,
                    Extension = Extension
                };
                if (0 < increment)
                {
                    seq.MinValue = minimum_value;
                    seq.MaxValue = maximum_value;
                }
                else
                {
                    seq.MinValue = minimum_value;
                    seq.MaxValue = maximum_value;
                }
                seq.Increment = increment;
                seq.IsCycled = cycle_option;
                seq.OwnedSchemaName = owned_schema;
                seq.OwnedTableName = owned_table;
                seq.OwnedColumnName = owned_field;
                if (seq.OwnedColumn != null)
                {
                    seq.OwnedColumn.Sequence = seq;
                }
                Generated = new WeakReference<NamedObject>(seq);
                return seq;
            }

            public SchemaObject ToSchemaObject(NpgsqlDataSet context)
            {
                switch (relkind)
                {
                    case 'r':   //table
                    case 'f':   //foreign table
                    case 'p':   //partitioned table
                    case 't':   //toast table
                        return ToTable(context);
                    case 'v':   //view
                    case 'm':   //materialized view
                        return ToView(context);
                    case 'i':   //index
                    case 'I':   //partitioned index
                        return ToIndex(context);
                    case 'S':   //sequence
                        return ToSequence(context);
                    case 'c':   //complex type
                        return ToComplexType(context);
                }
                return null;
            }

            public override string ToString()
            {
                return relname;
            }
        }

        internal class PgType : PgObject
        {
#pragma warning disable 0649
            public string typname;
            public uint typnamespace;
            public uint typowner;
            public int typlen;
            public bool typbyval;
            public char typtype;
            public char typcategory;
            public bool typispreferred;
            public bool typisdefined;
            public char typdelim;
            public uint typrelid;
            public uint typelem;
            public uint typarray;
            public string typinput;
            public string typoutput;
            public string typreceive;
            public string typsend;
            public string typmodin;
            public string typmodout;
            public string typanalyze;
            public char typalign;
            public char typstorage;
            public bool typnotnull;
            public uint typbasetype;
            public int typtypmod;
            public int typndims;
            public uint typcollation;
            public string typdefault;
            public string rngsubtypename;
            public string rngcollationname;
            public string rngsubopcname;
            public string rngcanonical;
            public string rngsubdiff;
            public string formatname;
            public string baseformatname;
            public string elemformatname;
            public string ownername;
            //public char relkind;
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgClass Relation;
            public PgType BaseType;
            public bool IsArray;
            public PgType ElementType;
            public PgType() : base() { }
            public static PgObjectCollection<PgType> Types = null;
            public static PgObjectCollection<PgType> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgType> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgType>(DataSet.Properties.Resources.PgType_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgType_SQL, working, connection, false);
                    return store;
                }
            }
            public static void FillByOid(PgObjectCollection<PgType> store, WorkingData working, NpgsqlConnection connection)
            {
                store.Fill(DataSet.Properties.Resources.PgType_SQL, working, connection, false);
            }
            public static PgObjectCollection<PgType> Load(WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgType_SQL, string.Format("oid = {0}::oid", oid));
                Types = new PgObjectCollection<PgType>(sql, working, connection);
                return Types;
            }
            public override void BeginFillReference(WorkingData working)
            {
            }
            public override void EndFillReference(WorkingData working)
            {
                //Relation = working.PgClasses.FindByOid(typrelid);
                BaseType = working.PgTypes.FindByOid(typbasetype);
                ElementType = working.PgTypes.FindByOid(typelem);
                IsArray = (ElementType != null) && (typlen == -1);
            }
            public override void FillReference(WorkingData working)
            {
                Schema = working.PgNamespaces.FindByOid(typnamespace);
                Relation = working.PgClasses.FindByOid(typrelid);
                //ArrayType = working.PgTypes.FindByOid(typarray);
            }

            private static string FuncStr(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }
                if (value == "-")
                {
                    return string.Empty;
                }
                return value;
            }
            public PgsqlBasicType ToBasicType(NpgsqlDataSet context)
            {
                if (typtype != 'b' || typcategory == 'A')
                {
                    return null;
                }
                PgsqlBasicType ret = new PgsqlBasicType(context, ownername, Schema?.nspname, typname)
                {
                    InputFunction = FuncStr(typinput),
                    OutputFunction = FuncStr(typoutput),
                    ReceiveFunction = FuncStr(typreceive),
                    SendFunction = FuncStr(typsend),
                    TypmodInFunction = FuncStr(typmodin),
                    TypmodOutFunction = FuncStr(typmodout),
                    AnalyzeFunction = FuncStr(typanalyze),
                    InternalLength = typlen,
                    PassedbyValue = typbyval,
                    Extension = Extension
                };
                switch (typalign)
                {
                    case 'c':
                        ret.Alignment = 1;
                        break;
                    case 's':
                        ret.Alignment = 2;
                        break;
                    case 'i':
                        ret.Alignment = 4;
                        break;
                    case 'd':
                        ret.Alignment = 8;
                        break;
                    default:
                        ret.Alignment = 0;
                        break;
                }
                switch (typstorage)
                {
                    case 'p':
                        ret.Storage = "plain";
                        break;
                    case 'e':
                        ret.Storage = "external";
                        break;
                    case 'x':
                        ret.Storage = "extended";
                        break;
                    case 'm':
                        ret.Storage = "main";
                        break;
                }
                ret.Like = baseformatname;
                ret.Category = typcategory.ToString();
                ret.Preferred = typispreferred;
                ret.Default = typdefault;
                ret.Element = elemformatname;
                ret.Delimiter = typdelim.ToString();
                ret.Collatable = (typcollation != 0);
                return ret;
            }
            public PgsqlEnumType ToEnumType(NpgsqlDataSet context)
            {
                if (typtype != 'e')
                {
                    return null;
                }
                PgsqlEnumType ret = new PgsqlEnumType(context, ownername, Schema?.nspname, typname)
                {
                    Extension = Extension
                };
                return ret;
            }
            public PgsqlRangeType ToRangeType(NpgsqlDataSet context)
            {
                if (typtype != 'r')
                {
                    return null;
                }
                PgsqlRangeType ret = new PgsqlRangeType(context, ownername, Schema?.nspname, typname)
                {
                    Subtype = rngsubtypename,
                    SubtypeDiff = rngsubdiff,
                    SubtypeOpClass = rngsubopcname,
                    Collation = rngcollationname,
                    CanonicalFunction = rngcanonical,
                    Extension = Extension
                };
                return ret;
            }
            public SchemaObject ToType(NpgsqlDataSet context)
            {
                switch (typtype)
                {
                    case 'b':   // 基本型
                        return ToBasicType(context);
                    case 'c':   // 複合型
                        return null;    // PgClassで生成する
                    case 'd':   // 派生型
                        return null;
                    case 'e':   // 列挙型
                        return ToEnumType(context);
                    case 'p':   // 疑似型
                        return null;
                    case 'r':   // 範囲型
                        return ToRangeType(context);
                    default:
                        return null;
                }
            }

            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Schema?.nspname, typname) : typname;
            }
            public override string ToString()
            {
                return typname;
            }
        }

        internal class PgConstraint : PgObject
        {
#pragma warning disable 0649
            public string conname;
            public uint connamespace;
            public char contype;
            public bool condeferrable;
            public bool condeferred;
            //public bool convalidated;
            public uint conrelid;
            //public uint contypid;
            public uint conindid;
            public uint confrelid;
            public char confupdtype;
            public char confdeltype;
            //public char confmatchtype;
            //public bool conislocal;
            //public int coninhcount;
            //public bool connoinherit;
            public short[] conkey;
            public short[] confkey;
            //public uint[] conpfeqop;
            //public string conppeqop;
            //public string conffeqop;
            //public string conexclop;
            //public string consrc;

            public string checkdef;
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgClass Object;
            public PgClass RefObject;
            public PgConstraint RefConstraint;
            public PgAttribute[] Keys;
            public PgAttribute[] RefKeys;


            //public override string Name { get { return conname; } }
            public PgConstraint() : base() { }
            //public static PgObjectCollection<PgConstraint> Constraints = null;

            public static readonly Dictionary<char, ForeignKeyRule> CharToForeignKeyRule = new Dictionary<char, ForeignKeyRule>
            {
                { 'c', ForeignKeyRule.Cascade },
                { 'n', ForeignKeyRule.SetNull },
                { 'd', ForeignKeyRule.SetDefault },
                { 'r', ForeignKeyRule.Restrict },
                { 'a', ForeignKeyRule.NoAction },
            };
            public static PgObjectCollection<PgConstraint> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgConstraint> store)
            {
                PgObjectCollection<PgConstraint> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgConstraint>(DataSet.Properties.Resources.PgConstraint_SQL, working, connection);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgConstraint_SQL, working, connection, false);
                }
                l.Join(DataSet.Properties.Resources.PgConstraint_CHECKSQL, connection);
                return l;
            }
            public static PgObjectCollection<PgConstraint> Load(WorkingData working, NpgsqlConnection connection)
            {
                PgObjectCollection<PgConstraint> l = new PgObjectCollection<PgConstraint>(DataSet.Properties.Resources.PgConstraint_SQL, working, connection);
                return l;
            }
            public static void FillByOid(PgObjectCollection<PgConstraint> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgConstraint_SQL, string.Format("conrelid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
            }
            public static uint[] GetRefTableOid(NpgsqlConnection connection, uint oid)
            {
                List<uint> oids = new List<uint>();
                using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgConstraint_REFTABLESQL, connection))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("oid", NpgsqlDbType.Oid) { Value = oid });
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                oids.Add((uint)reader.GetValue(0));
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
                return oids.ToArray();

            }
            public override void FillReference(WorkingData working)
            {
                Schema = working.PgNamespaces.FindByOid(connamespace);
                Object = working.PgClasses.FindByOid(conrelid);
                if (Object != null)
                {
                    Object.Constraints.Add(this);
                    Keys = new PgAttribute[conkey.Length];
                    for (int i = 0; i < conkey.Length; i++)
                    {
                        int k = conkey[i];
                        Keys[i] = working.PgAttributes.FindByOidNum(conrelid, k);
                    }
                }
                RefObject = working.PgClasses.FindByOid(confrelid);
            }
            public override void EndFillReference(WorkingData working)
            {
                PgClass c = working.PgClasses.FindByOid(conindid);
                if (c != null)
                {
                    c.IsImplicit = true;
                }
                if (RefObject != null)
                {
                    RefObject.Columns.Sort();
                    RefKeys = new PgAttribute[confkey.Length];
                    for (int i = 0; i < confkey.Length; i++)
                    {
                        int k = confkey[i];
                        RefKeys[i] = working.PgAttributes.FindByOidNum(confrelid, k);
                    }
                    foreach (PgConstraint cons in RefObject.Constraints)
                    {
                        if ("pu".IndexOf(cons.contype) == -1)
                        {
                            continue;
                        }
                        if (cons.Keys.Length != RefKeys.Length)
                        {
                            continue;
                        }
                        bool found = true;
                        for (int i = 0; i < cons.Keys.Length; i++)
                        {
                            if (cons.Keys[i].attnum != RefKeys[i].attnum)
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            RefConstraint = cons;
                            break;
                        }
                    }
                }
            }
            private string[] GetNameArray(PgAttribute[] attrs)
            {
                if (attrs == null)
                {
                    return null;
                }
                List<string> l = new List<string>();
                foreach (PgAttribute a in attrs)
                {
                    l.Add(a.attname);
                }
                return l.ToArray();
            }
            public Constraint ToConstraint(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (Constraint)o;
                }
                //Constraint c = null;
                //c = new Constraint(context, RefObject?.ownername, Schema?.nspname, conname, RefObject?.Schema?.nspname, RefObject?.relname, false);
                switch (contype)
                {
                    case 'c': // 検査制約
                        CheckConstraint cc = new CheckConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname, checkdef, false);
                        Generated = new WeakReference<NamedObject>(cc);
                        return cc;
                    case 'f': // 外部キー制約
                        ForeignKeyConstraint rc = new ForeignKeyConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname,
                            RefConstraint?.Schema?.nspname, RefConstraint?.conname, CharToForeignKeyRule[confupdtype], CharToForeignKeyRule[confdeltype], false, condeferrable, condeferred)
                        {
                            Columns = GetNameArray(Keys),
                            RefColumns = GetNameArray(RefKeys)
                        };
                        Generated = new WeakReference<NamedObject>(rc);
                        return rc;
                    case 'p': // プライマリキー制約
                    case 'u': // 一意性制約
                        if (Keys == null)
                        {
                            return null;
                        }
                        KeyConstraint kc = new KeyConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname, contype == 'p', false, condeferrable, condeferred)
                        {
                            Columns = new string[Keys.Length]
                        };
                        for (int i = 0; i < Keys.Length; i++)
                        {
                            kc.Columns[i] = Keys[i].attname;
                        }
                        Generated = new WeakReference<NamedObject>(kc);
                        return kc;
                    case 't': // 制約トリガ
                    case 'x': // 排他制約
                        return null;
                }
                return null;
            }
            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Schema?.nspname, conname) : ToIdentifier(conname);
            }

            public override string ToString()
            {
                return conname;
            }
        }
        internal abstract class PgOidSubid : IComparable
        {
            public abstract uint Oid { get; }
            public abstract int Subid { get; }
            public WeakReference<NamedObject> Generated;
            public virtual void FillReference(WorkingData working) { }
            public virtual void BeginFillReference(WorkingData working) { }
            public virtual void EndFillReference(WorkingData working) { }

            public abstract string GetIdentifier();

            public int CompareTo(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                {
                    return -1;
                }
                PgOidSubid a = (PgOidSubid)obj;
                int ret = Oid.CompareTo(a.Oid);
                if (ret != 0)
                {
                    return ret;
                }
                ret = Subid - a.Subid;
                return ret;
            }

        }
        internal class PgAttribute : PgOidSubid
        {
#pragma warning disable 0649
            public uint attrelid;
            public string attname;
            public uint atttypid;
            //public int attstattarget;
            //public int attlen;
            public int attnum;
            //public int attndims;
            //public int attcacheoff;
            public int atttypmod;
            //public bool attbyval;
            //public char attstorage;
            //public char attalign;
            public bool attnotnull;
            //public bool atthasdef;
            //public bool attisdropped;
            //public bool attislocal;
            //public int attinhcount;
            //public uint attcollation;
            //public int[] attacl;
            //public int[] attoptions;
            //public int[] attfdwoptions;

            public string formattype;
            public string defaultexpr;
#pragma warning restore 0649

            public override uint Oid { get { return attrelid; } }
            public override int Subid { get { return attnum; } }
            public PgClass Owner;
            public PgType DefType;
            public string DefTypeName;
            public PgType BaseType;
            public bool IsArray;
            public string ElemTypeName;
            public string BaseTypeName;
            public PgClass Sequence;
            //public override string Name { get { return string.Format("{0}_{1}", attrelid, attname); } }
            public static PgOidSubidCollection<PgAttribute> Attributes;

            public static PgOidSubidCollection<PgAttribute> Load(WorkingData working, NpgsqlConnection connection, PgOidSubidCollection<PgAttribute> store)
            {
                if (store == null)
                {
                    return new PgOidSubidCollection<PgAttribute>(DataSet.Properties.Resources.PgAttribute_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgAttribute_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgOidSubidCollection<PgAttribute> Load(WorkingData working, NpgsqlConnection connection)
            {
                Attributes = new PgOidSubidCollection<PgAttribute>(DataSet.Properties.Resources.PgAttribute_SQL, working, connection);
                return Attributes;
            }
            public static void FillByOid(PgOidSubidCollection<PgAttribute> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                store.RemoveByOid(oid);
                string sql = AddCondition(DataSet.Properties.Resources.PgAttribute_SQL, string.Format("attrelid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
            }
            private static string AbbrivatedTypeName(string typename)
            {
                if (typename.StartsWith("character varying"))
                {
                    return "varchar" + typename.Substring(17);
                }
                if (typename.StartsWith("timestamp") && typename.EndsWith(" without time zone"))
                {
                    return typename.Substring(0, typename.Length - 18);
                }
                return typename;
            }
            public override void FillReference(WorkingData working)
            {
                Owner = working.PgClasses.FindByOid(attrelid);
                if (Owner != null)
                {
                    Owner.Columns.Add(this);
                }
                DefTypeName = AbbrivatedTypeName(formattype);
                // 以降は EndFillReference にて定義
                BaseType = null;
                IsArray = false;
                BaseTypeName = null;
                ElemTypeName = null;
            }
            public override void BeginFillReference(WorkingData working) { }
            public override void EndFillReference(WorkingData working)
            {
                DefType = working.PgTypes.FindByOid(atttypid);
                //if (DefType != null && !string.IsNullOrEmpty(DefType.typname))
                //{
                //    DefTypeName = DefType.typname;
                //}
                BaseType = (DefType?.BaseType != null) ? DefType.BaseType : DefType;
                IsArray = false;
                ElemTypeName = null;
                BaseTypeName = BaseType?.typname;
                if (BaseType != null)
                {
                    IsArray = BaseType.IsArray;
                    ElemTypeName = BaseType.ElementType?.typname;
                }
            }
            public Column ToColumn(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (Column)o;
                }
                Column c = new Column(context, Owner?.Schema?.nspname, Owner?.relname, attname)
                {
                    DataType = DefTypeName,
                    BaseType = BaseTypeName,
                    DefaultValue = context.NormalizeSQL(defaultexpr),
                    NotNull = attnotnull
                };
                Type t;
                if (!TypeNameToType.TryGetValue(BaseTypeName, out t))
                {
                    if (IsArray && TypeNameToType.TryGetValue(ElemTypeName, out t))
                    {
                        try
                        {
                            t = Type.GetType(t.AssemblyQualifiedName + "[]");
                        }
                        catch
                        {
                            t = null;
                        }
                    }
                }
                c.ValueType = t;
                c.IsSupportedType = (t != null);
                if (attnum <= 0)
                {
                    c.HiddenLevel = (attname == "oid") ? HiddenLevel.Hidden : HiddenLevel.SystemInternal;
                    c.Index = attnum;
                }
                if (Sequence != null)
                {
                    c.Sequence = Sequence.ToSchemaObject(context) as Sequence;
                }
                Generated = new WeakReference<NamedObject>(c);
                return c;
            }

            public override string GetIdentifier()
            {
                return ToIdentifier(Owner?.Schema?.nspname, Owner?.relname, attname);
            }
            public override string ToString()
            {
                return attname;
            }
        }

        internal class PgDescription : PgObject
        {
#pragma warning disable 0649
            public uint objoid;
            public uint classoid;
            public int objsubid;
            public string description;
#pragma warning restore 0649
            public PgClass TargetClass;
            public PgConstraint TargetConstraint;
            public PgProc TargetProc;
            public PgTrigger TargetTrigger;
            public PgAttribute TargetAttribute;
            public PgType TargetType;
            public static PgObjectCollection<PgDescription> Descriptions;
            public static PgObjectCollection<PgDescription> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgDescription> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgDescription>(DataSet.Properties.Resources.PgDescription_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgDescription_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgDescription> Load(WorkingData working, NpgsqlConnection connection)
            {
                Descriptions = new PgObjectCollection<PgDescription>(DataSet.Properties.Resources.PgDescription_SQL, working, connection);
                return Descriptions;
            }
            public static void FillByOid(PgObjectCollection<PgDescription> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgDescription_SQL, string.Format("objoid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
            }

            public override void FillReference(WorkingData working)
            {
                TargetClass = working.PgClasses.FindByOid(objoid);
                TargetConstraint = working.PgConstraints.FindByOid(objoid);
                TargetProc = working.PgProcs.FindByOid(objoid);
                TargetTrigger = working.PgTriggers.FindByOid(objoid);
                TargetType = working.PgTypes.FindByOid(objoid);
                TargetAttribute = null;
                if (objsubid != 0)
                {
                    TargetAttribute = working.PgAttributes.FindByOidNum(objoid, objsubid);
                }
            }
            private string[] GetTargetName(bool fullName)
            {
                if (TargetAttribute != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetAttribute.Owner?.Schema?.nspname, TargetAttribute.Owner?.relname, TargetAttribute.attname };
                    }
                    else
                    {
                        return new string[] { TargetAttribute.Owner?.relname, TargetAttribute.attname };
                    }
                }
                if (TargetClass != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetClass.Schema?.nspname, TargetClass.relname };
                    }
                    else
                    {
                        return new string[] { TargetClass.relname };
                    }
                }
                if (TargetConstraint != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetConstraint.Schema?.nspname, TargetConstraint.conname };
                    }
                    else
                    {
                        return new string[] { TargetConstraint.conname };
                    }
                }
                if (TargetProc != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetProc.Schema?.nspname, TargetProc.GetIdentifier(false) };
                    }
                    else
                    {
                        return new string[] { TargetProc.GetIdentifier(false) };
                    }
                }
                if (TargetTrigger != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetTrigger.Target?.Schema.nspname, TargetTrigger.tgname };
                    }
                    else
                    {
                        return new string[] { TargetTrigger.Target?.Schema.nspname, TargetTrigger.tgname };
                    }
                }
                if (TargetType != null)
                {
                    if (fullName)
                    {
                        return new string[] { TargetType.Schema?.nspname, TargetType.typname };
                    }
                    else
                    {
                        return new string[] { TargetType.typname };
                    }
                }
                return StrUtil.EmptyStringArray;
            }

            public Comment ToComment(NpgsqlDataSet context)
            {
                Comment c;
                if (TargetAttribute != null)
                {
                    c = new ColumnComment(context, TargetClass?.Schema?.nspname, TargetClass?.relname, TargetAttribute.attname, description, true);
                }
                else if (TargetConstraint != null)
                {
                    c = new ConstraintComment(context, TargetConstraint.Schema?.nspname, TargetConstraint.Object.relname, TargetConstraint.conname, description, true);
                }
                else if (TargetProc != null)
                {
                    c = new StoredProcecureBaseComment(context, TargetProc.Schema?.nspname, TargetProc.proname, TargetProc.GetArgTypeStrs(), description, true);
                }
                else if (TargetTrigger != null)
                {
                    c = new TriggerComment(context, TargetTrigger.Target?.Schema?.nspname, TargetTrigger.Target?.relname, TargetTrigger.tgname, description, true);
                }
                else if (TargetClass != null)
                {
                    c = new Comment(context, TargetClass.Schema?.nspname, TargetClass.relname, null, description, true);
                }
                else if (TargetType != null)
                {
                    c = new Comment(context, TargetType.Schema?.nspname, TargetType.typname, null, description, true);
                }
                else
                {
                    return null;
                }
                c.Link();
                //Generated = c;
                return c;
            }

            public override string GetIdentifier(bool fullName)
            {
                return ToIdentifier(GetTargetName(fullName));
            }
            public override string ToString()
            {
                return description;
            }
        }

        internal class PgDependViewCollection : IReadOnlyList<PgDependView>
        {
            private readonly List<PgDependView> _items = new List<PgDependView>();

            public void Fill(string sql, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgDependViewCollection.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(PgDependView));
                            while (reader.Read())
                            {
                                PgDependView obj = new PgDependView();
                                ReadObject(obj, reader, mapper);
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
            }
            public void EndFillReference(WorkingData working)
            {
                foreach (PgDependView obj in _items)
                {
                    if (obj.Source != null && obj.View != null)
                    {
                        obj.Source.DependBy.Add(obj.View);
                    }
                }
            }
            public void FillReference(WorkingData working)
            {
                foreach (PgDependView obj in _items)
                {
                    obj.FillReference(working);
                }
            }

            public PgDependViewCollection(string sql, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, working, connection, false);
            }
            public PgDependViewCollection() { }

            #region IReadOnlyList の実装
            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public IEnumerator<PgDependView> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }

            public PgDependView this[int index] { get { return _items[index]; } }
            #endregion
        }

        internal class PgDependView
        {
#pragma warning disable 0649
            public uint source_oid;
            public uint view_oid;
#pragma warning restore 0649
            public PgObject Source;
            public PgClass View;
            public static PgDependViewCollection Load(WorkingData working, NpgsqlConnection connection, PgDependViewCollection store)
            {
                if (store == null)
                {
                    return new PgDependViewCollection(DataSet.Properties.Resources.PgDepend_View_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgDepend_View_SQL, working, connection, false);
                    return store;
                }
            }
            public static void FillByOid(PgDependViewCollection store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgDepend_View_SQL, string.Format("(d.refobjid = {0}::oid or rw.ev_class = {0}::oid)", oid));
                store.Fill(sql, working, connection, false);
            }
            public void FillReference(WorkingData working)
            {
                Source = (PgObject)working.PgClasses.FindByOid(source_oid) ?? working.PgProcs.FindByOid(source_oid);
                View = working.PgClasses.FindByOid(view_oid);
            }
        }
        internal class PgTrigger : PgObject
        {
#pragma warning disable 0649
            public uint tgrelid;
            public string tgname;
            public uint tgfoid;
            public short tgtype;
            //public char tgenabled;
            public bool tgisinternal;
            //public uint tgconstrrelid;
            //public uint tgconstrindid;
            //public uint tgconstraint;
            //public bool tgdeferrable;
            //public bool tginitdeferred;
            //public short tgnargs;
            public short[] tgattr;
            //public byte[] tgargs;
            public string triggerdef;
#pragma warning restore 0649
            public PgClass Target;
            public PgProc Procedure;
            public PgAttribute[] UpdateColumns;
            //public Schema Schema;
            //public static PgObjectCollection<PgTrigger> Triggers;
            public static PgObjectCollection<PgTrigger> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgTrigger> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgTrigger>(DataSet.Properties.Resources.PgTrigger_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgTrigger_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgTrigger> Load(WorkingData working, NpgsqlConnection connection)
            {
                return new PgObjectCollection<PgTrigger>(DataSet.Properties.Resources.PgTrigger_SQL, working, connection);
            }

            public static void FillByOid(PgObjectCollection<PgTrigger> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgTrigger_SQL, string.Format("tgrelid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
            }
            public override void FillReference(WorkingData working)
            {
                Target = working.PgClasses.FindByOid(tgrelid);
                Procedure = working.PgProcs.FindByOid(tgfoid);
                if (tgattr != null)
                {
                    List<PgAttribute> l = new List<PgAttribute>();
                    foreach (short n in tgattr)
                    {
                        PgAttribute a = working.PgAttributes.FindByOidNum(tgrelid, n);
                        if (a != null)
                        {
                            l.Add(a);
                        }
                    }
                    l.Sort();
                    UpdateColumns = l.ToArray();
                }
                //Schema = working.PgNamespaces.FindByOid(tg)
                //throw new NotImplementedException();
            }
            public override void EndFillReference(WorkingData working)
            {
                Procedure.DependBy.Add(this);
                base.EndFillReference(working);
            }
            private static readonly string[] TimingToText = new string[] { string.Empty, "before", "after", "instead of" };
            public Trigger ToTrigger(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (Trigger)o;
                }
                if (tgisinternal)
                {
                    return null;
                }
                if (Target == null)
                {
                    return null;
                }
                string def = null;
                if (Procedure != null)
                {
                    def = "execute procedure " + context.GetEscapedIdentifier(Procedure.Schema?.nspname, Procedure.proname, null, true) + "()";
                }
                Trigger t = new Trigger(context, Target.ownername, Target.Schema?.nspname, tgname, Target.Schema?.nspname, Target.relname, def, true)
                {
                    ProcedureSchema = Procedure?.Schema?.nspname,
                    ProcedureName = Procedure?.GetIdentifier(false),
                    Timing = ((tgtype & 2) != 0) ? TriggerTiming.Before : ((tgtype & 64) != 0) ? TriggerTiming.InsteadOf : TriggerTiming.After
                };

                t.TimingText = TimingToText[(int)t.Timing];
                t.Event = 0;
                t.EventText = string.Empty;
                if ((tgtype & 4) != 0)
                {
                    t.Event |= TriggerEvent.Insert;
                    t.EventText += " insert";
                }
                if ((tgtype & 8) != 0)
                {
                    t.Event |= TriggerEvent.Delete;
                    if (!string.IsNullOrEmpty(t.EventText))
                    {
                        t.EventText += " or";
                    }
                    t.EventText += " delete";
                }
                if ((tgtype & 32) != 0)
                {
                    t.Event |= TriggerEvent.Truncate;
                    if (!string.IsNullOrEmpty(t.EventText))
                    {
                        t.EventText += " or";
                    }
                    t.EventText += " truncate";
                }
                if ((tgtype & 16) != 0)
                {
                    t.Event |= TriggerEvent.Update;
                    if (!string.IsNullOrEmpty(t.EventText))
                    {
                        t.EventText += " or";
                    }
                    t.EventText += " update";
                }
                t.Orientation = ((tgtype & 1) != 0) ? TriggerOrientation.Row : TriggerOrientation.Statement;
                t.OrientationText = ((tgtype & 1) != 0) ? "row" : "statement";
                TokenizedPgsql sql = new TokenizedPgsql(triggerdef);
                StringBuilder buf = new StringBuilder();
                bool inWhen = false;
                foreach (PgsqlToken tk in sql)
                {
                    if (tk.ID == TokenID.Identifier)
                    {
                        if (string.Equals(tk.Value, "when", StringComparison.CurrentCultureIgnoreCase))
                        {
                            inWhen = true;
                            continue;
                        }
                        if (string.Equals(tk.Value, "execute", StringComparison.CurrentCultureIgnoreCase))
                        {
                            break;
                        }
                    }
                    if (!inWhen)
                    {
                        continue;
                    }
                    if (tk.ID == TokenID.Identifier)
                    {
                        buf.Append(NormalizeIdentifier(tk.Value));

                    }
                    else
                    {
                        buf.Append(tk.Value);
                    }
                }
                string when = buf.ToString().Trim();
                if (!string.IsNullOrEmpty(when))
                {
                    t.Condition = when;
                }
                if (UpdateColumns != null)
                {
                    foreach (PgAttribute a in UpdateColumns)
                    {
                        t.UpdateEventColumns.Add(context.GetEscapedIdentifier(a.attname, true));
                    }
                }
                Generated = new WeakReference<NamedObject>(t);
                return t;
            }

            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Target?.Schema?.nspname, Target?.relname, tgname) : ToIdentifier(Target?.relname, tgname);
            }
            public override string ToString()
            {
                return tgname;
            }
        }
        internal class PgTablespace : PgObject
        {
#pragma warning disable 0649
            public string spcname;
            public uint spcowner;
            public string[] spcoptions;
            public string location;
#pragma warning restore 0649
            public PgRole OwnerRole;
            public static PgObjectCollection<PgTablespace> Tablespaces;
            public static PgObjectCollection<PgTablespace> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgTablespace> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgTablespace>(DataSet.Properties.Resources.PgTablespace_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgTablespace_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgTablespace> Load(WorkingData working, NpgsqlConnection connection)
            {
                Tablespaces = new PgObjectCollection<PgTablespace>(DataSet.Properties.Resources.PgTablespace_SQL, working, connection);
                return Tablespaces;
            }

            public override void FillReference(WorkingData working)
            {
                OwnerRole = working.PgRoles.FindByOid(spcowner);
            }

            public PgsqlTablespace ToTablespace(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (PgsqlTablespace)o;
                }
                PgsqlTablespace ts = new PgsqlTablespace(context.Tablespaces)
                {
                    Oid = oid,
                    Name = spcname,
                    Options = spcoptions,
                    Path = location,
                    Owner = OwnerRole?.rolname
                };
                Generated = new WeakReference<NamedObject>(ts);
                return ts;

            }

            public override string GetIdentifier(bool fullName)
            {
                return spcname;
            }
            public override string ToString()
            {
                return spcname;
            }
        }

        internal class PgProc : PgObject
        {
#pragma warning disable 0649
            public string proname;
            public uint pronamespace;
            //public uint proowner;
            //public uint prolang;
            public float procost;
            public float prorows;
            //public uint provariadic;
            public string protransform;
            public char prokind;
            //public bool proisagg;
            public bool proiswindow;
            public bool prosecdef;
            public bool proleakproof;
            public bool proisstrict;
            public bool proretset;
            public char provolatile;
            public char proparallel;
            //public short pronargs;
            //public short pronargdefaults;
            public uint prorettype;
            public uint[] proargtypes;
            public uint[] proallargtypes;
            public char[] proargmodes;
            public string[] proargnames;
            ////public pg_node_tree proargdefaults;
            public uint[] protrftypes;
            public string prosrc;
            //public string probin;
            //public string[] proconfig;
            ////public aclitem[] proacl;
            public string ownername;
            public string lanname;
            //public string grant_check;  // pg_get_functiondef()のアクセス権がない場合は失敗するようにわざと入れている
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgType ReturnType;
            public PgType[] ArgTypes;
            public PgType[] AllArgTypes;
            public PgType[] TrfTypes;
            public Dictionary<int, string> ArgDefaults = new Dictionary<int, string>();

            internal string[] GetArgTypeStrs()
            {
                PgType[] args = ArgTypes;
                if (args == null)
                {
                    return StrUtil.EmptyStringArray;
                }
                List<string> l = new List<string>();
                foreach (PgType t in args)
                {
                    l.Add(t.formatname);
                }
                return l.ToArray();
            }
            internal string GetArgumentStr()
            {
                return StrUtil.DelimitedText(GetArgTypeStrs(), ",", "(", ")");
            }

            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Schema?.nspname, proname + GetArgumentStr()) : proname + GetArgumentStr();
            }
            public static void FillByOid(PgObjectCollection<PgProc> store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgProc_SQL, string.Format("p.oid = {0}::oid", oid));
                store.Fill(sql, working, connection, false);
                string sqlArg = AddCondition(DataSet.Properties.Resources.PgProc_ARGDEFAULTSQL, string.Format("ss.p_oid = {0}::oid", oid));
                store.JoinToDict<string>(sqlArg, typeof(PgProc).GetField("ArgDefaults"), "parameter_default", connection);
            }
            public static uint? GetOid(WorkingData backend, NpgsqlConnection connection, string schema, string name, string[] argtypes)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgProcOid_SQL, connection))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("schema", schema));
                    cmd.Parameters.Add(new NpgsqlParameter("name", name));
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                uint oid = reader.GetFieldValue<uint>(0);
                                string proname = reader.GetString(1);
                                uint[] proargtypes = reader.GetFieldValue<uint[]>(2);
                                if (proargtypes == null || proargtypes.Length == 0 && argtypes.Length == 0)
                                {
                                    return oid;
                                }
                                if (proargtypes.Length != argtypes.Length)
                                {
                                    continue;
                                }
                                bool matched = true;
                                for (int i = 0; i < proargtypes.Length; i++)
                                {
                                    PgType t = backend.PgTypes.FindByOid(proargtypes[i]);
                                    if (t.formatname != argtypes[i])
                                    {
                                        matched = false;
                                        break;
                                    }
                                }
                                if (matched)
                                {
                                    return oid;
                                }
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                    return null;
                }
            }
            public static PgObjectCollection<PgProc> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgProc> store)
            {
                string sql;
                if (connection.PostgreSqlVersion.Major < 10)
                {
                    sql = DataSet.Properties.Resources.PgProc9_SQL;
                }
                else if (connection.PostgreSqlVersion.Major == 10)
                {
                    sql = DataSet.Properties.Resources.PgProc10_SQL;
                }
                else
                {
                    sql = DataSet.Properties.Resources.PgProc_SQL;
                }
                PgObjectCollection<PgProc> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgProc>(sql, working, connection);
                }
                else
                {
                    l.Fill(sql, working, connection, false);
                }
                l.JoinToDict<string>(DataSet.Properties.Resources.PgProc_ARGDEFAULTSQL, typeof(PgProc).GetField("ArgDefaults"), "parameter_default", connection);
                return l;
            }

            public override void FillReference(WorkingData working)
            {
                Schema = working.PgNamespaces.FindByOid(pronamespace);
                ReturnType = working.PgTypes.FindByOid(prorettype);
                if (proargtypes != null)
                {
                    ArgTypes = new PgType[proargtypes.Length];
                    for (int i = 0; i < proargtypes.Length; i++)
                    {
                        ArgTypes[i] = working.PgTypes.FindByOid(proargtypes[i]);
                    }
                }
                if (proallargtypes != null)
                {
                    AllArgTypes = new PgType[proallargtypes.Length];
                    for (int i = 0; i < proallargtypes.Length; i++)
                    {
                        AllArgTypes[i] = working.PgTypes.FindByOid(proallargtypes[i]);
                    }
                }
                if (protrftypes != null)
                {
                    TrfTypes = new PgType[protrftypes.Length];
                    for (int i = 0; i < protrftypes.Length; i++)
                    {
                        TrfTypes[i] = working.PgTypes.FindByOid(protrftypes[i]);
                    }
                }
            }
            public override void BeginFillReference(WorkingData working)
            {
                ArgTypes = null;
                AllArgTypes = null;
            }
            public override void EndFillReference(WorkingData working)
            {
            }
            private ParameterDirection GetParameterDirection(int index)
            {
                if (proargmodes == null || index < 0 || proargmodes.Length <= index)
                {
                    return ParameterDirection.Input;
                }
                switch (proargmodes[index])
                {
                    case 'i':
                        return ParameterDirection.Input;
                    case 'o':
                        return ParameterDirection.Output;
                    case 'b':
                        return ParameterDirection.InputOutput;
                    case 'v':
                        return ParameterDirection.Input;
                    case 't':
                        return ParameterDirection.ReturnValue;
                    default:
                        return ParameterDirection.Input;
                }
                //return ParameterDirection.Input;
            }
            private ParameterDir GetParameterDir(int index)
            {
                if (proargmodes == null || index < 0 || proargmodes.Length <= index)
                {
                    return ParameterDir.Input;
                }
                switch (proargmodes[index])
                {
                    case 'i':
                        return ParameterDir.Input;
                    case 'o':
                        return ParameterDir.Output;
                    case 'v':
                        return ParameterDir.VariaDic;
                    case 't':
                        return ParameterDir.Table;
                    default:
                        return ParameterDir.Input;
                }
                //return ParameterDirection.Input;
            }
            private const float PROCOST_DEFAULT_C = 1.0f;
            private const float PROCOST_DEFAULT_INTERNAL = 1.0f;
            private const float PROCOST_DEFAULT = 100.0f;
            private const float PROROWS_DEFAULT = 1000.0f;
            private float GetDefaultCost()
            {
                switch (lanname)
                {
                    case "internal":
                    case "INTERNAL":
                        return PROCOST_DEFAULT_INTERNAL;
                    case "c":
                    case "C":
                        return PROCOST_DEFAULT_C;
                }
                return PROCOST_DEFAULT;
            }

            private string GetTransformTypeDefs()
            {
                if (TrfTypes == null || TrfTypes.Length == 0)
                {
                    return null;
                }
                StringBuilder buf = new StringBuilder("transform for type ");
                string prefix = string.Empty;
                foreach (PgType t in TrfTypes)
                {
                    buf.Append(prefix);
                    buf.Append(t.formatname);
                    prefix = ", ";
                }
                return buf.ToString();
            }

            private IReturnType GetReturnType()
            {
                if (!proretset)
                {
                    return new SimpleReturnType(ReturnType?.formatname);
                }
                PgType[] args = AllArgTypes ?? ArgTypes;
                if (args == null)
                {
                    return new SetOfReturnType(ReturnType?.formatname);
                }
                List<TableReturnType.Column> l = new List<TableReturnType.Column>();
                for (int i = 0; i < args.Length; i++)
                {
                    ParameterDir dir = GetParameterDir(i);
                    if (dir == ParameterDir.Table)
                    {
                        l.Add(new TableReturnType.Column(proargnames?[i], args[i]));
                    }
                }
                if (l.Count == 0)
                {
                    return new SetOfReturnType(ReturnType?.formatname);
                }
                return new TableReturnType(l.ToArray());
            }

            public StoredProcedureBase ToStoredProcedureBase(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (StoredProcedureBase)o;
                }
                StoredProcedureBase fn;
                if (prokind == 'p')
                {
                    fn = new StoredProcedure(context, ownername, Schema?.nspname, proname, prosrc, true)
                    {
                        Extension = Extension,
                        Language = lanname
                    };
                }
                else
                {
                    fn = new StoredFunction(context, ownername, Schema?.nspname, proname, prosrc, true)
                    {
                        ReturnType = GetReturnType(),
                        Extension = Extension,
                        Language = lanname
                    };
                }
                //if (fn.BaseType == null)
                //{
                //    fn.BaseType = fn.DataType;
                //}
                PgType[] args = AllArgTypes ?? ArgTypes;
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        ParameterDir dir = GetParameterDir(i);
                        if (dir == ParameterDir.Table)
                        {
                            continue;
                        }
                        Parameter p = new Parameter(fn)
                        {
                            Name = proargnames?[i],
                            Direction = GetParameterDirection(i),
                            DataType = args[i].formatname,
                            BaseType = args[i].BaseType?.typname
                        };
                        if (p.BaseType == null)
                        {
                            p.BaseType = p.DataType;
                        }
                        p.Index = i + 1;
                        Type t;
                        if (!TypeNameToType.TryGetValue(p.BaseType, out t))
                        {
                            t = typeof(string);
                        }
                        p.ValueType = t;
                        string v;
                        if (ArgDefaults.TryGetValue(p.Index, out v))
                        {
                            p.DefaultValue = v;
                        }
                    }
                }
                List<string> extra = new List<string>();
                string def = GetTransformTypeDefs();
                if (!string.IsNullOrEmpty(def))
                {
                    extra.Add(def);
                }
                if (prokind == 'w' || proiswindow)
                {
                    extra.Add("window");
                }
                switch (provolatile)
                {
                    case 'i':
                        extra.Add("immutable");
                        break;
                    //case 'v':
                    //    extra.Add("volatile");    // default option
                    //    break;
                    case 's':
                        extra.Add("stable");
                        break;
                }
                if (proleakproof)
                {
                    extra.Add("leakproof");
                }
                if (proisstrict)
                {
                    //extra.Add("returns null on null input");
                    extra.Add("strict");
                }
                //else
                //{
                //    extra.Add("called on null input");    // default option
                //}
                if (prosecdef)
                {
                    extra.Add("security definer");
                }
                //else
                //{
                //    extra.Add("security invoker");    // default option
                //}
                switch (proparallel)
                {
                    case 's':
                        extra.Add("parallel safe");
                        break;
                    case 'r':
                        extra.Add("parallel restricted");
                        break;
                        //case 'u':
                        //    extra.Add("parallel unsafe"); // default option
                        //    break;
                }
                if (procost != GetDefaultCost())
                {
                    extra.Add(string.Format("cost {0}", procost));
                }
                if (proretset && prorows != PROROWS_DEFAULT)
                {
                    extra.Add(string.Format("rows {0}", prorows));
                }
                fn.ExtraInfo = extra.ToArray();
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                fn.DependBy = l.ToArray();
                Generated = new WeakReference<NamedObject>(fn);
                return fn;
            }

            public override string ToString()
            {
                return proname;
            }
        }

        internal class PgDependCollection : IReadOnlyList<PgDepend>
        {
            private readonly List<PgDepend> _items = new List<PgDepend>();

            public void Fill(string sql, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("pg_trigger_oid", NpgsqlTypes.NpgsqlDbType.Oid) { Value = working.PgTriggerOid });
                    cmd.Parameters.Add(new NpgsqlParameter("pg_proc_oid", NpgsqlTypes.NpgsqlDbType.Oid) { Value = working.PgProcOid });
                    cmd.Parameters.Add(new NpgsqlParameter("pg_type_oid", NpgsqlTypes.NpgsqlDbType.Oid) { Value = working.PgTypeOid });
                    cmd.Parameters.Add(new NpgsqlParameter("pg_class_oid", NpgsqlTypes.NpgsqlDbType.Oid) { Value = working.PgClassOid });
                    LogDbCommand("PgDependCollection.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(PgDepend));
                            while (reader.Read())
                            {
                                PgDepend obj = new PgDepend();
                                ReadObject(obj, reader, mapper);
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
            }
            public void EndFillReference(WorkingData working)
            {
                foreach (PgDepend depend in _items)
                {
                    if (depend.Obj == null || depend.RefObj == null)
                    {
                        continue;
                    }
                    depend.RefObj.DependBy.Add(depend.Obj);
                    if (depend.RefObj is PgExtension)
                    {
                        depend.Obj.Extension = depend.RefObj.GetIdentifier(false);
                    }
                }
            }
            public void FillReference(WorkingData working)
            {
                foreach (PgDepend obj in _items)
                {
                    obj.FillReference(working);
                }
            }

            public PgDependCollection(string sql, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, working, connection, false);
            }
            public PgDependCollection() { }

            #region IReadOnlyList の実装
            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public IEnumerator<PgDepend> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }

            public PgDepend this[int index] { get { return _items[index]; } }
            #endregion
        }

        internal class PgDepend
        {
#pragma warning disable 0649
            public char deptype;
            public uint classid;
            public string classname;
            public uint objid;
            public int objsubid;
            public uint refclassid;
            public string refclassname;
            public uint refobjid;
            public int refobjsubid;
#pragma warning restore 0649
            public PgObject Obj;
            public PgAttribute Attribute;
            public PgObject RefObj;
            public PgAttribute RefAttribute;
            public static PgDependCollection Load(WorkingData working, NpgsqlConnection connection, PgDependCollection store)
            {
                PgDependCollection l = store;
                if (l == null)
                {
                    l = new PgDependCollection(DataSet.Properties.Resources.PgDepend_SQL, working, connection);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgDepend_SQL, working, connection, false);
                }
                return l;
            }
            private PgObject FindByOid(uint classid, uint oid, WorkingData working)
            {
                if (classid == working.PgExtensionOid)
                {
                    return working.PgExtensions.FindByOid(oid);
                }
                if (classid == working.PgTypeOid)
                {
                    return working.PgTypes.FindByOid(oid);
                }
                if (classid == working.PgProcOid)
                {
                    return working.PgProcs.FindByOid(oid);
                }
                if (classid == working.PgClassOid)
                {
                    return working.PgClasses.FindByOid(oid);
                }
                if (classid == working.PgTriggerOid)
                {
                    return working.PgTriggers.FindByOid(oid);
                }
                if (classid == working.PgLanguageOid)
                {
                    return null;
                }
                if (classid == working.PgForeignDataWrapperOid)
                {
                    return working.PgForeignDataWrappers.FindByOid(oid);
                }
                if (classid == working.PgForeignServerOid)
                {
                    return working.PgForeignServers.FindByOid(oid);
                }
                throw new NotImplementedException(string.Format("FindByOid({0}) not implemented.", classid));
            }
            public void FillReference(WorkingData working)
            {
                Obj = FindByOid(classid, objid, working);
                if (objsubid != 0)
                {
                    Attribute = working.PgAttributes.FindByOidNum(objid, objsubid);
                }
                RefObj = FindByOid(refclassid, refobjid, working);
                if (refobjsubid != 0)
                {
                    RefAttribute = working.PgAttributes.FindByOidNum(refobjid, refobjsubid);
                }
            }
            public static void FillByOid(PgDependCollection store, WorkingData working, NpgsqlConnection connection, uint oid)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgDepend_SQL, string.Format("(d.objid = {0}::oid or d.refobjid = {0}::oid)", oid));
                store.Fill(sql, working, connection, false);
            }
        }

        internal class PgExtension : PgObject
        {
#pragma warning disable 0649
            public string extname;
            public uint extowner;
            public uint extnamespace;
            public bool extrelocatable;
            public string extversion;
            public uint[] extconfig;
            public string[] extcondition;
            public string default_version;
#pragma warning restore 0649
            public PgRole Owner;
            public PgNamespace Schema;
            public PgClass[] ExtConfig;
            public static PgObjectCollection<PgExtension> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgExtension> store)
            {
                PgObjectCollection<PgExtension> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgExtension>(DataSet.Properties.Resources.PgExtension_SQL, working, connection);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgExtension_SQL, working, connection, false);
                }
                return l;
            }
            public override void FillReference(WorkingData working)
            {
                Owner = working.PgRoles.FindByOid(extowner);
                Schema = working.PgNamespaces.FindByOid(extnamespace);
                List<PgClass> l = new List<PgClass>();
                if (extconfig != null)
                {
                    foreach (uint oid in extconfig)
                    {
                        l.Add(working.PgClasses.FindByOid(oid));
                    }
                }
                ExtConfig = l.ToArray();
            }

            public override string GetIdentifier(bool fullName)
            {
                return fullName ? ToIdentifier(Schema?.nspname, extname) : extname;
            }

            public PgsqlExtension ToPgsqlExtension(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (PgsqlExtension)o;
                }

                var ret = new PgsqlExtension(context, Owner?.rolname, Schema?.nspname, extname)
                {
                    IsHidden = context.IsHiddenSchema(Schema?.nspname),
                    Version = extversion,
                    Relocatable = extrelocatable,
                    DefaultVersion = default_version,
                    //Configurations = new 
                };
                int n = ExtConfig.Length;
                ret.Configurations = new PgsqlExtension.ConfigTable[n];
                for (int i = 0; i < n; i++)
                {
                    ret.Configurations[i] = new PgsqlExtension.ConfigTable(context.Tables[ExtConfig[i].GetIdentifier(true)], extcondition[i]);
                }
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                ret.DependBy = l.ToArray();
                return ret;
            }
        }

        internal class PgDatabase : PgObject
        {
#pragma warning disable 0649
            public string datname;
            public uint datdba;
            public string dbaname;
            public int encoding;
            public string encoding_char;
            public string datcollate;
            public string datctype;
            public bool datistemplate;
            public bool datallowconn;
            public int datconnlimit;
            //public uint datlastsysoid;
            public uint datfrozenxid;
            public uint datminmxid;
            public uint dattablespace;
            public string dattablespacename;
            public string version;
            public string version_num;
#pragma warning restore 0649
            public bool IsCurrent;
            public static string current_database;
            public static PgObjectCollection<PgDatabase> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgDatabase> store)
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand("select current_database()", connection))
                {
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                current_database = reader.GetString(0);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
                if (store == null)
                {
                    return new PgObjectCollection<PgDatabase>(DataSet.Properties.Resources.PgDatabase_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgDatabase_SQL, working, connection, false);
                    return store;
                }
            }

            public static PgObjectCollection<PgDatabase> FillByDatname(PgObjectCollection<PgDatabase> store, WorkingData working, NpgsqlConnection connection, string datname)
            {
                string sql = AddCondition(DataSet.Properties.Resources.PgDatabase_SQL, string.Format("datname = {0}", ToLiteralStr(datname)));
                if (store == null)
                {
                    return new PgObjectCollection<PgDatabase>(sql, working, connection);
                }
                else
                {
                    store.Fill(sql, working, connection, false);
                    return store;
                }

            }

            public override void FillReference(WorkingData working)
            {
                IsCurrent = (datname == current_database);
            }

            public PgsqlDatabase ToDatabase(NpgsqlDataSet context)
            {
                int v;
                if (!int.TryParse(version_num, out v))
                {
                    v = 0;
                }
                PgsqlDatabase ret = new PgsqlDatabase(context, datname)
                {
                    //Name = datname,
                    ConnectionInfo = context.ConnectionInfo,
                    Encoding = encoding_char,
                    DefaultTablespace = dattablespacename,
                    //DbaUserName = datdba,
                    DbaUserName = dbaname,
                    IsCurrent = IsCurrent,
                    Version = version,
                    VersionNum = new int[] { v / 10000, v % 10000 },
                    IsTemplate = datistemplate,
                    LcCollate = datcollate,
                    LcCtype = datctype
                };
                return ret;
            }

            public override string GetIdentifier(bool fullName)
            {
                return datname;
            }
            public override string ToString()
            {
                return datname;
            }
        }

        internal class PgSettingCollection : IReadOnlyList<PgSetting>
        {
            private readonly List<PgSetting> _items = new List<PgSetting>();

            public void Fill(string sql, WorkingData working, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgSettingCollection.Fill", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            FieldInfo[] mapper = CreateMapper(reader, typeof(PgSetting));
                            while (reader.Read())
                            {
                                PgSetting obj = new PgSetting();
                                ReadObject(obj, reader, mapper);
                                _items.Add(obj);
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
            }
            public void EndFillReference(WorkingData working)
            {
            }
            public void FillReference(WorkingData working)
            {
                foreach (PgSetting obj in _items)
                {
                    obj.FillReference(working);
                }
            }

            public PgSettingCollection(string sql, WorkingData working, NpgsqlConnection connection)
            {
                Fill(sql, working, connection, false);
            }
            public PgSettingCollection() { }

            #region IReadOnlyList の実装
            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public IEnumerator<PgSetting> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }

            public PgSetting this[int index] { get { return _items[index]; } }
            #endregion
        }
        internal class PgSetting : IComparable
        {
#pragma warning disable 0649
            public string name;
            public string setting;
            public string unit;
            public string category;
            public string short_desc;
            public string extra_desc;
            public string context;
            public string vartype;
            public string source;
            public string min_val;
            public string max_val;
            public string[] enumvals;
            public string boot_val;
            public string reset_val;
            public string sourcefile;
            public int? sourceline;
            public bool pending_restart;
#pragma warning restore 0649

            public static PgSettingCollection Load(WorkingData working, NpgsqlConnection connection, PgSettingCollection store)
            {
                if (store == null)
                {
                    return new PgSettingCollection(DataSet.Properties.Resources.PgSettings_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgSettings_SQL, working, connection, false);
                    return store;
                }
            }

            public void FillReference(WorkingData working)
            {
            }

            public PgsqlSetting ToPgsqlSetting(NpgsqlDataSet context)
            {
                return new PgsqlSetting()
                {
                    Name = name,
                    Setting = setting,
                    Unit = unit,
                    VarType = vartype,
                    EnumVals = enumvals,
                    Category = category,
                    ShortDesc = short_desc,
                    ExtraDesc = extra_desc,
                    Context = this.context,
                    Source = source,
                    BootVal = boot_val,
                    ResetVal = reset_val,
                    PendingRestart = pending_restart
                };
            }

            public int CompareTo(object obj)
            {
                if (!(obj is PgSetting))
                {
                    return -1;
                }
                return name.CompareTo(((PgSetting)obj).name);
            }
            public override bool Equals(object obj)
            {
                if (!(obj is PgSetting))
                {
                    return false;
                }
                return string.Compare(name, ((PgSetting)obj).name) == 0;
            }
            public override int GetHashCode()
            {
                if (string.IsNullOrEmpty(name))
                {
                    return 0;
                }
                return name.GetHashCode();
            }
            public override string ToString()
            {
                return string.Format("{0}: {1}", name, setting);
            }
        }

        internal class PgForeignDataWrapper : PgObject
        {
#pragma warning disable 0649
            public string fdwname;
            public uint fdwowner;
            public uint fdwhandler;
            public uint fdwvalidator;
            //public aclitem[] fdwacl;
            public string[] fdwoptions;
#pragma warning restore 0649
            public PgRole Owner;
            public PgProc Handler;
            public PgProc Validator;
            public static PgObjectCollection<PgForeignDataWrapper> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgForeignDataWrapper> store)
            {
                PgObjectCollection<PgForeignDataWrapper> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgForeignDataWrapper>(DataSet.Properties.Resources.PgForeignDataWrapper_SQL, working, connection);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgForeignDataWrapper_SQL, working, connection, false);
                }
                return l;
            }
            public override void FillReference(WorkingData working)
            {
                Owner = working.PgRoles.FindByOid(fdwowner);
                Handler = working.PgProcs.FindByOid(fdwhandler);
                Validator = working.PgProcs.FindByOid(fdwvalidator);
            }

            public override string GetIdentifier(bool fullName)
            {
                return fdwname;
            }

            public PgsqlForeignDataWrapper ToPgsqlForeignDataWrapper(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (PgsqlForeignDataWrapper)o;
                }

                var ret = new PgsqlForeignDataWrapper(context, Owner?.rolname, null, fdwname)
                {
                    Handler = Handler?.proname,
                    Validator = Validator?.proname,
                    Options = fdwoptions
                };
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                ret.DependBy = l.ToArray();
                return ret;
            }
        }

        internal class PgForeignServer : PgObject
        {
#pragma warning disable 0649
            public string srvname;
            public uint srvowner;
            public uint srvfdw;
            public string srvtype;
            public string srvversion;
            //public aclitem[] srvacl;
            public string[] srvoptions;
#pragma warning restore 0649
            public PgRole Owner;
            public PgForeignDataWrapper Fdw;

            public static PgObjectCollection<PgForeignServer> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgForeignServer> store)
            {
                PgObjectCollection<PgForeignServer> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgForeignServer>(DataSet.Properties.Resources.PgForeignServer_SQL, working, connection);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgForeignDataWrapper_SQL, working, connection, false);
                }
                return l;
            }
            public override void FillReference(WorkingData working)
            {
                Owner = working.PgRoles.FindByOid(srvowner);
                Fdw = working.PgForeignDataWrappers.FindByOid(srvfdw);
            }

            public override string GetIdentifier(bool fullName)
            {
                return srvname;
            }

            public PgsqlForeignServer ToPgsqlForeignServer(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (PgsqlForeignServer)o;
                }

                var ret = new PgsqlForeignServer(context, Owner?.rolname, null, srvname)
                {
                    Fdw = Fdw.fdwname,
                    ServerType = srvtype,
                    Version = srvversion,
                    Options = srvoptions
                };
                List<string> l = new List<string>();
                TrimDependBy();
                foreach (PgObject c in DependBy)
                {
                    l.Add(c.GetIdentifier(true));
                }
                l.Sort();
                ret.DependBy = l.ToArray();
                return ret;
            }

        }

        private void LoadUserInfo(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (conn == null)
            {
                return;
            }
            DisableChangeLog();
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.NpgsqlDataSet_USERINFOSQL, conn))
                {
                    LogDbCommand("NpgsqlDataSet.LoadUserInfo", cmd);
                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CurrentSchema = reader.GetValue(0).ToString();
                            }
                        }
                    }
                    catch (PostgresException t) when (t.SqlState == ERROR_CODE_PERMISSION_DENIED) { }
                }
            }
            finally
            {
                EnableChangeLog();
            }
        }

        internal class PgRole : PgObject
        {
#pragma warning disable 0649
            public string rolname;
            public bool rolsuper;
            public bool rolinherit = true;
            public bool rolcreaterole;
            public bool rolcreatedb;
            public bool rolcanlogin;
            public bool rolreplication;
            public int rolconnlimit = -1;
            //public string rolpassword;
            public DateTime? rolvaliduntil;
            public bool rolbypassrls;
            public string[] rolconfig;
#pragma warning restore 0649

            public static PgObjectCollection<PgRole> Roles;
            public static PgObjectCollection<PgRole> Load(WorkingData working, NpgsqlConnection connection, PgObjectCollection<PgRole> store)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgRole>(DataSet.Properties.Resources.PgRoles_SQL, working, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgRoles_SQL, working, connection, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgRole> Load(WorkingData working, NpgsqlConnection connection)
            {
                Roles = new PgObjectCollection<PgRole>(DataSet.Properties.Resources.PgRoles_SQL, working, connection);
                return Roles;
            }

            public override void FillReference(WorkingData working)
            {
            }

            public PgsqlUser ToUser(NpgsqlDataSet context)
            {
                NamedObject o;
                if (Generated != null && Generated.TryGetTarget(out o))
                {
                    return (PgsqlUser)o;
                }
                PgsqlUser u = new PgsqlUser(context.Users)
                {
                    Oid = oid,
                    Id = rolname,
                    Name = rolname,
                    CanLogin = rolcanlogin,
                    IsInherit = rolinherit,
                    CanCreateDb = rolcreatedb,
                    CanCreateRole = rolcreaterole,
                    IsSuperUser = rolsuper,
                    Replication = rolreplication,
                    BypassRowLevelSecurity = rolbypassrls,
                    PasswordExpiration = rolvaliduntil ?? DateTime.MaxValue,
                    Config = rolconfig,
                    ConnectionLimit = (rolconnlimit != -1) ? rolconnlimit : (int?)null
                };
                Generated = new WeakReference<NamedObject>(u);
                return u;
            }

            public override string GetIdentifier(bool fullName)
            {
                return rolname;
            }
            public override string ToString()
            {
                return rolname;
            }
        }
        internal class WorkingData
        {
            public NpgsqlDataSet Context;

            public PgObjectCollection<PgClass> PgClasses;
            //private Dictionary<string, PgClass> _nameToPgClass = null;
            //private Dictionary<string, PgAttribute> _nameToPgAttribute = null;
            public PgObjectCollection<PgConstraint> PgConstraints;
            public PgObjectCollection<PgNamespace> PgNamespaces;
            public PgOidSubidCollection<PgAttribute> PgAttributes;
            public PgObjectCollection<PgDescription> PgDescriptions;
            public PgObjectCollection<PgTrigger> PgTriggers;
            public PgObjectCollection<PgTablespace> PgTablespaces;
            public PgObjectCollection<PgDatabase> PgDatabases;
            public PgObjectCollection<PgType> PgTypes;
            public PgObjectCollection<PgProc> PgProcs;
            public PgObjectCollection<PgRole> PgRoles;
            public PgObjectCollection<PgExtension> PgExtensions;
            public PgObjectCollection<PgForeignDataWrapper> PgForeignDataWrappers;
            public PgObjectCollection<PgForeignServer> PgForeignServers;
            public PgSettingCollection PgSettings;
            public PgDependViewCollection PgDependViews;
            public PgDependCollection PgDepends;

            public uint PgCatalogOid;

            public uint PgClassOid;
            public uint PgConstraintOid;
            public uint PgNamespaceOid;
            public uint PgDescriptionOid;
            public uint PgTriggerOid;
            public uint PgTablespaceOid;
            public uint PgTypeOid;
            public uint PgProcOid;
            public uint PgLanguageOid;
            public uint PgExtensionOid;
            public uint PgForeignDataWrapperOid;
            public uint PgForeignServerOid;

            public void FillAll(NpgsqlConnection connection)
            {
                Context.LoadEncodings(connection);

                PgClassOid = PgClass.GetOid(connection, "pg_catalog", "pg_class", 'r').Value;
                PgConstraintOid = PgClass.GetOid(connection, "pg_catalog", "pg_constraint", 'r').Value;
                PgNamespaceOid = PgClass.GetOid(connection, "pg_catalog", "pg_namespace", 'r').Value;
                PgDescriptionOid = PgClass.GetOid(connection, "pg_catalog", "pg_description", 'r').Value;
                PgTriggerOid = PgClass.GetOid(connection, "pg_catalog", "pg_trigger", 'r').Value;
                PgTablespaceOid = PgClass.GetOid(connection, "pg_catalog", "pg_tablespace", 'r').Value;
                PgTypeOid = PgClass.GetOid(connection, "pg_catalog", "pg_type", 'r').Value;
                PgProcOid = PgClass.GetOid(connection, "pg_catalog", "pg_proc", 'r').Value;
                PgLanguageOid = PgClass.GetOid(connection, "pg_catalog", "pg_language", 'r').Value;
                PgExtensionOid = PgClass.GetOid(connection, "pg_catalog", "pg_extension", 'r').Value;
                PgForeignDataWrapperOid = PgClass.GetOid(connection, "pg_catalog", "pg_foreign_data_wrapper", 'r').Value;
                PgForeignServerOid = PgClass.GetOid(connection, "pg_catalog", "pg_foreign_server", 'r').Value;

                PgNamespaces = PgNamespace.Load(this, connection, null);
                PgNamespaces.UpdateNameToItem();
                PgCatalogOid = PgNamespaces.FindByName("pg_catalog").oid;

                PgTablespaces = PgTablespace.Load(this, connection, null);
                PgDatabases = PgDatabase.Load(this, connection, null);
                PgClasses = PgClass.Load(this, connection, null);
                //_nameToPgClass = null;
                //_nameToPgAttribute = null;
                PgTypes = PgType.Load(this, connection, null);
                PgAttributes = PgAttribute.Load(this, connection, null);
                PgProcs = PgProc.Load(this, connection, null);
                PgConstraints = PgConstraint.Load(this, connection, null);
                PgTriggers = PgTrigger.Load(this, connection, null);
                PgDescriptions = PgDescription.Load(this, connection, null);
                PgRoles = PgRole.Load(this, connection, null);
                PgExtensions = PgExtension.Load(this, connection, null);
                PgForeignDataWrappers = PgForeignDataWrapper.Load(this, connection, null);
                PgForeignServers = PgForeignServer.Load(this, connection, null);
                PgSettings = PgSetting.Load(this, connection, null);
                PgDependViews = PgDependView.Load(this, connection, null);
                PgDepends = PgDepend.Load(this, connection, null);

                PgNamespaces.BeginFillReference(this);
                PgTablespaces.BeginFillReference(this);
                PgDatabases.BeginFillReference(this);
                PgClasses.BeginFillReference(this);
                PgTypes.BeginFillReference(this);
                PgAttributes.BeginFillReference(this);
                PgProcs.BeginFillReference(this);
                PgConstraints.BeginFillReference(this);
                PgTriggers.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);
                PgRoles.BeginFillReference(this);
                PgExtensions.BeginFillReference(this);
                PgForeignDataWrappers.BeginFillReference(this);
                PgForeignServers.BeginFillReference(this);
                PgSettings.BeginFillReference(this);
                PgDependViews.BeginFillReference(this);
                PgDepends.BeginFillReference(this);

                PgNamespaces.FillReference(this);
                PgTablespaces.FillReference(this);
                PgDatabases.FillReference(this);
                PgClasses.FillReference(this);
                //_nameToPgClass = null;
                //_nameToPgAttribute = null;
                PgTypes.FillReference(this);
                PgAttributes.FillReference(this);
                PgProcs.FillReference(this);
                PgConstraints.FillReference(this);
                PgTriggers.FillReference(this);
                PgDescriptions.FillReference(this);
                PgRoles.FillReference(this);
                PgExtensions.FillReference(this);
                PgForeignDataWrappers.FillReference(this);
                PgForeignServers.FillReference(this);
                PgSettings.FillReference(this);
                PgDependViews.FillReference(this);
                PgDepends.FillReference(this);

                //PgNamespaces.UpdateNameToItem();
                PgTablespaces.UpdateNameToItem();
                PgDatabases.UpdateNameToItem();
                PgClasses.UpdateNameToItem();
                PgTypes.UpdateNameToItem();
                PgAttributes.UpdateNameToItem();
                PgProcs.UpdateNameToItem();
                PgConstraints.UpdateNameToItem();
                PgTriggers.UpdateNameToItem();
                PgDescriptions.UpdateNameToItem();
                PgExtensions.UpdateNameToItem();
                PgForeignDataWrappers.UpdateNameToItem();
                PgForeignServers.UpdateNameToItem();
                PgRoles.UpdateNameToItem();
                //PgSettings.UpdateNameToItem();
                //PgDependViews.UpdateNameToItem();
                //PgDepends.UpdateNameToItem();

                PgNamespaces.EndFillReference(this);
                PgTablespaces.EndFillReference(this);
                PgDatabases.EndFillReference(this);
                PgClasses.EndFillReference(this);
                PgTypes.EndFillReference(this);
                PgAttributes.EndFillReference(this);
                PgProcs.EndFillReference(this);
                PgConstraints.EndFillReference(this);
                PgTriggers.EndFillReference(this);
                PgDescriptions.EndFillReference(this);
                PgRoles.EndFillReference(this);
                PgExtensions.EndFillReference(this);
                PgForeignDataWrappers.EndFillReference(this);
                PgForeignServers.EndFillReference(this);
                PgSettings.EndFillReference(this);
                PgDependViews.EndFillReference(this);
                PgDepends.EndFillReference(this);

                LoadFromPgNamespaces();
                LoadFromPgTablespaces();
                LoadFromPgDatabases();
                LoadFromPgClass();
                LoadFromPgType();
                LoadFromPgProc();
                LoadFromPgConstraint();
                LoadFromPgTrigger();
                LoadFromPgDescription();
                LoadFromPgRoles();
                LoadFromPgExtensions();
                LoadFromPgForeignDataWrappers();
                LoadFromPgForeignServers();
                LoadFromPgSettings();
                //LoadFromPgDepend();
            }

            private void FillTableByOidInternal(uint oid, NpgsqlConnection connection, Dictionary<uint, bool> loadedOids)
            {
                if (loadedOids.ContainsKey(oid))
                {
                    return;
                }
                PgClass.FillByOid(PgClasses, this, connection, oid);
                foreach (uint reloid in PgClass.GetRelatedOid(connection, oid))
                {
                    PgClass.FillByOid(PgClasses, this, connection, reloid);
                }
                PgAttribute.FillByOid(PgAttributes, this, connection, oid);
                PgConstraint.FillByOid(PgConstraints, this, connection, oid);
                PgDescription.FillByOid(PgDescriptions, this, connection, oid);
                PgTrigger.FillByOid(PgTriggers, this, connection, oid);
                PgDependView.FillByOid(PgDependViews, this, connection, oid);
                PgDepend.FillByOid(PgDepends, this, connection, oid);
                loadedOids[oid] = true;
                foreach (uint reloid in PgConstraint.GetRefTableOid(connection, oid))
                {
                    FillTableByOidInternal(reloid, connection, loadedOids);
                }
            }
            public void FillSelectableByOid(uint oid, NpgsqlConnection connection)
            {
                Dictionary<uint, bool> loadedOids = new Dictionary<uint, bool>();
                FillTableByOidInternal(oid, connection, loadedOids);

                PgClasses.BeginFillReference(this);
                PgAttributes.BeginFillReference(this);
                PgConstraints.BeginFillReference(this);
                PgTriggers.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);
                PgDependViews.BeginFillReference(this);
                PgDepends.BeginFillReference(this);

                PgClasses.FillReference(this);
                PgAttributes.FillReference(this);
                PgConstraints.FillReference(this);
                PgTriggers.FillReference(this);
                PgDescriptions.FillReference(this);
                PgDependViews.FillReference(this);
                PgDepends.FillReference(this);

                PgClasses.UpdateNameToItem();
                PgAttributes.UpdateNameToItem();
                PgConstraints.UpdateNameToItem();
                PgTriggers.UpdateNameToItem();
                PgDescriptions.UpdateNameToItem();

                PgClasses.EndFillReference(this);
                PgAttributes.EndFillReference(this);
                PgConstraints.EndFillReference(this);
                PgTriggers.EndFillReference(this);
                PgDescriptions.EndFillReference(this);
                PgDependViews.EndFillReference(this);
                PgDepends.EndFillReference(this);

                LoadFromPgClass();
                LoadFromPgConstraint();
                LoadFromPgTrigger();
                LoadFromPgDescription();
            }
            //public void FillViewByOid(uint oid, NpgsqlConnection connection)
            //{
            //    Dictionary<uint, bool> loadedOids = new Dictionary<uint, bool>();
            //    FillTableByOidInternal(oid, connection, loadedOids);

            //    PgClasses.BeginFillReference(this);
            //    PgAttributes.BeginFillReference(this);
            //    PgConstraints.BeginFillReference(this);
            //    PgTriggers.BeginFillReference(this);
            //    PgDescriptions.BeginFillReference(this);

            //    PgClasses.FillReference(this);
            //    PgAttributes.FillReference(this);
            //    PgConstraints.FillReference(this);
            //    PgTriggers.FillReference(this);
            //    PgDescriptions.FillReference(this);

            //    PgClasses.UpdateNameToItem();
            //    PgAttributes.UpdateNameToItem();
            //    PgConstraints.UpdateNameToItem();
            //    PgTriggers.UpdateNameToItem();
            //    PgDescriptions.UpdateNameToItem();

            //    PgClasses.EndFillReference(this);
            //    PgAttributes.EndFillReference(this);
            //    PgConstraints.EndFillReference(this);
            //    PgTriggers.EndFillReference(this);
            //    PgDescriptions.EndFillReference(this);

            //    LoadFromPgClass();
            //    LoadFromPgConstraint();
            //    LoadFromPgTrigger();
            //    LoadFromPgDescription();
            //}
            public void FillTypeByOid(uint oid, NpgsqlConnection connection)
            {
                PgClass.FillByOid(PgClasses, this, connection, oid);
                PgType.FillByOid(PgTypes, this, connection);
                PgAttribute.FillByOid(PgAttributes, this, connection, oid);
                PgDescription.FillByOid(PgDescriptions, this, connection, oid);
                PgDependView.FillByOid(PgDependViews, this, connection, oid);
                PgDepend.FillByOid(PgDepends, this, connection, oid);

                PgClasses.BeginFillReference(this);
                PgTypes.BeginFillReference(this);
                PgAttributes.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);
                PgDependViews.BeginFillReference(this);
                PgDepends.BeginFillReference(this);

                PgClasses.FillReference(this);
                PgTypes.FillReference(this);
                PgAttributes.FillReference(this);
                PgDescriptions.FillReference(this);
                PgDependViews.FillReference(this);
                PgDepends.FillReference(this);

                PgClasses.UpdateNameToItem();
                PgTypes.UpdateNameToItem();
                PgAttributes.UpdateNameToItem();
                PgDescriptions.UpdateNameToItem();

                PgClasses.EndFillReference(this);
                PgTypes.EndFillReference(this);
                PgAttributes.EndFillReference(this);
                PgDescriptions.EndFillReference(this);
                PgDependViews.EndFillReference(this);
                PgDepends.EndFillReference(this);

                LoadFromPgClass();
                LoadFromPgConstraint();
                LoadFromPgDescription();
            }
            public void FillProcByOid(uint oid, NpgsqlConnection connection)
            {
                PgProc.FillByOid(PgProcs, this, connection, oid);
                PgDescription.FillByOid(PgDescriptions, this, connection, oid);
                PgDependView.FillByOid(PgDependViews, this, connection, oid);
                PgDepend.FillByOid(PgDepends, this, connection, oid);

                PgProcs.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);
                PgDependViews.BeginFillReference(this);
                PgDepends.BeginFillReference(this);

                PgProcs.FillReference(this);
                PgDescriptions.FillReference(this);
                PgDependViews.FillReference(this);
                PgDepends.FillReference(this);

                PgProcs.UpdateNameToItem();
                PgDescriptions.UpdateNameToItem();

                PgProcs.EndFillReference(this);
                PgDescriptions.EndFillReference(this);
                PgDependViews.EndFillReference(this);
                PgDepends.EndFillReference(this);

                LoadFromPgProc();
                LoadFromPgDescription();
            }

            public void FillSequenceByOid(uint oid, NpgsqlConnection connection)
            {
                PgClass.FillByOid(PgClasses, this, connection, oid);
                PgDescription.FillByOid(PgDescriptions, this, connection, oid);

                PgClasses.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);

                PgClasses.FillReference(this);
                PgDescriptions.FillReference(this);

                PgClasses.EndFillReference(this);
                PgDescriptions.EndFillReference(this);

                LoadFromPgClass();
                LoadFromPgDescription();
            }

            public void FillDatabaseByName(string databaseName, WorkingData working, NpgsqlConnection connection)
            {
                //PgRole.Load(connection);
                //PgTablespace.Load(connection);
                PgDatabase.FillByDatname(PgDatabases, working, connection, databaseName);
                PgDatabases.BeginFillReference(this);
                PgDatabases.FillReference(this);
                PgDatabases.EndFillReference(this);
            }

            public void FillSettings(NpgsqlConnection connection)
            {
                PgSettings = PgSetting.Load(this, connection, null);
                PgSettings.BeginFillReference(this);
                PgSettings.FillReference(this);
                PgSettings.EndFillReference(this);
                LoadFromPgSettings();
            }

            public void FillTablespaces(NpgsqlConnection connection)
            {
                PgNamespaces = PgNamespace.Load(this, connection);
                LoadFromPgNamespaces();
            }

            public void FillUsers(NpgsqlConnection connection)
            {
                PgRoles = PgRole.Load(this, connection);
                LoadFromPgRoles();
            }

            public WorkingData(NpgsqlDataSet context)
            {
                Context = context;
            }
            private void LoadFromPgNamespaces()
            {
                foreach (PgNamespace ns in PgNamespaces)
                {
                    ns.ToSchema(Context);

                    //if (!Context.IsHiddenSchema(ns.nspname))
                    //{
                    //    Context.RequireSchema(ns.nspname);
                    //}
                }
            }
            private void LoadFromPgTablespaces()
            {
                List<string> l = new List<string>();
                foreach (PgTablespace ts in PgTablespaces)
                {
                    Tablespace t = ts.ToTablespace(Context);
                    l.Add(t.Name);
                }
                l.Sort();
                Context.TablespaceNames = l.ToArray();
            }

            private void LoadFromPgSettings()
            {
                if (Context == null || Context.Database == null)
                {
                    return;
                }
                PgsqlDatabase db = Context.Database;
                PgsqlSettingCollection l = db.Settings;
                if (l == null)
                {
                    return;
                }
                l.Clear();
                foreach (PgSetting s in PgSettings)
                {
                    l.Add(s.ToPgsqlSetting(Context));
                }
            }

            private void LoadFromPgRoles()
            {
                if (Context == null || Context.Database == null)
                {
                    return;
                }
                List<string> l = new List<string>();
                foreach (PgRole ts in PgRoles)
                {
                    PgsqlUser u = ts.ToUser(Context);
                    if (u.CanLogin)
                    {
                        l.Add(u.Id);
                    }
                }
                l.Sort();
                Context.UserIds = l.ToArray();
            }

            private void LoadFromPgExtensions()
            {
                foreach (PgExtension ext in PgExtensions)
                {
                    ext.ToPgsqlExtension(Context);
                }
            }

            private void LoadFromPgForeignDataWrappers()
            {
                foreach (PgForeignDataWrapper fdw in PgForeignDataWrappers)
                {
                    fdw.ToPgsqlForeignDataWrapper(Context);
                }
            }

            private void LoadFromPgForeignServers()
            {
                foreach (PgForeignServer svr in PgForeignServers)
                {
                    svr.ToPgsqlForeignServer(Context);
                }
            }

            private void LoadFromPgDatabases()
            {
                List<PgsqlDatabase> lTmpl = new List<PgsqlDatabase>();
                List<PgsqlDatabase> lOther = new List<PgsqlDatabase>();
                foreach (PgDatabase db in PgDatabases)
                {
                    PgsqlDatabase obj = db.ToDatabase(Context);
                    lTmpl.Add(obj);
                    if (db.IsCurrent)
                    {
                        Context.Database = obj;
                    }
                    else if (!obj.IsTemplate)
                    {
                        lOther.Add(obj);
                    }
                }
                lTmpl.Sort(PgsqlDatabase.CompareTemplate);
                lOther.Sort();
                Context.OtherDatabases.Clear();
                Context.OtherDatabases.AddRange(lOther);
                Context.DatabaseTemplates.Clear();
                Context.DatabaseTemplates.AddRange(lTmpl);
            }

            private void LoadFromPgClass()
            {
                foreach (PgClass c in PgClasses)
                {
                    c.ToSchemaObject(Context);
                }
            }
            private void LoadFromPgProc()
            {
                foreach (PgProc p in PgProcs)
                {
                    p.ToStoredProcedureBase(Context);
                }
            }

            private void LoadFromPgConstraint()
            {
                foreach (PgConstraint c in PgConstraints)
                {
                    c.ToConstraint(Context);
                }
                Context.InvalidateConstraints();
            }
            private void LoadFromPgTrigger()
            {
                foreach (PgTrigger t in PgTriggers)
                {
                    t.ToTrigger(Context);
                }
                Context.InvalidateTriggers();
            }
            private void LoadFromPgDescription()
            {
                foreach (PgDescription d in PgDescriptions)
                {
                    d.ToComment(Context);
                }
            }
            //private void LoadFromPgDepend()
            //{
            //    foreach (PgDepend d in PgDepends)
            //    {
            //        if (d.Object == null || d.RefObject == null)
            //        {
            //            continue;
            //        }
            //        d.SetDependency(this);
            //    }
            //}
            private void LoadFromPgType()
            {
                foreach (PgType t in PgTypes)
                {
                    t.ToType(Context);
                }
            }

            //private object _nameToPgClassLock = new object();
            //private void UpdateNameToPgClass()
            //{
            //    if (_nameToPgClass != null)
            //    {
            //        return;
            //    }
            //    lock (_nameToPgClassLock)
            //    {
            //        if (_nameToPgClass != null)
            //        {
            //            return;
            //        }
            //        Dictionary<string, PgClass> dictC = new Dictionary<string, PgClass>();
            //        Dictionary<string, PgAttribute> dictA = new Dictionary<string, PgAttribute>();
            //        foreach (PgClass c in PgClasses)
            //        {
            //            string id = c.GetIdentifier();
            //            dictC[id] = c;
            //            foreach (PgAttribute a in c.Columns)
            //            {
            //                dictA[id + "." + a.attname] = a;
            //            }
            //        }
            //        _nameToPgClass = dictC;
            //        _nameToPgAttribute = dictA;
            //    }
            //}
            //internal PgClass FindPgClassByName(string schema, string name)
            //{
            //    if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(name))
            //    {
            //        return null;
            //    }
            //    UpdateNameToPgClass();
            //    string id = schema + "." + name;
            //    PgClass c;
            //    if (!_nameToPgClass.TryGetValue(id, out c))
            //    {
            //        return null;
            //    }
            //    return c;
            //}
            //internal PgAttribute FindPgAttributeByName(string schema, string table, string column)
            //{
            //    if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(table) || string.IsNullOrEmpty(column))
            //    {
            //        return null;
            //    }
            //    UpdateNameToPgClass();
            //    string id = schema + "." + table + "." + column;
            //    PgAttribute a;
            //    if (!_nameToPgAttribute.TryGetValue(id, out a))
            //    {
            //        return null;
            //    }
            //    return a;
            //}

        }
        private WorkingData _backend;

        public override void LoadSchema(IDbConnection connection, bool clearBeforeLoad)
        {
            if (clearBeforeLoad)
            {
                Clear();
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            LoadUserInfo(conn);
            _backend = new WorkingData(this);
            _backend.FillAll(conn);
            OnSchemaLoaded();
        }
        internal Selectable RefreshSelectable(Selectable selectable, char relkind, NpgsqlConnection connection)
        {
            string sch = selectable.SchemaName;
            string name = selectable.Name;
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                uint? oid = PgClass.GetOid(connection, selectable.SchemaName, selectable.Name, relkind);
                if (!oid.HasValue)
                {
                    selectable.Release();
                    return null;
                }
                _backend.FillSelectableByOid(oid.Value, connection);
            }
            Selectable ret = Objects[sch, name] as Selectable;
            if (ret != selectable)
            {
                selectable.ReplaceTo(ret);
                selectable.Release();
            }
            return ret;
        }
        internal Table RefreshTable(Table table, NpgsqlConnection connection)
        {
            return RefreshSelectable(table, 'r', connection) as Table;
        }
        internal View RefreshView(View view, NpgsqlConnection connection)
        {
            return RefreshSelectable(view, 'v', connection) as View;
        }
        internal ComplexType RefreshComplexType(ComplexType type, NpgsqlConnection connection)
        {
            return RefreshSelectable(type, 'c', connection) as ComplexType;
        }
        internal StoredFunction RefreshStoredFunction(StoredFunction function, NpgsqlConnection connection)
        {
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                List<string> args = new List<string>();
                foreach (Parameter p in function.Parameters)
                {
                    if ((p.Direction & ParameterDirection.Input) != 0)
                    {
                        args.Add(p.DataType);
                    }
                }
                uint? oid = PgProc.GetOid(_backend, connection, function.SchemaName, function.Name, args.ToArray());
                if (!oid.HasValue)
                {
                    function.Release();
                    return null;
                }
                _backend.FillProcByOid(oid.Value, connection);
            }
            string id = function.FullIdentifier;
            StoredFunction ret = Objects[id] as StoredFunction;
            if (ret != function)
            {
                function.ReplaceTo(ret);
                function.Release();
            }
            return ret;
        }
        internal StoredProcedure RefreshStoredProcedure(StoredProcedure procedure, NpgsqlConnection connection)
        {
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                List<string> args = new List<string>();
                foreach (Parameter p in procedure.Parameters)
                {
                    if ((p.Direction & ParameterDirection.Input) != 0)
                    {
                        args.Add(p.DataType);
                    }
                }
                uint? oid = PgProc.GetOid(_backend, connection, procedure.SchemaName, procedure.Name, args.ToArray());
                if (!oid.HasValue)
                {
                    procedure.Release();
                    return null;
                }
                _backend.FillProcByOid(oid.Value, connection);
            }
            string id = procedure.FullIdentifier;
            StoredProcedure ret = Objects[id] as StoredProcedure;
            if (ret != procedure)
            {
                procedure.ReplaceTo(ret);
                procedure.Release();
            }
            return ret;
        }

        //private SchemaObject RefreshDatabase(PgsqlDatabase database, NpgsqlConnection connection)
        //{
        //    if (_backend == null)
        //    {
        //        LoadSchema(connection, false);
        //        return DatabaseTemplates[database.Name];
        //    }
        //    else
        //    {
        //        _backend.FillDatabaseByName(database.Identifier, connection);
        //    }
        //    return Database;
        //}

        internal Sequence RefreshSequence(Sequence sequence, NpgsqlConnection connection)
        {
            string sch = sequence.SchemaName;
            string name = sequence.Name;
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                uint? oid = PgClass.GetOid(connection, sequence.SchemaName, sequence.Name, 'S');
                if (!oid.HasValue)
                {
                    sequence.Release();
                    return null;
                }
                _backend.FillSelectableByOid(oid.Value, connection);
            }
            Sequence ret = Objects[sch, name] as Sequence;
            if (ret != sequence)
            {
                sequence.ReplaceTo(ret);
                sequence.Release();
            }
            return ret;
        }

        public override void RefreshSettings(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                _backend.FillSettings(conn);
            }
        }

        public override void RefreshTablespaces(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                _backend.FillTablespaces(conn);
            }
        }

        public override void RefreshUsers(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (_backend == null)
            {
                LoadSchema(connection, false);
            }
            else
            {
                _backend.FillUsers(conn);
            }
        }

        public override SchemaObject Refresh(SchemaObject obj, IDbConnection connection)
        {
            if (obj == null)
            {
                return null;
            }
            NpgsqlConnection conn = connection as NpgsqlConnection;
            if (obj is Table)
            {
                return RefreshTable((Table)obj, conn);
            }
            if (obj is View)
            {
                return RefreshView((View)obj, conn);
            }
            if (obj is ComplexType)
            {
                return RefreshComplexType((ComplexType)obj, conn);
            }
            if (obj is StoredFunction)
            {
                return RefreshStoredFunction((StoredFunction)obj, conn);
            }
            if (obj is StoredProcedure)
            {
                return RefreshStoredProcedure((StoredProcedure)obj, conn);
            }
            if (obj is PgsqlDatabase)
            {
                return obj;
                //return RefreshDatabase((PgsqlDatabase)obj, conn);
            }
            if (obj is Sequence)
            {
                return obj;
                //RefreshSequence((Sequence)obj, conn);
            }
            throw new NotImplementedException(string.Format("{0} {1} is not supported.", obj.GetType().FullName, obj.FullName));
            //return obj;
        }

        public class SetOfReturnType : IReturnType
        {
            public string DataType { get; set; }

            public string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine)
            {
                return prefix + GetDefName();
            }

            public string GetDefName()
            {
                return "setof " + DataType;
            }

            public SetOfReturnType() { }
            public SetOfReturnType(string dataType)
            {
                DataType = dataType;
            }
        }

        public class TableReturnType : IReturnType
        {
            public struct Column
            {
                public string Name { get; set; }
                public string DataType { get; set; }

                public string GetSQL(Db2SourceContext context)
                {
                    return string.Format("{0} {1}", context.GetEscapedIdentifier(Name, false), DataType);
                }

                public override string ToString()
                {
                    return string.Format("{0} {1}", Name, DataType);
                }

                internal Column(string name, PgType type)
                {
                    Name = name;
                    DataType = type.formatname;
                }
            }
            public Column[] Columns { get; set; }

            private void _Add(StringBuilder buffer, string delimiter, string value, ref int column, int indent, int charPerLine)
            {
                if (!string.IsNullOrEmpty(delimiter))
                {
                    buffer.Append(delimiter);
                    column += delimiter.Length;
                    if (charPerLine < column)
                    {
                        buffer.AppendLine();
                        buffer.Append(GetIndent(indent));
                        column = indent;
                    }
                    else
                    {
                        buffer.Append(' ');
                        column++;
                    }
                }
                buffer.Append(value);
                column += value.Length;
            }

            public string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine)
            {
                StringBuilder buf = new StringBuilder();
                int p = prefix.Length;
                buf.Append(prefix);
                _Add(buf, string.Empty, "table (", ref p, indent, charPerLine);
                string delimiter = string.Empty;
                foreach (Column column in Columns)
                {
                    _Add(buf, delimiter, column.GetSQL(context), ref p, indent, charPerLine);
                    delimiter = ",";
                }
                buf.Append(")");
                return buf.ToString();
            }

            public string GetDefName()
            {
                return "table";
            }

            public TableReturnType() { }
            public TableReturnType(Column[] columns)
            {
                Columns = columns;
            }
        }

    }
}
