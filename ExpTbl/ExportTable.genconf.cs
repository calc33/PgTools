using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class ExportTable
    {
        private string _tableSql;
        private Dictionary<string, bool> _ignoreFields = new Dictionary<string, bool>();
        private Dictionary<string, string> _fixedFields = new Dictionary<string, string>();
        private void CommitRuleSection(string section, List<string> lines)
        {
            switch (section)
            {
                case "[TABLE_SQL]":
                    StringBuilder buf = new StringBuilder();
                    foreach (string s in lines)
                    {
                        buf.AppendLine(s);
                    }
                    _tableSql = buf.ToString().TrimEnd();
                    break;
                case "[IGNORE_FIELDS]":
                    _ignoreFields = new Dictionary<string, bool>();
                    foreach (string s in lines)
                    {
                        _ignoreFields[s.ToLower()] = true;
                    }
                    break;
                case "[FIEXED_FIELDS]":
                    _fixedFields = new Dictionary<string, string>();
                    foreach (string s in lines)
                    {
                        int p = s.IndexOf('=');
                        if (p == -1)
                        {
                            continue;
                        }
                        string k = s.Substring(0, p);
                        string v = s.Substring(p + 1);
                        _fixedFields[k.ToLower()] = v;
                    }
                    break;
            }

        }
        private void LoadFromRuleFile(string ruleFile, Encoding encoding)
        {
            if (string.IsNullOrEmpty(ruleFile))
            {
                return;
            }
            if (!File.Exists(ruleFile))
            {

                throw new FileNotFoundException("ファイルが見つかりません", ruleFile);
            }
            List<string> l = new List<string>();
            string sect = null;
            foreach (string s in File.ReadLines(ruleFile, encoding))
            {
                string s2 = s.Trim();
                if (s2.StartsWith("["))
                {
                    CommitRuleSection(sect, l);
                    sect = s2.ToUpper();
                    l = new List<string>();
                }
                else if (s2.StartsWith("#"))
                {
                    continue;
                }
                else
                {
                    l.Add(s);
                }
            }
            CommitRuleSection(sect, l);
        }
        private async Task ExportConfigAsync(Db2SourceContext dataSet, List<string> schemas, List<string> excludedSchemas, string configFile, string ruleFile, Encoding encoding)
        {
            LoadFromRuleFile(ruleFile, encoding);
            await dataSet.LoadSchemaAsync();
            Dictionary<string, bool> activeSchemas = GetActiveSchemaDict(dataSet, schemas, excludedSchemas);
            StringBuilder buf = new StringBuilder();
            IDbConnection conn = dataSet.NewConnection();
            IDbCommand cmdTbl = dataSet.GetSqlCommand(_tableSql, conn);
            IDbCommand cmdCol = dataSet.GetSqlCommand(Properties.Resources.ColumnSQL, conn);
            IDbCommand cmdKey = dataSet.GetSqlCommand(Properties.Resources.KeyConsSQL, conn);
            try
            {
                IDbDataParameter pC = cmdCol.Parameters[0] as IDbDataParameter;
                //pC.ParameterName = "oid";
                pC.DbType = DbType.Int64;
                //cmdCol.Parameters.Add(pC);
                IDbDataParameter pK = cmdKey.Parameters[0] as IDbDataParameter;
                //pK.ParameterName = "oid";
                pK.DbType = DbType.Int64;
                //cmdKey.Parameters.Add(pK);
                List<TableInfo> l = new List<TableInfo>();
                using (IDataReader reader = cmdTbl.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        uint id = (uint)reader.GetValue(0);
                        string sch = reader.GetString(1);
                        string tbl = reader.GetString(2);
                        if (!activeSchemas.ContainsKey(sch.ToLower()))
                        {
                            continue;
                        }
                        l.Add(new TableInfo() { Oid = id, Schema = sch, TableName = tbl });
                    }
                }
                for(int i = 0; i < l.Count; i++)
                {
                    TableInfo info = l[i];
                    SortedList<int, string> lCol = new SortedList<int, string>();
                    List<string> ignoreCols = new List<string>();
                    List<ValueInfo> fixedCols = new List<ValueInfo>();
                    pC.Value = (Int64)info.Oid;
                    using (IDataReader readerC = cmdCol.ExecuteReader())
                    {
                        while (readerC.Read())
                        {
                            string col = readerC.GetString(0);
                            string[] keys = new string[]
                            {
                                (info.Schema + "." + info.TableName + "." + col).ToLower(),
                                (info.TableName + "." + col).ToLower(),
                                col.ToLower()
                            };
                            int idx = readerC.GetInt32(1);
                            lCol.Add(idx, col);
                            if (_ignoreFields.ContainsKey(keys[0]) || _ignoreFields.ContainsKey(keys[1]) || _ignoreFields.ContainsKey(keys[2]))
                            {
                                ignoreCols.Add(col);
                            }
                            foreach (string k in keys)
                            {
                                if (_fixedFields.ContainsKey(k))
                                {
                                    fixedCols.Add(new ValueInfo() { FieldName = col, Value = _fixedFields[k] });
                                    break;
                                }
                            }
                        }
                    }
                    info.IgnoreFields = ignoreCols.ToArray();
                    List<string> keyCols = new List<string>();
                    pK.Value = (Int64)info.Oid;
                    using (IDataReader readerK = cmdKey.ExecuteReader())
                    {
                        if (readerK.Read())
                        {
                            Array a = readerK.GetValue(1) as Array;
                            foreach (object o in a)
                            {
                                int p = Convert.ToInt32(o);
                                keyCols.Add(lCol[p]);
                            }
                        }
                    }
                    info.OrderBy = keyCols.ToArray();
                    buf.AppendLine(string.Format("[{0}.{1}]", info.Schema, info.TableName));
                    buf.Append("ORDER=");
                    buf.AppendLine(ToCsv(info.OrderBy));
                    buf.Append("WHERE=");
                    buf.AppendLine();
                    buf.Append("IGNORE=");
                    buf.AppendLine(ToCsv(info.IgnoreFields));
                    buf.AppendLine(string.Format("[{0}.{1}\\FIXED]", info.Schema, info.TableName));
                    List<string> fixVals = new List<string>();
                    foreach (ValueInfo f in fixedCols)
                    {
                        buf.Append(f.FieldName);
                        buf.Append('=');
                        buf.AppendLine(f.Value);
                    }
                    buf.AppendLine();
                }
            }
            finally
            {
                cmdTbl.Dispose();
                cmdCol.Dispose();
                cmdKey.Dispose();
                conn.Dispose();
            }
            File.WriteAllText(_configFileName, buf.ToString(), encoding);
        }
    }
}
