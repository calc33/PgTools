using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class ExportTable
    {
        private static string[] FromCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                return new string[0];
            }
            string[] v = csv.Split(',');
            return v;
        }
        private struct ValueInfo
        {
            public string FieldName;
            public string Value;
        }
        private struct TableInfo
        {
            public uint Oid;
            public string Schema;
            public string TableName;
            public string[] Fields;
            public string[] Formats;
            public string[] OrderBy;
            public string[] IgnoreFields;
            public Dictionary<string, string> FixedFields;
            public string condition;

            public string GetSql(Db2SourceContext dataSet)
            {
                Table tbl = dataSet.Tables[Schema, TableName];
                if (tbl == null)
                {
                    return null;
                }
                StringBuilder buf = new StringBuilder();
                string prefix = "select ";
                List<string> fmts = new List<string>();
                List<string> flds = new List<string>();
                foreach (Column c in tbl.Columns)
                {
                    if (c.HiddenLevel != HiddenLevel.Visible || IgnoreFields.Contains(c.Name))
                    {
                        continue;
                    }
                    flds.Add(dataSet.GetEscapedIdentifier(c.Name, true));
                    string fmt = c.StringFormat;
                    if (c.BaseType == "date")
                    {
                        fmt = "yyyy-MM-dd";
                    }
                    else if (c.BaseType == "timestamp")
                    {
                        fmt = null;
                    }
                    fmts.Add(fmt);
                    buf.Append(prefix);
                    string v;
                    if (FixedFields.TryGetValue(c.Name.ToLower(), out v))
                    {
                        buf.Append(Db2SourceContext.ToLiteralStr(v));
                        buf.Append(" as ");
                        buf.Append(dataSet.GetEscapedIdentifier(c.Name, true));
                    }
                    else
                    {
                        buf.Append(dataSet.GetEscapedIdentifier(c.Name, true));
                    }
                    prefix = ", ";
                }
                Fields = flds.ToArray();
                Formats = fmts.ToArray();
                buf.AppendLine();
                buf.Append("from ");
                buf.AppendLine(tbl.EscapedIdentifier(string.Empty));
                if (!string.IsNullOrEmpty(condition))
                {
                    buf.Append("where ");
                    buf.AppendLine(condition);
                }
                if (OrderBy != null && 0 < OrderBy.Length)
                {
                    prefix = "order by ";
                    foreach (string s in OrderBy)
                    {
                        buf.Append(prefix);
                        buf.Append(dataSet.GetEscapedIdentifier(s, true));
                        prefix = ", ";
                    }
                }
                buf.AppendLine();
                return buf.ToString();
            }
            public IDbCommand GetSqlCommand(Db2SourceContext dataSet, EventHandler<LogEventArgs> logEvent, IDbConnection connection)
            {
                string sql = GetSql(dataSet);
                if (string.IsNullOrEmpty(sql))
                {
                    return null;
                }
                IDbCommand cmd = dataSet.GetSqlCommand(sql, logEvent, connection);
                return cmd;
            }

            private static TableInfo ToTableInfo(string sect, List<string> values, Dictionary<string, string> fixedValues)
            {
                string sc;
                string tbl;
                string[] order = new string[0];
                string[] ignore = new string[0];
                string where = string.Empty;
                {
                    int p = sect.IndexOf('.');
                    if (p != -1)
                    {
                        sc = sect.Substring(0, p);
                        tbl = sect.Substring(p + 1);
                    }
                    else
                    {
                        sc = string.Empty;
                        tbl = sect;
                    }
                }
                foreach (string kv in values)
                {
                    int p = kv.IndexOf('=');
                    string k;
                    string v;
                    if (p != -1)
                    {
                        k = kv.Substring(0, p).Trim().ToUpper();
                        v = kv.Substring(p + 1).Trim();
                    }
                    else
                    {
                        k = kv.Trim().ToUpper();
                        v = string.Empty;
                    }
                    switch (k)
                    {
                        case "ORDER":
                            order = FromCsv(v);
                            break;
                        case "IGNORE":
                            ignore = FromCsv(v);
                            break;
                        case "WHERE":
                            where = v;
                            break;
                    }
                }
                return new TableInfo() { Schema = sc, TableName = tbl, OrderBy = order, IgnoreFields = ignore, condition = where, FixedFields = fixedValues };
            }
            public static TableInfo[] LoadFromConfigFile(string configFile)
            {
                List<TableInfo> l = new List<Db2Source.ExportTable.TableInfo>();
                string sect = null;
                List<string> vals = new List<string>();
                bool inFixed = false;
                Dictionary<string, string> fixVals = new Dictionary<string, string>();
                //string order = null;
                //string ignore = null;
                //string where = null;
                TableInfo info;
                foreach (string s in File.ReadLines(configFile))
                {
                    string s2 = s.Trim();
                    if (s2.StartsWith("[") && s2.EndsWith("]"))
                    {
                        if (s2.EndsWith("\\FIXED]"))
                        {
                            inFixed = true;
                        }
                        else
                        {
                            inFixed = false;
                            if (!string.IsNullOrEmpty(sect))
                            {
                                info = ToTableInfo(sect, vals, fixVals);
                                l.Add(info);
                            }
                            sect = s2.Substring(1, s2.Length - 2).Trim();
                            vals = new List<string>();
                            fixVals = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        if (inFixed)
                        {
                            int p = s2.IndexOf('=');
                            if (p != -1)
                            {
                                string k = s2.Substring(0, p).Trim();
                                string v = s2.Substring(p + 1).Trim();
                                fixVals[k] = v;
                            }
                        }
                        else
                        {
                            vals.Add(s2);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(sect))
                {
                    info = ToTableInfo(sect, vals, fixVals);
                    l.Add(info);
                }
                return l.ToArray();
            }
            public override string ToString()
            {
                return TableName;
            }
        }

        private static string EscapedText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            StringBuilder buf = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\t':
                        buf.Append("\\t");
                        break;
                    case '\n':
                        buf.Append("\\n");
                        break;
                    case '\r':
                        buf.Append("\\r");
                        break;
                    case '\\':
                        buf.Append("\\\\");
                        break;
                    default:
                        buf.Append(c);
                        break;
                }
            }
            return buf.ToString();
        }
        private static string GetArrayText(Array value, string format)
        {
            if (value == null)
            {
                return "\\N";
            }
            if (value.Length == 0)
            {
                return "{}";
            }
            StringBuilder buf = new StringBuilder();
            char prefix = '{';
            foreach (object v in value)
            {
                buf.Append(prefix);
                buf.Append(ToText(v, format));
                prefix = ',';
            }
            buf.Append('}');
            return buf.ToString();
        }
        private static string ToText(object value, string format)
        {
            if (value == null || value is DBNull)
            {
                return "\\N";
            }
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                if (!string.IsNullOrEmpty(format))
                {
                    return dt.ToString(format);
                }
                if (dt.Millisecond == 0)
                {
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
            if (value is bool)
            {
                bool v = (bool)value;
                return v ? "t" : "f";
            }
            if (value is string)
            {
                return EscapedText((string)value);
            }
            if (value.GetType().IsArray)
            {
                return GetArrayText((Array)value, format);
            }
            return value.ToString();
        }

        private void ExportTbl(Db2SourceContext dataSet, string baseDir, TableInfo table, IDbCommand command, Encoding encoding)
        {
            string dir = Path.Combine(baseDir, table.Schema);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, table.TableName + ".sql");
            using (StreamWriter sw = new StreamWriter(path, false, encoding))
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    sw.Write("COPY ");
                    sw.Write(dataSet.GetEscapedIdentifier(table.Schema, table.TableName, null, true));
                    string prefix = " (";
                    foreach (string f in table.Fields)
                    {
                        sw.Write(prefix);
                        sw.Write(f);
                        prefix = ", ";
                    }
                    sw.WriteLine(") FROM stdin;");
                    while (reader.Read())
                    {
                        object[] vals = new object[reader.FieldCount];
                        reader.GetValues(vals);
                        sw.Write(ToText(vals[0], table.Formats[0]));
                        for (int i = 1; i < vals.Length; i++)
                        {
                            sw.Write('\t');
                            sw.Write(ToText(vals[i], table.Formats[i]));
                        }
                        sw.WriteLine();
                    }
                    sw.WriteLine("\\.");
                }
            }
        }

        private async Task ExportAsync(Db2SourceContext dataSet, List<string> schemas, List<string> excludedSchemas, string configFile, string baseDir, Encoding encoding)
        {
            await dataSet.LoadSchemaAsync();
            Dictionary<string, bool> activeSchemas = GetActiveSchemaDict(dataSet, schemas, excludedSchemas);
            TableInfo[] tbls = TableInfo.LoadFromConfigFile(configFile);
            using (IDbConnection conn = dataSet.NewConnection(true))
            {
                foreach (TableInfo tbl in tbls)
                {
                    if (!activeSchemas.ContainsKey(tbl.Schema))
                    {
                        continue;
                    }
                    using (IDbCommand cmd = tbl.GetSqlCommand(DataSet, null, conn))
                    {
                        ExportTbl(dataSet, baseDir, tbl, cmd, encoding);
                    }
                }
            }
        }
    }
}
