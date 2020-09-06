using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
//using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
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

                if (ft == typeof(Int16))
                {
                    f.SetValue(target, reader.GetFieldValue<Int16>(i));
                }
                else if (ft == typeof(Int32))
                {
                    f.SetValue(target, reader.GetInt32(i));
                }
                else if (ft == typeof(UInt32))
                {

                    f.SetValue(target, reader.GetFieldValue<UInt32>(i));
                }
                else if (ft == typeof(Int64))
                {
                    f.SetValue(target, reader.GetInt64(i));
                }
                else if (ft == typeof(UInt64))
                {
                    f.SetValue(target, reader.GetFieldValue<UInt32>(i));
                }
                else if (ft == typeof(bool))
                {
                    f.SetValue(target, reader.GetBoolean(i));
                }
                else if (ft == typeof(char))
                {
                    f.SetValue(target, reader.GetChar(i));
                }
                else if (ft == typeof(string))
                {
                    if (reader.IsDBNull(i))
                    {
                        f.SetValue(target, null);
                    }
                    else
                    {
                        f.SetValue(target, reader.GetString(i));
                    }
                }
                else if (ft.IsArray)
                {
                    Type et = ft.GetElementType();
                    if (reader.IsDBNull(i))
                    {
                        f.SetValue(target, null);
                    }
                    else if (et == typeof(byte))
                    {
                        f.SetValue(target, reader.GetFieldValue<byte[]>(i));
                    }
                    else if (et == typeof(sbyte))
                    {
                        f.SetValue(target, reader.GetFieldValue<sbyte[]>(i));
                    }
                    else if (et == typeof(Int16))
                    {
                        f.SetValue(target, reader.GetFieldValue<Int16[]>(i));
                    }
                    else if (et == typeof(Int32))
                    {
                        f.SetValue(target, reader.GetFieldValue<Int32[]>(i));
                    }
                    else if (et == typeof(UInt32))
                    {
                        f.SetValue(target, reader.GetFieldValue<UInt32[]>(i));
                    }
                    else if (et == typeof(Int64))
                    {
                        f.SetValue(target, reader.GetFieldValue<Int64[]>(i));
                    }
                    else if (et == typeof(UInt64))
                    {
                        f.SetValue(target, reader.GetFieldValue<UInt64[]>(i));
                    }
                    else if (et == typeof(bool))
                    {
                        f.SetValue(target, reader.GetFieldValue<bool[]>(i));
                    }
                    else if (et == typeof(char))
                    {
                        f.SetValue(target, reader.GetFieldValue<char[]>(i));
                    }
                    else if (et == typeof(string))
                    {
                        f.SetValue(target, reader.GetFieldValue<string[]>(i));
                    }
                }
                else if (ft.IsSubclassOf(typeof(IList)))
                {
                    Type gt = ft.GetGenericTypeDefinition();
                    if (gt == typeof(Int32))
                    {
                        f.SetValue(target, reader.GetFieldValue<Int32[]>(i));
                    }
                    else if (gt == typeof(Int64))
                    {
                        f.SetValue(target, reader.GetFieldValue<Int64[]>(i));
                    }
                    else if (gt == typeof(bool))
                    {
                        f.SetValue(target, reader.GetFieldValue<bool[]>(i));
                    }
                    else if (gt == typeof(char))
                    {
                        f.SetValue(target, reader.GetFieldValue<char[]>(i));
                    }
                    else if (gt == typeof(string))
                    {
                        f.SetValue(target, reader.GetFieldValue<string[]>(i));
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

        private class PgObjectCollection<T>: IReadOnlyList<T> where T : PgObject, new()
        {
            private List<T> _items = new List<T>();
            private Dictionary<uint, T> _oidToItem = null;
            private object _dictionaryLock = new object();

            private void InvalidateDictionary()
            {
                _oidToItem = null;
            }
            private void RequireDictionary()
            {
                if (_oidToItem != null)
                {
                    return;
                }
                lock (_dictionaryLock)
                {
                    if (_oidToItem != null)
                    {
                        return;
                    }
                    Dictionary<uint, T> dictOid = new Dictionary<uint, T>();
                    foreach (T obj in _items)
                    {
                        dictOid[obj.oid] = obj;
                    }
                    _oidToItem = dictOid;
                }
            }
            public void Fill(string sql, NpgsqlConnection connection, Dictionary<uint, PgObject> dict, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                InvalidateDictionary();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.Fill", cmd);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                        while (reader.Read())
                        {
                            T obj = new T();
                            ReadObject(obj, reader, mapper);
                            dict[obj.oid] = obj;
                            _items.Add(obj);
                        }
                    }
                }
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void Join(string sql, NpgsqlConnection connection)
            {
                InvalidateDictionary();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.Join", cmd);
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
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void JoinToDict<T2>(string sql, FieldInfo dictField, string fieldname, NpgsqlConnection connection)
            {
                InvalidateDictionary();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgObjectCollection<T>.JoinToDict", cmd);
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
                            Dictionary<int, T2> dict = (Dictionary<int,T2>)dictField.GetValue(obj);
                            dict[i] = reader.GetFieldValue<T2>(fieldPos);
                        }
                    }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.BeginFillReference(working);
                }
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
                RequireDictionary();
                T ret;
                if (!_oidToItem.TryGetValue(oid, out ret))
                {
                    return null;
                }
                return ret;
            }

            public PgObjectCollection(string sql, NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Fill(sql, connection, dict, false);
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
        private class PgOidSubidCollection<T> : IReadOnlyList<T> where T: PgOidSubid, new()
        {
            private List<T> _items = new List<T>();
            private Dictionary<ulong, T> _oidNumToItem = null;
            private object _dictionaryLock = new object();

            private void InvalidateDictionary()
            {
                _oidNumToItem = null;
            }
            private void RequireDictionary()
            {
                if (_oidNumToItem != null)
                {
                    return;
                }
                lock (_dictionaryLock)
                {
                    if (_oidNumToItem != null)
                    {
                        return;
                    }
                    Dictionary<ulong, T> dictOid = new Dictionary<ulong, T>();
                    foreach (T obj in _items)
                    {
                        ulong v = (((ulong)obj.Oid) << 32) | (uint)obj.Subid;
                        dictOid[v] = obj;
                    }
                    _oidNumToItem = dictOid;
                }
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL クエリのセキュリティ脆弱性を確認")]
            public void Fill(string sql, NpgsqlConnection connection, bool clearBeforeFill)
            {
                if (clearBeforeFill)
                {
                    _items.Clear();
                }
                InvalidateDictionary();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
                {
                    LogDbCommand("PgOidSubidCollection<T>.Fill", cmd);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        FieldInfo[] mapper = CreateMapper(reader, typeof(T));
                        while (reader.Read())
                        {
                            T obj = new T();
                            ReadObject(obj, reader, mapper);
                            _items.Add(obj);
                        }
                    }
                }
            }
            public void BeginFillReference(WorkingData working)
            {
                foreach (T obj in _items)
                {
                    obj.BeginFillReference(working);
                }
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

            public T FindByOidNum(uint oid, int num)
            {
                RequireDictionary();
                T ret;
                ulong v = ((ulong)oid) << 32 | (uint)num;
                if (!_oidNumToItem.TryGetValue(v, out ret))
                {
                    return null;
                }
                return ret;
            }

            public PgOidSubidCollection(string sql, NpgsqlConnection connection)
            {
                Fill(sql, connection, false);
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

        //private class PgDependCollection : IReadOnlyList<PgDepend>
        //{
        //    private List<PgDepend> _items = new List<PgDepend>();
        //    private Dictionary<uint, List<PgDepend>> _oidToItem = null;
        //    private Dictionary<uint, List<PgDepend>> _refOidToItem = null;
        //    private object _dictionaryLock = new object();

        //    private void InvalidateDictionary()
        //    {
        //        _oidToItem = null;
        //        _refOidToItem = null;
        //    }
        //    private void RequireDictionary()
        //    {
        //        if (_oidToItem != null)
        //        {
        //            return;
        //        }
        //        lock (_dictionaryLock)
        //        {
        //            if (_oidToItem != null)
        //            {
        //                return;
        //            }
        //            Dictionary<uint, List<PgDepend>> dictOid = new Dictionary<uint, List<PgDepend>>();
        //            Dictionary<uint, List<PgDepend>> dictRefOid = new Dictionary<uint, List<PgDepend>>();
        //            foreach (PgDepend obj in _items)
        //            {
        //                List<PgDepend> l;
        //                if (!dictOid.TryGetValue(obj.objid, out l))
        //                {
        //                    l = new List<PgDepend>();
        //                    dictOid.Add(obj.objid, l);
        //                }
        //                l.Add(obj);
        //                if (!dictRefOid.TryGetValue(obj.refobjid, out l))
        //                {
        //                    l = new List<PgDepend>();
        //                    dictRefOid.Add(obj.refobjid, l);
        //                }
        //                l.Add(obj);
        //            }
        //            _oidToItem = dictOid;
        //            _refOidToItem = dictRefOid;
        //        }
        //    }
        //    public void Fill(string sql, NpgsqlConnection connection, bool clearBeforeFill)
        //    {
        //        if (clearBeforeFill)
        //        {
        //            _items.Clear();
        //        }
        //        InvalidateDictionary();
        //        using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
        //        {
        //            using (NpgsqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                FieldInfo[] mapper = CreateMapper(reader, typeof(PgDepend));
        //                while (reader.Read())
        //                {
        //                    PgDepend obj = new PgDepend();
        //                    ReadObject(obj, reader, mapper);
        //                    _items.Add(obj);
        //                }
        //            }
        //        }
        //    }
        //    public void BeginFillReference(WorkingData working)
        //    {
        //        foreach (PgDepend obj in _items)
        //        {
        //            obj.BeginFillReference(working);
        //        }
        //    }
        //    public void EndFillReference(WorkingData working)
        //    {
        //        foreach (PgDepend obj in _items)
        //        {
        //            obj.EndFillReference(working);
        //        }
        //    }
        //    public void FillReference(WorkingData working)
        //    {
        //        foreach (PgDepend obj in _items)
        //        {
        //            obj.FillReference(working);
        //        }
        //    }

        //    public List<PgDepend> FindByOid(uint oid)
        //    {
        //        RequireDictionary();
        //        List<PgDepend> ret;
        //        if (!_oidToItem.TryGetValue(oid, out ret))
        //        {
        //            ret = new List<PgDepend>();
        //            _oidToItem.Add(oid, ret);
        //        }
        //        return ret;
        //    }
        //    public List<PgDepend> FindByOidNum(uint oid, int num)
        //    {
        //        RequireDictionary();
        //        List<PgDepend> ret = new List<PgDepend>();
        //        List<PgDepend> l;
        //        if (!_oidToItem.TryGetValue(oid, out l))
        //        {
        //            return ret;
        //        }
        //        foreach (PgDepend obj in l)
        //        {
        //            if (obj.objsubid == num)
        //            {
        //                ret.Add(obj);
        //            }
        //        }
        //        return ret;
        //    }

        //    public List<PgDepend> FindByRefOid(uint oid)
        //    {
        //        RequireDictionary();
        //        List<PgDepend> ret;
        //        if (!_refOidToItem.TryGetValue(oid, out ret))
        //        {
        //            ret = new List<PgDepend>();
        //            _refOidToItem.Add(oid, ret);
        //        }
        //        return ret;
        //    }
        //    public List<PgDepend> FindByRefOidNum(uint oid, int num)
        //    {
        //        RequireDictionary();
        //        List<PgDepend> ret = new List<PgDepend>();
        //        List<PgDepend> l;
        //        if (!_refOidToItem.TryGetValue(oid, out l))
        //        {
        //            return ret;
        //        }
        //        foreach (PgDepend obj in l)
        //        {
        //            if (obj.refobjsubid == num)
        //            {
        //                ret.Add(obj);
        //            }
        //        }
        //        return ret;
        //    }

        //    public PgDependCollection(string sql, NpgsqlConnection connection)
        //    {
        //        Fill(sql, connection, false);
        //    }
        //    public PgDependCollection() { }

        //    #region IReadOnlyList の実装
        //    public int Count
        //    {
        //        get
        //        {
        //            return _items.Count;
        //        }
        //    }

        //    public IEnumerator<PgDepend> GetEnumerator()
        //    {
        //        return _items.GetEnumerator();
        //    }

        //    IEnumerator IEnumerable.GetEnumerator()
        //    {
        //        return ((IEnumerable)_items).GetEnumerator();
        //    }

        //    public PgDepend this[int index] { get { return _items[index]; } }
        //    #endregion
        //}

        private abstract class PgObject
        {
            //public static PgObjectCollection<PgObject> DefaultStore;
#pragma warning disable 0649
            public uint oid;
#pragma warning restore 0649
            //public abstract string Name { get; }
            public SchemaObject Generated;
            public virtual void BeginFillReference(WorkingData working) { }
            public virtual void EndFillReference(WorkingData working) { }
            public abstract void FillReference(WorkingData working);
            public PgObject() { }
        }
        private class PgNamespace: PgObject
        {
#pragma warning disable 0649
            public string nspname;
            public uint nspowner;

            //public override string Name { get { return nspname; } }
            public PgNamespace() : base() { }
            public static PgObjectCollection<PgNamespace> Namespaces = null;
            public const string SQL = "select oid, nspname, nspowner from pg_catalog.pg_namespace";
            public static PgObjectCollection<PgNamespace> Load(NpgsqlConnection connection, PgObjectCollection<PgNamespace> store, Dictionary<uint, PgObject> dict)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgNamespace>(SQL, connection, dict);
                }
                else
                {
                    store.Fill(SQL, connection, dict, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgNamespace> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Namespaces = new PgObjectCollection<PgNamespace>(SQL, connection, dict);
                return Namespaces;
            }
            public override void FillReference(WorkingData working)
            {
            }
        }
        private class PgClass: PgObject
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
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgClass IndexTable;
            public PgTablespace TableSpace;
            public bool IsImplicit;
            public string[] IndexColumns;
            public List<PgAttribute> Columns = new List<PgAttribute>();
            public List<PgConstraint> Constraints = new List<PgConstraint>();
            public List<PgClass> Indexes = new List<PgClass>();

            //public override string Name { get { return string.Format("{0}_{1}:{2}", relnamespace, relname, relkind); } }
            public PgClass() : base() { }
            public static PgObjectCollection<PgClass> Classes = null;
            public static PgObjectCollection<PgClass> Load(NpgsqlConnection connection, PgObjectCollection<PgClass> store, Dictionary<uint, PgObject> dict)
            {
                PgObjectCollection<PgClass> l;
                if (store == null)
                {
                    l = new PgObjectCollection<PgClass>(DataSet.Properties.Resources.PgClass_SQL, connection, dict);
                }
                else
                {
                    l = store;
                    l.Fill(DataSet.Properties.Resources.PgClass_SQL, connection, dict, false);
                }
                l.Join(DataSet.Properties.Resources.PgClass_VIEWDEFSQL, connection);
                l.Join(DataSet.Properties.Resources.PgClass_INDEXSQL, connection);
                l.Join(DataSet.Properties.Resources.PgClass_SEQUENCESQL, connection);
                return l;
            }
            public static PgObjectCollection<PgClass> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Classes = new PgObjectCollection<PgClass>(DataSet.Properties.Resources.PgClass_SQL, connection, dict);
                return Classes;
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
                                PgAttribute a = working.PgAttributes.FindByOidNum(indrelid, i);
                                Columns.Add(a);
                            }
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
                if (relkind != 'v')
                {
                    return null;
                }
                View v = new View(context, ownername, Schema?.nspname, relname, viewdef, true);
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (!c.IsHidden)
                    {
                        c.Index = i++;
                    }
                }
                Generated = v;
                return v;
            }
            public Table ToTable(NpgsqlDataSet context)
            {
                if (relkind != 'r')
                {
                    return null;
                }
                Table t = new Table(context, ownername, Schema?.nspname, relname);
                t.TablespaceName = TableSpace?.spcname;
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (!c.IsHidden)
                    {
                        c.Index = i++;
                    }
                }
                Generated = t;
                return t;
            }
            public ComplexType ToComplexType(NpgsqlDataSet context)
            {
                if (relkind != 'c')
                {
                    return null;
                }
                ComplexType t = new ComplexType(context, ownername, Schema?.nspname, relname);
                int i = 1;
                foreach (PgAttribute a in Columns)
                {
                    Column c = a.ToColumn(context);
                    if (!c.IsHidden)
                    {
                        c.Index = i++;
                    }
                }
                Generated = t;
                return t;
            }

            public Index ToIndex(NpgsqlDataSet context)
            {
                if (relkind != 'i')
                {
                    return null;
                }
                Index idx = new Index(context, ownername, Schema?.nspname, relname, IndexTable?.Schema?.nspname, IndexTable?.relname, IndexColumns, indexdef);
                idx.IsUnique = indisunique;
                idx.IsImplicit = IsImplicit;
                idx.IndexType = indextype;
                idx.SqlDef = NormalizeSql(indexdef);
                Generated = idx;
                return idx;
            }
            public Sequence ToSequence(NpgsqlDataSet context)
            {
                if (relkind != 'S')
                {
                    return null;
                }
                Sequence seq = new Sequence(context, ownername, Schema?.nspname, relname);
                seq.StartValue = start_value.ToString();
                if (0 < increment)
                {
                    seq.MinValue = (minimum_value != 1) ? minimum_value.ToString() : null;
                    seq.MaxValue = (maximum_value != long.MaxValue) ? maximum_value.ToString() : null;
                }
                else
                {
                    seq.MinValue = (minimum_value != long.MinValue) ? minimum_value.ToString() : null;
                    seq.MaxValue = (maximum_value != -1) ? maximum_value.ToString() : null;
                }
                seq.Increment = (increment != 1) ? increment.ToString() : null;
                seq.IsCycled = cycle_option;
                seq.OwnedSchema = owned_schema;
                seq.OwnedTable = owned_table;
                seq.OwnedColumn = owned_field;
                return seq;
            }

            public SchemaObject ToSchemaObject(NpgsqlDataSet context)
            {
                switch (relkind)
                {
                    case 'r':
                        return ToTable(context);
                    case 'v':
                        return ToView(context);
                    case 'i':
                        return ToIndex(context);
                    case 'S': // なぜかsequenceだけ大文字
                        return ToSequence(context);
                    case 'c': //複合型
                        return ToComplexType(context);
                    case 't': //TOASTテーブル
                    case 'f': //外部テーブル
                        return null;
                }
                return null;
            }

            public override string ToString()
            {
                return relname;
            }
        }

        private class PgType: PgObject
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
            public static PgObjectCollection<PgType> Load(NpgsqlConnection connection, PgObjectCollection<PgType> store, Dictionary<uint, PgObject> dict)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgType>(DataSet.Properties.Resources.PgType_SQL, connection, dict);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgType_SQL, connection, dict, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgType> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Types = new PgObjectCollection<PgType>(DataSet.Properties.Resources.PgType_SQL, connection, dict);
                return Types;
            }
            public override void BeginFillReference(WorkingData working)
            {
            }
            public override void EndFillReference(WorkingData working)
            {
                Relation = working.PgClasses.FindByOid(typrelid);
                BaseType = working.PgTypes.FindByOid(typbasetype);
                ElementType = working.PgTypes.FindByOid(typelem);
                IsArray = (ElementType != null) && (typlen == -1);
            }
            public override void FillReference(WorkingData working)
            {
                Schema = working.PgNamespaces.FindByOid(typnamespace);
                //ArrayType = working.PgTypes.FindByOid(typarray);
            }

            public BasicType ToBasicType(NpgsqlDataSet context)
            {
                if (typtype != 'b' || typcategory == 'A')
                {
                    return null;
                }
                BasicType ret = new BasicType(context, ownername, Schema?.nspname, typname);
                ret.InputFunction = typinput;
                ret.OutputFunction = typoutput;
                ret.ReceiveFunction = typreceive;
                ret.SendFunction = typsend;
                ret.TypmodInFunction = typmodin;
                ret.TypmodOutFunction = typmodout;
                ret.AnalyzeFunction = typanalyze;
                //ret.InternalLengthFunction = ;
                ret.PassedbyValue = typbyval;
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
            public SchemaObject ToType(NpgsqlDataSet context)
            {
                switch (typtype)
                {
                    case 'b':   // 基本型
                        return ToBasicType(context);
                    case 'c':   // 複合型
                        return null;
                    case 'd':   // 派生型
                        return null;
                    case 'e':   // 列挙型
                        return null;
                    case 'p':   // 疑似型
                        return null;
                    case 'r':   // 範囲型
                        return null;
                    default:
                        return null;
                }
            }

            public override string ToString()
            {
                return typname;
            }
        }

        private class PgConstraint: PgObject
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
            public static PgObjectCollection<PgConstraint> Constraints = null;

            public static readonly Dictionary<char, ForeignKeyRule> CharToForeignKeyRule = new Dictionary<char, ForeignKeyRule>
            {
                { 'c', ForeignKeyRule.Cascade },
                { 'n', ForeignKeyRule.SetNull },
                { 'd', ForeignKeyRule.SetDefault },
                { 'r', ForeignKeyRule.Restrict },
                { 'a', ForeignKeyRule.NoAction },
            };
        public static PgObjectCollection<PgConstraint> Load(NpgsqlConnection connection, PgObjectCollection<PgConstraint> store, Dictionary<uint, PgObject> dict)
            {
                PgObjectCollection<PgConstraint> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgConstraint>(DataSet.Properties.Resources.PgConstraint_SQL, connection, dict);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgConstraint_SQL, connection, dict, false);
                }
                l.Join(DataSet.Properties.Resources.PgConstraint_CHECKSQL, connection);
                return l;
            }
            public static PgObjectCollection<PgConstraint> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Constraints = new PgObjectCollection<PgConstraint>(DataSet.Properties.Resources.PgConstraint_SQL, connection, dict);
                return Constraints;
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
                    bool found = false;
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
                        found = true;
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
                //Constraint c = null;
                //c = new Constraint(context, RefObject?.ownername, Schema?.nspname, conname, RefObject?.Schema?.nspname, RefObject?.relname, false);
                switch (contype)
                {
                    case 'c': // 検査制約
                        CheckConstraint cc = new CheckConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname, checkdef, false);
                        Generated = cc;
                        return cc;
                    case 'f': // 外部キー制約
                        ForeignKeyConstraint rc = new ForeignKeyConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname,
                            RefConstraint?.Schema?.nspname, RefConstraint?.conname, CharToForeignKeyRule[confupdtype], CharToForeignKeyRule[confdeltype], false, condeferrable, condeferred);
                        rc.Columns = GetNameArray(Keys);
                        rc.RefColumns = GetNameArray(RefKeys);
                        Generated = rc;
                        return rc;
                    case 'p': // プライマリキー制約
                    case 'u': // 一意性制約
                        if (Keys == null)
                        {
                            return null;
                        }
                        KeyConstraint kc = new KeyConstraint(context, Object?.ownername, Schema?.nspname, conname, Object?.Schema?.nspname, Object?.relname, contype == 'p', false, condeferrable, condeferred);
                        kc.Columns = new string[Keys.Length];
                        for (int i = 0; i < Keys.Length; i++)
                        {
                            kc.Columns[i] = Keys[i].attname;
                        }
                        Generated = kc;
                        return kc;
                    case 't': // 制約トリガ
                    case 'x': // 排他制約
                        return null;
                }
                return null;
            }

            public override string ToString()
            {
                return conname;
            }
        }
        private abstract class PgOidSubid: IComparable
        {
            public abstract uint Oid { get; }
            public abstract int Subid { get; }
            public NamedObject Generated;
            public virtual void FillReference(WorkingData working) { }
            public virtual void BeginFillReference(WorkingData working) { }
            public virtual void EndFillReference(WorkingData working) { }

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
        private class PgAttribute: PgOidSubid
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
            //public int atttypmod;
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
            //public override string Name { get { return string.Format("{0}_{1}", attrelid, attname); } }
            public static PgOidSubidCollection<PgAttribute> Attributes;

            public static PgOidSubidCollection<PgAttribute> Load(NpgsqlConnection connection, PgOidSubidCollection<PgAttribute> store)
            {
                if (store == null)
                {
                    return new PgOidSubidCollection<PgAttribute>(DataSet.Properties.Resources.PgAttribute_SQL, connection);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgAttribute_SQL, connection, false);
                    return store;
                }
            }
            public static PgOidSubidCollection<PgAttribute> Load(NpgsqlConnection connection)
            {
                Attributes = new PgOidSubidCollection<PgAttribute>(DataSet.Properties.Resources.PgAttribute_SQL, connection);
                return Attributes;
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
                Column c = new Column(context, Owner?.Schema?.nspname);
                c.TableName = Owner?.relname;
                c.Name = attname;
                c.DataType = DefTypeName;
                c.BaseType = BaseTypeName;
                c.DefaultValue = defaultexpr;
                c.NotNull = attnotnull;
                Type t = null;
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
                    c.IsHidden = true;
                    c.Index = attnum;
                }
                Generated = c;
                return c;
            }
            public override string ToString()
            {
                return attname;
            }
        }

        private class PgDescription: PgObject
        {
#pragma warning disable 0649
            public uint objoid;
            public uint classoid;
            public int objsubid;
            public string description;
#pragma warning restore 0649
            public PgClass TargetClass;
            public PgAttribute TargetAttribute;
            public static PgObjectCollection<PgDescription> Descriptions;
            private const string SQL = "select objoid, classoid, objsubid, description from pg_catalog.pg_description";
            public static PgObjectCollection<PgDescription> Load(NpgsqlConnection connection, PgObjectCollection<PgDescription> store, Dictionary<uint, PgObject> dict)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgDescription>(SQL, connection, dict);
                }
                else
                {
                    store.Fill(SQL, connection, dict, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgDescription> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Descriptions = new PgObjectCollection<PgDescription>(SQL, connection, dict);
                return Descriptions;
            }

            public override void FillReference(WorkingData working)
            {
                TargetClass = working.PgClasses.FindByOid(objoid);
                TargetAttribute = null;
                if (objsubid != 0)
                {
                    TargetAttribute = working.PgAttributes.FindByOidNum(objoid, objsubid);
                }
            }

            public Comment ToComment(NpgsqlDataSet context)
            {
                if (TargetClass == null)
                {
                    return null;
                }
                Comment c = null;
                if (TargetAttribute != null)
                {
                    c = new ColumnComment(context, TargetClass?.Schema?.nspname, TargetClass?.relname, TargetAttribute.attname, description, true);
                }
                else
                {
                    c = new Comment(context, TargetClass?.Schema?.nspname, TargetClass?.relname, description, true);
                }
                c.Link();
                //Generated = c;
                return c;
            }
        }
//        private class PgDepend
//        {
//#pragma warning disable 0649
//            public uint classid;
//            public uint objid;
//            public int objsubid;
//            public uint refclassid;
//            public uint refobjid;
//            public int refobjsubid;
//            public char deptype;
//#pragma warning restore 0649
//            public PgClass Object;
//            public PgAttribute Attribute;
//            public PgClass RefObject;
//            public PgAttribute RefAttribute;
//            public static PgDependCollection Depends;
//            private const string SQL = "select classid, objid, objsubid, refclassid, refobjid, refobjsubid, deptype from pg_catalog.pg_depend";
//            public static PgDependCollection Load(NpgsqlConnection connection, PgDependCollection store)
//            {
//                if (store == null)
//                {
//                    return new PgDependCollection(SQL, connection);
//                }
//                else
//                {
//                    store.Fill(SQL, connection, false);
//                    return store;
//                }
//            }
//            public static PgDependCollection Load(NpgsqlConnection connection)
//            {
//                Depends = new PgDependCollection(SQL, connection);
//                return Depends;
//            }

//            public void FillReference(WorkingData working)
//            {
//                Object = null;
//                Attribute = null;
//                if (objsubid == 0)
//                {
//                    Object = working.PgClasses.FindByOid(objid);
//                }
//                else
//                {
//                    Attribute = working.PgAttributes.FindByOidNum(objid, objsubid);
//                }
//                RefObject = null;
//                RefAttribute = null;
//                if (objsubid == 0)
//                {
//                    RefObject = working.PgClasses.FindByOid(refobjid);
//                }
//                else
//                {
//                    RefAttribute = working.PgAttributes.FindByOidNum(refobjid, refobjsubid);
//                }
//            }
//            public void BeginFillReference(WorkingData working) { }
//            public void EndFillReference(WorkingData working) { }
//            public Dependency ToDependency(WorkingData working)
//            {
//                SchemaObject obj = (Object.Generated as SchemaObject);
//                SchemaObject robj = (RefObject.Generated as SchemaObject);
//                if (obj == null || robj == null)
//                {
//                    return null;
//                }
//                return new Dependency(obj, robj);
//                //if (obj is Index)
//                //{
//                //    return;
//                //}
//                //obj.ReferTo.Add(robj);
//                //robj.ReferFrom.Add(obj);
//            }
//        }
        private class PgTrigger: PgObject
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
            public static PgObjectCollection<PgTrigger> Triggers;
            public static PgObjectCollection<PgTrigger> Load(NpgsqlConnection connection, PgObjectCollection<PgTrigger> store, Dictionary<uint, PgObject> dict)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgTrigger>(DataSet.Properties.Resources.PgTrigger_SQL, connection, dict);
                }
                else
                {
                    store.Fill(DataSet.Properties.Resources.PgTrigger_SQL, connection, dict, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgTrigger> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Triggers = new PgObjectCollection<PgTrigger>(DataSet.Properties.Resources.PgProc_SQL, connection, dict);
                return Triggers;
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
            private static string[] TimingToText = new string[] { string.Empty, "before", "after", "instead of" };
            public Trigger ToTrigger(NpgsqlDataSet context)
            {
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
                    def = "execute procedure " + context.GetEscapedIdentifier(Procedure.Schema?.nspname, Procedure.proname, Target.Schema?.nspname) + "()";
                }
                Trigger t = new Trigger(context, Target.ownername, Target.Schema?.nspname, tgname, Target.Schema?.nspname, Target.relname, def, true);
                t.ProcedureSchema = Procedure.Schema?.nspname;
                t.ProcedureName = Procedure.GetInternalName();
                t.Timing = ((tgtype & 2) != 0) ? TriggerTiming.Before : ((tgtype & 64) != 0) ? TriggerTiming.InsteadOf : TriggerTiming.After;
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
                TokenizedSQL sql = new TokenizedSQL(triggerdef);
                StringBuilder buf = new StringBuilder();
                bool inWhen = false;
                foreach (Token tk in sql.Tokens)
                {
                    if (tk.ID == TokenID.Identifier) {
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
                        t.UpdateEventColumns.Add(context.GetEscapedIdentifier(a.attname));
                    }
                }
                Generated = t;
                return t;
            }
        }
        private class PgTablespace: PgObject
        {
#pragma warning disable 0649
            public string spcname;
            public uint spcowner;
#pragma warning restore 0649
            public static PgObjectCollection<PgTablespace> Tablespaces;
            private const string SQL = "select oid, spcname, spcowner from pg_catalog.pg_tablespace";
            public static PgObjectCollection<PgTablespace> Load(NpgsqlConnection connection, PgObjectCollection<PgTablespace> store, Dictionary<uint, PgObject> dict)
            {
                if (store == null)
                {
                    return new PgObjectCollection<PgTablespace>(SQL, connection, dict);
                }
                else
                {
                    store.Fill(SQL, connection, dict, false);
                    return store;
                }
            }
            public static PgObjectCollection<PgTablespace> Load(NpgsqlConnection connection, Dictionary<uint, PgObject> dict)
            {
                Tablespaces = new PgObjectCollection<PgTablespace>(SQL, connection, dict);
                return Tablespaces;
            }

            public override void FillReference(WorkingData working)
            {
            }
        }

        private class PgProc: PgObject
        {
#pragma warning disable 0649
            public string proname;
            public uint pronamespace;
            //public uint proowner;
            //public uint prolang;
            //public float procost;
            //public float prorows;
            //public uint provariadic;
            public string protransform;
            //public bool proisagg;
            //public bool proiswindow;
            //public bool prosecdef;
            //public bool proleakproof;
            //public bool proisstrict;
            //public bool proretset;
            //public char provolatile;
            //public short pronargs;
            //public short pronargdefaults;
            public uint prorettype;
            public uint[] proargtypes;
            public uint[] proallargtypes;
            public char[] proargmodes;
            public string[] proargnames;
            ////public pg_node_tree proargdefaults;
            //public int[] protrftypes;
            public string prosrc;
            //public string probin;
            //public string[] proconfig;
            ////public aclitem[] proacl;
            public string ownername;
            public string lanname;
#pragma warning restore 0649
            public PgNamespace Schema;
            public PgType ReturnType;
            public PgType[] ArgTypes;
            public PgType[] AllArgTypes;
            public Dictionary<int, string> ArgDefaults = new Dictionary<int, string>();

            internal string GetInternalName()
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(proname);
                buf.Append('(');
                PgType[] args = AllArgTypes != null ? AllArgTypes : ArgTypes;
                if (args != null)
                {
                    bool needComma = false;
                    foreach (PgType t in args)
                    {
                        if (needComma)
                        {
                            buf.Append(',');
                        }
                        buf.Append(t.formatname);
                        needComma = true;
                    }
                }
                buf.Append(')');
                return buf.ToString();
            }
            public static PgObjectCollection<PgProc> Load(NpgsqlConnection connection, PgObjectCollection<PgProc> store, Dictionary<uint, PgObject> dict)
            {
                PgObjectCollection<PgProc> l = store;
                if (l == null)
                {
                    l = new PgObjectCollection<PgProc>(DataSet.Properties.Resources.PgProc_SQL, connection, dict);
                }
                else
                {
                    l.Fill(DataSet.Properties.Resources.PgProc_SQL, connection, dict, false);
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
                    //case 'v':
                    //    return ParameterDirection.VarDic;
                    //case 't':
                    //    retuen ParameterDirection.Table;
                    default:
                        return ParameterDirection.Input;
                }
                //return ParameterDirection.Input;
            }
            public StoredFunction ToStoredFunction(NpgsqlDataSet context)
            {
                StoredFunction fn = new StoredFunction(context, ownername, Schema?.nspname, proname, GetInternalName(), prosrc, true);
                fn.DataType = ReturnType?.formatname;
                fn.BaseType = ReturnType?.BaseType?.formatname;
                if (fn.BaseType == null)
                {
                    fn.BaseType = fn.DataType;
                }
                fn.Language = lanname;
                PgType[] args = AllArgTypes != null ? AllArgTypes : ArgTypes;
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        Parameter p = new Parameter(fn);
                        p.Name = proargnames != null ? proargnames[i] : null;
                        p.Direction = GetParameterDirection(i);
                        p.DataType = args[i].formatname;
                        p.BaseType = args[i].BaseType?.typname;
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
                        if (ArgDefaults.TryGetValue(i + 1, out v))
                        {
                            p.DefaultValue = v;
                        }
                    }
                }
                Generated = fn;
                return fn;
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
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentSchema = reader.GetValue(0).ToString();
                        }
                    }
                }
            }
            finally
            {
                EnableChangeLog();
            }
        }

        private class WorkingData
        {
            public NpgsqlDataSet Context;
            public Dictionary<uint, PgObject> OidToObject = new Dictionary<uint, PgObject>();
            public PgObjectCollection<PgClass> PgClasses;
            public PgObjectCollection<PgConstraint> PgConstraints;
            public PgObjectCollection<PgNamespace> PgNamespaces;
            public PgOidSubidCollection<PgAttribute> PgAttributes;
            public PgObjectCollection<PgDescription> PgDescriptions;
            //public PgDependCollection PgDepends;
            public PgObjectCollection<PgTrigger> PgTriggers;
            public PgObjectCollection<PgTablespace> PgTablespaces;
            public PgObjectCollection<PgType> PgTypes;
            public PgObjectCollection<PgProc> PgProcs;
            public WorkingData(NpgsqlDataSet context, NpgsqlConnection connection)
            {
                Context = context;
                PgNamespaces = PgNamespace.Load(connection, null, OidToObject);
                PgTablespaces = PgTablespace.Load(connection, null, OidToObject);
                PgClasses = PgClass.Load(connection, null, OidToObject);
                PgTypes = PgType.Load(connection, null, OidToObject);
                PgAttributes = PgAttribute.Load(connection, null);
                PgProcs = PgProc.Load(connection, null, OidToObject);
                PgConstraints = PgConstraint.Load(connection, null, OidToObject);
                PgTriggers = PgTrigger.Load(connection, null, OidToObject);
                PgDescriptions = PgDescription.Load(connection, null, OidToObject);
                //PgDepends = PgDepend.Load(connection, null);

                PgNamespaces.BeginFillReference(this);
                PgTablespaces.BeginFillReference(this);
                PgClasses.BeginFillReference(this);
                PgTypes.BeginFillReference(this);
                PgAttributes.BeginFillReference(this);
                PgProcs.BeginFillReference(this);
                PgConstraints.BeginFillReference(this);
                PgTriggers.BeginFillReference(this);
                PgDescriptions.BeginFillReference(this);
                //PgDepends.BeginFillReference(this);

                PgNamespaces.FillReference(this);
                PgTablespaces.FillReference(this);
                PgClasses.FillReference(this);
                PgTypes.FillReference(this);
                PgAttributes.FillReference(this);
                PgProcs.FillReference(this);
                PgConstraints.FillReference(this);
                PgTriggers.FillReference(this);
                PgDescriptions.FillReference(this);
                //PgDepends.FillReference(this);

                PgNamespaces.EndFillReference(this);
                PgTablespaces.EndFillReference(this);
                PgClasses.EndFillReference(this);
                PgTypes.EndFillReference(this);
                PgAttributes.EndFillReference(this);
                PgProcs.EndFillReference(this);
                PgConstraints.EndFillReference(this);
                PgTriggers.EndFillReference(this);
                PgDescriptions.EndFillReference(this);
                //PgDepends.EndFillReference(this);

                LoadFromPgNamespaces();
                LoadFromPgClass();
                LoadFromPgType();
                LoadFromPgProc();
                LoadFromPgConstraint();
                LoadFromPgTrigger();
                LoadFromPgDescription();
                //LoadFromPgDepend();
                foreach (Schema s in Context.Schemas)
                {
                    foreach (Schema.CollectionIndex idx in Enum.GetValues(typeof(Schema.CollectionIndex)))
                    {
                        s.GetCollection(idx).Sort();
                    }
                }
            }
            private void LoadFromPgNamespaces()
            {
                foreach (PgNamespace ns in PgNamespaces)
                {
                    if (!Context.IsHiddenSchema(ns.nspname))
                    {
                        Context.RequireSchema(ns.nspname);
                    }
                }
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
                    p.ToStoredFunction(Context);
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
        }

        public override void LoadSchema(IDbConnection connection)
        {
            //Clear();
            NpgsqlConnection conn = connection as NpgsqlConnection;
            LoadUserInfo(conn);
            WorkingData working = new WorkingData(this, conn);
        }
    }
}
