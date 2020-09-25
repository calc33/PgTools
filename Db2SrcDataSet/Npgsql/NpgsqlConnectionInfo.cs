using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Threading;

namespace Db2Source
{
    public class NpgsqlConnectionInfo: ConnectionInfo
    {
        private const int DEFAULT_PGSQL_PORT = 5432;
        private const string DEFAULT_PGSQL_DBNAME = "postgres";
        public static string GetPgPassConfPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "postgresql", "pgpass.conf");
        }

        public static string[] ExtractPgPassEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry))
            {
                return new string[0];
            }
            List<string> l = new List<string>();
            StringBuilder buf = new StringBuilder(entry.Length);

            int n = entry.Length;
            for (int i = 0; i < n; i++)
            {
                char c = entry[i];
                switch (c)
                {
                    case ':':
                        l.Add(buf.ToString());
                        buf.Clear();
                        break;
                    case '\\':
                        i++;
                        if (i < n)
                        {
                            char c2 = entry[i];
                            buf.Append(c2);
                        }
                        break;
                    default:
                        if (char.IsSurrogate(c))
                        {
                            buf.Append(c);
                            i++;
                        }
                        buf.Append(entry[i]);
                        break;
                }
            }
            l.Add(buf.ToString());
            return l.ToArray();
        }

        private static string EscapeForPgPass(string value)
        {
            StringBuilder buf = new StringBuilder(value.Length * 2);
            foreach (char c in value)
            {
                if (c == ':' || c == '\\')
                {
                    buf.Append('\\');
                }
                buf.Append(c);
            }
            return buf.ToString();
        }
        public static string ToPgPassEntry(string host, int port, string database, string username, string password)
        {
            return string.Format("{0}:{1}:{2}:{3}:{4}", 
                EscapeForPgPass(host), port, EscapeForPgPass(database),
                EscapeForPgPass(username), EscapeForPgPass(password));
        }

        private static string GetStoredPassword(string host, int port, string database, string username)
        {
            string path = GetPgPassConfPath();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string[] target = new string[] { host, port.ToString(), database, username };
                foreach (string s in File.ReadLines(path, Encoding.Default))
                {
                    string[] entries = ExtractPgPassEntry(s);
                    if (entries.Length != 5)
                    {
                        continue;
                    }
                    bool matched = true;
                    for (int i = 0; i < target.Length; i++)
                    {
                        if (string.Compare(target[i], entries[i], true) != 0 && entries[i] != "*")
                        {
                            matched = false;
                            break;
                        }
                    }
                    if (matched)
                    {
                        return entries[4];
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public override bool FillStoredPassword(bool testConnectoin)
        {
            string pass = GetStoredPassword(ServerName, ServerPort, DatabaseName, UserName);
            if (pass == null)
            {
                return false;
            }
            Password = pass;
            IsPasswordHidden = true;
            if (!testConnectoin)
            {
                return true;
            }
            try
            {
                IDbConnection conn = NewConnection();
                conn.Dispose();
            }
            catch
            {
                Password = null;
                IsPasswordHidden = false;
                return false;
            }
            return true;
        }

        public override void SavePassword()
        {
            if (Password == GetStoredPassword(ServerName, ServerPort, DatabaseName, UserName))
            {
                return;
            }
            string path = GetPgPassConfPath();
            string lockPath = path + ".$lock$";
            string target = ToPgPassEntry(ServerName, ServerPort, DatabaseName, UserName, null);
            string newEntry = target + EscapeForPgPass(Password);
            if (!File.Exists(path))
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, newEntry + Environment.NewLine);
                return;
            }
            FileStream stream = null;
            FileStream lockStream = null;
            // pgpass.confの更新は競合すると困るので排他制御する
            try
            {
                DateTime timeout = DateTime.Now.AddSeconds(3.0);    // タイムアウト3秒
                while (stream == null && DateTime.Now <= timeout)
                {
                    // 排他制御のために一時ファイル pgpass.conf.$lock$ を作成(書き込み処理が終わったら削除)
                    // pgpass.conf自体が排他制御できなかった場合の保険(DB2Src.net同士のみの排他制御になる)
                    if (lockStream == null)
                    {
                        try
                        {
                            lockStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0, FileOptions.DeleteOnClose);
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(100);
                        }
                        catch (SecurityException)
                        {
                            Thread.Sleep(100);
                        }
                    }
                    try
                    {
                        // pgpass.conf自身も排他で開く
                        stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                    catch (SecurityException)
                    {
                        Thread.Sleep(100);
                    }
                }
                if (lockStream == null)
                {
                    // エラーを発生させるために再度ストリームを開く
                    // (ここまでの状況からして確実にエラーになるが、エラーにならなければそれはそれでOK)
                    stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                else
                {
                    if (stream == null)
                    {
                        // ロックファイルによる排他制御はできたが、ファイル自体の完全排他が駄目な場合
                        // エディタで開きっぱなしのケースであると想定してファイル側の排他制御はなしで続行
                        stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    }
                }
                using (stream)
                {
                    List<string> entries = new List<string>();
                    bool added = false;
                    stream.Position = 0;
                    byte[] buf = new byte[stream.Length];
                    stream.Read(buf, 0, buf.Length);
                    string all = Encoding.Default.GetString(buf);
                    foreach (string s in all.Split('\r', '\n'))
                    {
                        if (string.IsNullOrEmpty(s))
                        {
                            continue;
                        }
                        if (s.StartsWith(target, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // エントリはあるがパスワードが変わっていた場合
                            entries.Add(newEntry);
                            added = true;
                        }
                        else
                        {
                            entries.Add(s);
                        }
                    }
                    if (!added)
                    {
                        // 新しく追加する場合
                        entries.Insert(0, newEntry);
                    }
                    stream.Position = 0;
                    byte[] newline = Encoding.Default.GetBytes(Environment.NewLine);
                    foreach (string s in entries)
                    {
                        byte[] b = Encoding.Default.GetBytes(s);
                        stream.Write(b, 0, b.Length);
                        stream.Write(newline, 0, newline.Length);
                    }
                    if (stream.Position < stream.Length)
                    {
                        stream.SetLength(stream.Position);
                    }
                    stream.Flush();
                }
            }
            finally
            {
                if (lockStream != null)
                {
                    lockStream.Dispose();
                }
            }
        }

        public static ConnectionInfo[] GetKnownConnectionInfos()
        {
            string path = GetPgPassConfPath();
            if (!File.Exists(path))
            {
                return new ConnectionInfo[0];
            }
            List<ConnectionInfo> list = new List<ConnectionInfo>();
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (StreamReader sr = new StreamReader(stream, Encoding.Default))
            {
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine();
                    if (string.IsNullOrEmpty(s))
                    {
                        continue;
                    }
                    string[] prms = ExtractPgPassEntry(s);
                    foreach (string p in prms)
                    {
                        // ワイルドカードを使用しているエントリーは対象外
                        if (p == "*")
                        {
                            continue;
                        }
                    }
                    if (prms.Length < 5)
                    {
                        continue;
                    }
                    NpgsqlConnectionInfo info = new NpgsqlConnectionInfo();
                    try
                    {
                        info.ServerName = prms[0];
                        info.ServerPort = int.Parse(prms[1]);
                        info.DatabaseName = prms[2];
                        info.UserName = prms[3];
                        info.Password = prms[4];
                        info.IsPasswordHidden = true;
                        info.Name = info.GetDefaultName();
                        list.Add(info);
                    }
                    catch { }
                }
            }
            list.Sort(CompareByName);
            return list.ToArray();
        }
        private const string DATABASE_TYPE = "Npgsql";
        private const string DATABASE_DESC = "Postgres(NpgSql)";
        [InputField("ポート", 15)]
        public int ServerPort { get; set; }
        [InputField("データベース名", 16)]
        public string DatabaseName { get; set; }
        public override string DatabaseType
        {
            get { return DATABASE_TYPE; }
        }
        public override string DatabaseDesc
        {
            get { return DATABASE_DESC; }
        }

        public override string GetDefaultName()
        {
            string sPort = (ServerPort == DEFAULT_PGSQL_PORT) ? string.Empty : ":" + ServerPort.ToString();
            string sDb = (DatabaseName == DEFAULT_PGSQL_DBNAME) ? string.Empty : "/" + DatabaseName;
            return string.Format("{0}@{1}{2}{3}", UserName, ServerName, sPort, sDb);
        }

        public override Db2SourceContext NewDataSet()
        {
            return new NpgsqlDataSet(this);
        }
        public NpgsqlConnectionInfo()
        {
            ServerName = "localhost";
            ServerPort = 5432;
            DatabaseName = "postgres";
            UserName = "postgres";
        }
        public override string ToConnectionString()
        {
            return string.Format("Host={0};Database={1};Port={2};Username={3};Password={4};Persist Security Info=True;Application Name=DB2Src.net",
                ServerName, DatabaseName, ServerPort, UserName, Password);
        }
        public override IDbConnection NewConnection()
        {
            NpgsqlConnection conn = new NpgsqlConnection(ToConnectionString());
            try
            {
                conn.Open();
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        static NpgsqlConnectionInfo()
        {
            ConnectionInfo.RegisterDatabaseType(DATABASE_TYPE, typeof(NpgsqlConnectionInfo));
        }

        public override int ContentCompareTo(ConnectionInfo obj)
        {
            NpgsqlConnectionInfo o = obj as NpgsqlConnectionInfo;
            int ret = base.ContentCompareTo(o);
            if (ret != 0)
            {
                return ret;
            }
            ret = ServerPort - o.ServerPort;
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(DatabaseName, o.DatabaseName);
            if (ret != 0)
            {
                return ret;
            }
            return 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + ServerPort.GetHashCode() + GetStrHash(DatabaseName);
        }
    }
}
