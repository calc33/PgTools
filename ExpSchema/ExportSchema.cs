﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Db2Source
{
    public class ExportSchema
    {
        public Db2SourceContext DataSet { get; set; }
        private void ExportTable(StringBuilder buffer, Table table)
        {
            if (table == null)
            {
                return;
            }
            buffer.AppendLine(DataSet.GetSQL(table, string.Empty, ";", 0, true, true));
            List<Constraint> list = new List<Constraint>(table.Constraints);
            list.Sort();
            int lastLength = buffer.Length;
            foreach (Constraint c in list)
            {
                switch (c.ConstraintType)
                {
                    case ConstraintType.Primary:
                        // 本体ソース内で出力している
                        break;
                    case ConstraintType.Unique:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                    case ConstraintType.ForeignKey:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                    case ConstraintType.Check:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                }
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            if (!string.IsNullOrEmpty(table.CommentText))
            {
                buffer.Append(DataSet.GetSQL(table.Comment, string.Empty, ";", 0, true));
            }
            foreach (Column c in table.Columns)
            {
                if (!string.IsNullOrEmpty(c.CommentText))
                {
                    buffer.Append(DataSet.GetSQL(c.Comment, string.Empty, ";", 0, true));
                }
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            foreach (Trigger t in table.Triggers)
            {
                buffer.Append(DataSet.GetSQL(t, string.Empty, ";", 0, true));
                buffer.AppendLine();
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            foreach (Index i in table.Indexes)
            {
                buffer.Append(DataSet.GetSQL(i, string.Empty, ";", 0, true));
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
        }
        private void ExportView(StringBuilder buffer, View view)
        {
            if (view == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(view, string.Empty, ";", 0, true));
            if (!string.IsNullOrEmpty(view.CommentText))
            {
                buffer.Append(DataSet.GetSQL(view.Comment, string.Empty, ";", 0, true));
            }
            foreach (Column c in view.Columns)
            {
                if (!string.IsNullOrEmpty(c.CommentText))
                {
                    buffer.Append(DataSet.GetSQL(c.Comment, string.Empty, ";", 0, true));
                }
            }
            buffer.AppendLine();
        }
        private void ExportStoredFunction(StringBuilder buffer, StoredFunction function)
        {
            if (function == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(function, string.Empty, ";", 0, true));
            if (!string.IsNullOrEmpty(function.CommentText))
            {
                buffer.Append(DataSet.GetSQL(function.Comment, string.Empty, ";", 0, true));
            }
        }
        private void ExportSequence(StringBuilder buffer, Sequence sequence)
        {
            if (sequence == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(sequence, string.Empty, ";", 0, true, true));
        }
        private void ExportComplexType(StringBuilder buffer, ComplexType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportEnumType(StringBuilder buffer, EnumType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportBasicType(StringBuilder buffer, BasicType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportRangeType(StringBuilder buffer, RangeType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private async Task ExportAsync(Db2SourceContext dataSet, List<string> schemas, List<string> excludedSchemas, string baseDir, Encoding encoding)
        {
            Dictionary<string, bool> exported = new Dictionary<string, bool>();
            await dataSet.LoadSchemaAsync();
            foreach (Schema s in dataSet.Schemas)
            {
                string sn = s.Name.ToLower();
                if (_schemas.Count != 0)
                {
                    if (schemas.IndexOf(sn) == -1)
                    {
                        continue;
                    }
                }
                else
                {
                    if (s.IsHidden)
                    {
                        continue;
                    }
                    if (excludedSchemas.IndexOf(sn) != -1)
                    {
                        continue;
                    }
                }
                Console.Error.WriteLine(s.Name + "を出力しています");
                string schemaDir = Path.Combine(baseDir, s.Name);
                foreach (SchemaObject obj in s.Objects)
                {
                    StringBuilder buf = new StringBuilder();
                    if (obj is Table)
                    {
                        ExportTable(buf, (Table)obj);
                    }
                    else if (obj is View)
                    {
                        ExportView(buf, (View)obj);
                    }
                    else if (obj is StoredFunction)
                    {
                        ExportStoredFunction(buf, (StoredFunction)obj);
                    }
                    else if (obj is Sequence)
                    {
                        ExportSequence(buf, (Sequence)obj);
                    }
                    else if (obj is ComplexType)
                    {
                        ExportComplexType(buf, (ComplexType)obj);
                    }
                    else if (obj is EnumType)
                    {
                        ExportEnumType(buf, (EnumType)obj);
                    }
                    else if (obj is BasicType)
                    {
                        ExportBasicType(buf, (BasicType)obj);
                    }
                    else if (obj is RangeType)
                    {
                        ExportRangeType(buf, (RangeType)obj);
                    }
                    if (buf.Length != 0)
                    {
                        string dir = Path.Combine(schemaDir, obj.GetExportFolderName());
                        string path = Path.Combine(dir, obj.Name + ".sql");
                        Directory.CreateDirectory(dir);
                        bool append = exported.ContainsKey(path);
                        encoding = encoding ?? Encoding.UTF8;
                        using (StreamWriter sw = new StreamWriter(path, append, encoding))
                        {
                            if (append)
                            {
                                sw.Write(DataSet.GetNewLine());
                            }
                            sw.Write(DataSet.NormalizeNewLine(buf));
                        }
                        exported[path] = true;
                    }
                }
            }
        }
        private static string _hostname = "localhost";
        private static int _port = 5432;
        private static string _database = "postgres";
        private static string _username = "postgres";
        //private static string _password = null;
        private static bool _showUsage = false;
        private static string _exportDir = null;
        private static List<string> _schemas = new List<string>();
        private static List<string> _excludeSchemas = new List<string>();
        private static Encoding _encoding = Encoding.UTF8;
        private static NewLineRule _newLine = NewLineRule.None;
        private static readonly Dictionary<string, NewLineRule> ArgToNewLineRule = new Dictionary<string, NewLineRule>()
        {
            { "cr", NewLineRule.Cr },
            { "lf", NewLineRule.Lf },
            { "crlf", NewLineRule.Crlf },
        };
        public static void AnalyzeArguments(string[] args)
        {
            try
            {
                int n = args.Length;
                for (int i = 0; i < n; i++)
                {
                    string a = args[i];
                    string v = null;
                    if (a.StartsWith("--"))
                    {
                        int p = a.IndexOf('=');
                        if (0 <= p)
                        {
                            v = a.Substring(p + 1);
                            a = a.Substring(0, p);
                        }
                    }
                    switch (a)
                    {
                        case "-h":
                            _hostname = args[++i];
                            break;
                        case "--host":
                            _hostname = v;
                            break;
                        case "-p":
                            _port = int.Parse(args[++i]);
                            break;
                        case "--port":
                            _port = int.Parse(v);
                            break;
                        case "-d":
                            _database = args[++i];
                            break;
                        case "--dbname":
                            _database = v;
                            break;
                        case "-U":
                            _username = args[++i];
                            break;
                        case "--username":
                            _username = v;
                            break;
                        case "-n":
                            _schemas.Add(args[++i].ToLower());
                            break;
                        case "--schema":
                            _schemas.Add(v.ToLower());
                            break;
                        case "-N":
                            _excludeSchemas.Add(args[++i].ToLower());
                            break;
                        case "--exclude-schema":
                            _excludeSchemas.Add(v.ToLower());
                            break;
                        case "-E":
                            int cp;
                            v = args[++i];
                            if (int.TryParse(v, out cp))
                            {
                                _encoding = Encoding.GetEncoding(cp);
                            }
                            else
                            {
                                _encoding = Encoding.GetEncoding(v);
                            }
                            break;
                        case "--encoding":
                            if (int.TryParse(v, out cp))
                            {
                                _encoding = Encoding.GetEncoding(cp);
                            }
                            else
                            {
                                _encoding = Encoding.GetEncoding(v);
                            }
                            break;
                        case "--newline":
                            if (!ArgToNewLineRule.TryGetValue(v.ToLower(), out _newLine))
                            {
                                ShowArgumentError(string.Format("不明な改行指定です: {0}", v));
                            }
                            break;
                        case "-?":
                        case "--help":
                            _showUsage = true;
                            break;
                        default:
                            if (a.StartsWith("-"))
                            {
                                ShowArgumentError(string.Format("不明なオプションです: {0}", a));
                            }
                            _exportDir = a;
                            break;
                    }
                }
            }
            catch
            {
                _showUsage = true;
            }
            if (_showUsage)
            {
                ShowUsage();
            }
        }

        public static void ShowArgumentError(string message)
        {
            Console.Error.WriteLine(message);
            Console.Error.Flush();
            Environment.Exit(1);
        }
        public static void ShowUsage()
        {
            Console.Error.Write(Properties.Resources.Usage);
            Console.Error.Flush();
            Environment.Exit(1);
        }

        private static string ReadPassword()
        {
            Console.Error.Write("パスワード: ");
            Console.Error.Flush();

            StringBuilder buf = new StringBuilder();
            for (;;)
            {
                ConsoleKeyInfo k = Console.ReadKey(true);
                switch (k.Key)
                {
                    case ConsoleKey.Enter:
                        Console.Out.WriteLine();
                        return buf.ToString();
                    case ConsoleKey.Backspace:
                        if (buf.Length == 0)
                        {
                            Console.Beep();
                            break;
                        }
                        buf.Length--;
                        break;
                    default:
                        char c = k.KeyChar;
                        if (char.IsControl(c))
                        {
                            Console.Beep();
                            break;
                        }
                        if (char.IsLetter(k.KeyChar))
                        {
                            if (Console.CapsLock ^ ((k.Modifiers & ConsoleModifiers.Shift) != 0))
                            {
                                c = char.ToUpper(c);
                            }
                            else
                            {
                                c = char.ToLower(c);
                            }
                        }
                        buf.Append(c);
                        break;
                }
            }
            //return null;
        }
        private static IDbConnection TryLogin(NpgsqlConnectionInfo info)
        {
            if (info.Password == null)
            {
                info.Password = ReadPassword();
            }
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    IDbConnection conn = info.NewConnection();
                    return conn;
                }
                catch
                {
                    info.Password = ReadPassword();
                }
            }
            return null;
        }

        public static void Execute()
        {
            NpgsqlConnectionInfo info = new NpgsqlConnectionInfo();
            info.ServerName = _hostname;
            info.ServerPort = _port;
            info.DatabaseName = _database;
            info.UserName = _username;
            info.FillStoredPassword(false);
            using (IDbConnection conn = TryLogin(info))
            {
                if (conn == null)
                {
                    Console.Error.WriteLine("ログインできませんでした。終了します。");
                    Environment.Exit(1);
                }
            }
            ExportSchema obj = new ExportSchema();
            obj.DataSet = new NpgsqlDataSet(info) { NewLineRule = _newLine };
            if (_exportDir == null)
            {
                _exportDir = Environment.CurrentDirectory;
            }
            Task t = obj.ExportAsync(obj.DataSet, _schemas, _excludeSchemas, _exportDir, _encoding);
            while (!t.IsCompleted)
            {
                Thread.Sleep(100);
            }
            if (t.IsFaulted)
            {
                Console.Error.WriteLine(t.ToString());
                Console.Error.Flush();
                Environment.Exit(1);
            }
        }
    }
}
