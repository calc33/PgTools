using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum ExportMode
    {
        Unknown,
        Export,
        GenerateConfig,
        GenerateRule
    }
    public partial class ExportTable
    {
        public ExportTable()
        {
            _tableSql = Properties.Resources.TableSQL;
        }
        public NpgsqlDataSet DataSet { get; set; }
        private static string ToCsv(IList<string> items)
        {
            if (items.Count == 0)
            {
                return string.Empty;
            }
            string ret = items[0];
            for (int i = 1; i < items.Count; i++)
            {
                ret = ret + "," + items[i];
            }
            return ret;
        }
        private Dictionary<string,bool> GetActiveSchemaDict(Db2SourceContext dataSet, List<string> schemas, List<string> excludedSchemas)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
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
                ret[sn] = true;
            }
            return ret;
        }

        private static string _hostname = "localhost";
        private static int _port = 5432;
        private static string _database = "postgres";
        private static string _username = "postgres";
        //private static string _password = null;
        private static ExportMode _mode = ExportMode.Unknown;
        private static string _ruleFileName = "ExpTblRule.cfg";
        private static string _configFileName = "ExpTbl.cfg";
        private static bool _showUsage = false;
        private static string _exportDir = null;
        private static List<string> _schemas = new List<string>();
        private static List<string> _excludeSchemas = new List<string>();
        private static Encoding _encoding = Encoding.UTF8;
        public static void AnalyzeArguments(string[] args)
        {
            try
            {
                int n = args.Length;
                if (n == 0)
                {
                    ShowUsage();
                    return;
                }
                switch (args[0].ToLower())
                {
                    case "exp":
                        _mode = ExportMode.Export;
                        break;
                    case "genrule":
                        _mode = ExportMode.GenerateRule;
                        break;
                    case "genconf":
                        _mode = ExportMode.GenerateConfig;
                        break;
                    case "--help":
                    case "-?":
                        ShowUsage();
                        return;
                    default:
                        ShowFatalError(true, string.Format("不明なコマンドです: {0}", args[0]));
                        return;
                }
                for (int i = 1; i < n; i++)
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
                            ShowFatalError(_mode == ExportMode.GenerateRule, "-c は exp コマンドもしくは genconf コマンドでのみ使用できます");
                            _schemas.Add(args[++i].ToLower());
                            break;
                        case "--schema":
                            ShowFatalError(_mode == ExportMode.GenerateRule, "-c は exp コマンドもしくは genconf コマンドでのみ使用できます");
                            _schemas.Add(v.ToLower());
                            break;
                        case "-N":
                            ShowFatalError(_mode == ExportMode.GenerateRule, "-c は exp コマンドもしくは genconf コマンドでのみ使用できます");
                            _excludeSchemas.Add(args[++i].ToLower());
                            break;
                        case "--exclude-schema":
                            ShowFatalError(_mode == ExportMode.GenerateRule, "-c は exp コマンドもしくは genconf コマンドでのみ使用できます");
                            _excludeSchemas.Add(v.ToLower());
                            break;
                        case "-c":
                            ShowFatalError(_mode != ExportMode.Export, "-c は exp コマンドでのみ使用できます");
                            _ruleFileName = args[++i];
                            break;
                        case "--config":
                            ShowFatalError(_mode != ExportMode.Export, "--config は exp コマンドでのみ使用できます");
                            if (!string.IsNullOrEmpty(v))
                            {
                                _configFileName = v;
                            }
                            break;
                        case "-r":
                            ShowFatalError(_mode != ExportMode.GenerateConfig, "-r は genconf コマンドでのみ使用できます");
                            _ruleFileName = args[++i];
                            break;
                        case "--rule":
                            ShowFatalError(_mode != ExportMode.GenerateConfig, "--rule は genconf コマンドでのみ使用できます");
                            _ruleFileName = v;
                            break;
                        case "-E":
                            _encoding = Encoding.GetEncoding(args[++i]);
                            break;
                        case "--encoding":
                            _encoding = Encoding.GetEncoding(v);
                            break;
                        case "-?":
                        case "--help":
                            _showUsage = true;
                            break;
                        default:
                            switch (_mode)
                            {
                                case ExportMode.Export:
                                    _exportDir = a;
                                    break;
                                case ExportMode.GenerateConfig:
                                    _configFileName = a;
                                    break;
                                case ExportMode.GenerateRule:
                                    _ruleFileName = a;
                                    break;
                                default:
                                    _showUsage = true;
                                    break;
                            }
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
        public static void ShowFatalError(bool condition, string message)
        {
            if (!condition)
            {
                return;
            }
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

        private static IDbConnection TryLogin(NpgsqlConnectionInfo info)
        {
            if (info.Password == null)
            {
                Console.Error.Write("パスワード: ");
                Console.Error.Flush();
                info.Password = Console.In.ReadLine();
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
                    Console.Error.Write("パスワード: ");
                    Console.Error.Flush();
                    info.Password = Console.In.ReadLine();
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
            ExportTable obj = new ExportTable();
            obj.DataSet = new NpgsqlDataSet(info);
            if (_exportDir == null)
            {
                _exportDir = Environment.CurrentDirectory;
            }
            Task t = null;
            switch (_mode)
            {
                case ExportMode.Export:
                    t = obj.ExportAsync(obj.DataSet, _schemas, _excludeSchemas, _configFileName, _exportDir, _encoding);
                    break;
                case ExportMode.GenerateConfig:
                    t = obj.ExportConfigAsync(obj.DataSet, _schemas, _excludeSchemas, _configFileName, _ruleFileName, _encoding);
                    break;
                case ExportMode.GenerateRule:
                    File.WriteAllText(_ruleFileName, Properties.Resources.DBExpRule, _encoding);
                    break;
            }
            if (t == null)
            {
                return;
            }
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
