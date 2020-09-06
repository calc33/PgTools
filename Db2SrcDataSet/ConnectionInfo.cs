using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Db2Source
{
    public class InputFieldAttribute: Attribute
    {
        public string Title { get; private set; }
        public int Order { get; private set; }
        public bool HiddenField { get; private set; }
        public InputFieldAttribute(string title, int order, bool hiddenField = false) : base()
        {
            Title = title;
            Order = order;
            HiddenField = hiddenField;
        }
    }
    public delegate void Command(object sender, ConnectionInfo info);
    public class ExtraCommand
    {
        public string Title { get; set; }
        public string Category { get; set; }
        private Command Command { get; set; }
        public void Invoke(object sender, ConnectionInfo info)
        {
            Command?.Invoke(sender, info);
        }
        public ExtraCommand(ConnectionInfo owner, string title, string category, Command command)
        {
            Title = title;
            Category = category;
            Command = command;
            if (owner != null)
            {
                owner.ExtraCommands.Add(this);
            }
        }
    }
    public class ExtraCommandCollecion: IList<ExtraCommand>, IList
    {
        private List<ExtraCommand> _list = new List<ExtraCommand>();

        public ExtraCommand this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_list)[index];
            }

            set
            {
                ((IList)_list)[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        public int Add(object value)
        {
            return ((IList)_list).Add(value);
        }

        public void Add(ExtraCommand item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public bool Contains(ExtraCommand item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public void CopyTo(ExtraCommand[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ExtraCommand> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public int IndexOf(ExtraCommand item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        public void Insert(int index, ExtraCommand item)
        {
            _list.Insert(index, item);
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
        }

        public bool Remove(ExtraCommand item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
    public abstract partial class ConnectionInfo: IComparable
    {
        private static Dictionary<string, Type> _databaseTypeToType = new Dictionary<string, Type>();
        public static void RegisterDatabaseType(string databaseType, Type type)
        {
            _databaseTypeToType[databaseType] = type;
        }
        public static string[] DatabaseTypes
        {
            get
            {
                string[] ret = _databaseTypeToType.Keys.ToArray();
                Array.Sort(ret);
                return ret;
            }
        }
        public static Type GetConnectionInfoType(string databaseType)
        {
            Type t;
            if (!_databaseTypeToType.TryGetValue(databaseType, out t))
            {
                return null;
            }
            return t;
        }
        public static ConnectionInfo NewConnectionInfo(string databaseType)
        {
            Type t;
            if (!_databaseTypeToType.TryGetValue(databaseType, out t))
            {
                return null;
            }
            ConstructorInfo ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                return null;
            }
            return ctor.Invoke(null) as ConnectionInfo;
        }
        public abstract Db2SourceContext NewDataSet();
        public virtual bool FillStoredPassword(bool testConnectoin)
        {
            return false;
        }
        public virtual void SavePassword() { }

        public string Name { get; set; }

        public virtual string Description
        {
            get
            {
                return string.Format("{0}:{1}", DatabaseType, GetDefaultName());
            }
        }
        public virtual string GetDefaultName()
        {
            return string.Format("{0}@{1}", UserName, ServerName);
        }
        /// <summary>
        /// データベースの種別を識別する文字列
        /// </summary>
        public abstract string DatabaseType { get; }
        /// <summary>
        /// 画面に表示する際のデータベース名称
        /// </summary>
        public abstract string DatabaseDesc { get; }
        [InputField("サーバー", 10)]
        public string ServerName { get; set; }
        [InputField("ユーザー", 20)]
        public string UserName { get; set; }
        [InputField("パスワード", 30, true)]
        [JsonIgnore]
        public string Password { get; set; }
        [JsonProperty(PropertyName = "Password")]
        public string CryptedPassword
        {
            get
            {
                return Password;
            }
            set
            {
                Password = value;
            }
        }

        [JsonIgnore]
        public bool IsPasswordHidden { get; set; }
        [JsonIgnore]
        public ExtraCommandCollecion ExtraCommands { get; } = new ExtraCommandCollecion();

        internal protected virtual void Load(Dictionary<string, string> data)
        {
            ServerName = data["ServerName"];
            UserName = data["UserName"];
            CryptedPassword = data["Password"];
        }
        internal protected virtual void Load(IDataReader reader)
        {

        }
        //internal protected virtual void Save(Dictionary<string, string> data)
        //{
        //    data["server"] = ServerName;
        //    data["username"] = UserName;
        //    data["password"] = CryptedPassword;
        //}

        public override bool Equals(object obj)
        {
            if (!(obj is ConnectionInfo))
            {
                return false;
            }
            ConnectionInfo o = (ConnectionInfo)obj;
            return string.Equals(Name, o.Name) && ContentEquals(o);
        }
        public bool ContentEquals(ConnectionInfo obj)
        {
            return (ContentCompareTo(obj) == 0);
        }
        public virtual int ContentCompareTo(ConnectionInfo obj)
        {
            if (obj == null)
            {
                return -1;
            }
            int ret;
            ret = string.Compare(DatabaseType, obj.DatabaseType);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(ServerName, obj.ServerName);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(UserName, obj.UserName);
            if (ret != 0)
            {
                return ret;
            }
            return 0;
        }
        protected static int GetStrHash(string value)
        {
            if (value == null)
            {
                return 0;
            }
            return value.GetHashCode();
        }
        public override int GetHashCode()
        {
            return GetStrHash(Name)
                + GetStrHash(DatabaseType)
                + GetStrHash(ServerName)
                + GetStrHash(UserName);
            //+ Password.GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
        public abstract string ToConnectionString();
        public abstract IDbConnection NewConnection();
        public static int CompareByName(ConnectionInfo item1, ConnectionInfo item2)
        {
            if (item1 == null || item2 == null)
            {
                return (item1 == null ? 1 : 0) - (item2 == null ? 1 : 0);
            }
            return string.Compare(item1.Name, item2.Name, true, CultureInfo.CurrentCulture);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is ConnectionInfo))
            {
                return -1;
            }
            ConnectionInfo c = (ConnectionInfo)obj;
            int ret = string.Compare(Name, c.Name);
            if (ret != 0)
            {
                return ret;
            }
            return ContentCompareTo(c);
        }
    }

    public sealed partial class ConnectionList: IList<ConnectionInfo>
    {
        private static string InitDefaultConnectionListPath()
        {
            string path = System.IO.Path.Combine(Db2SourceContext.AppDataDir, "ConnectionList.db");
            return path;
        }
        public static string DefaultConnectionListPath = InitDefaultConnectionListPath();
        private static List<Type> _connectionInfoClasses = new List<Type>();
        public static void Register(Type connectionInfoType)
        {
            if (connectionInfoType == null)
            {
                throw new ArgumentNullException("connectionInfoType");
            }
            //if (!connectionInfoType.IsAssignableFrom(typeof(ConnectionInfo)))
            if (!connectionInfoType.IsSubclassOf(typeof(ConnectionInfo)))
            {
                throw new ArgumentException(string.Format("ConnectionInfo型を継承していないクラスです: {0}", connectionInfoType.FullName));
            }
            _connectionInfoClasses.Add(connectionInfoType);
        }

        [JsonIgnore]
        public string Path { get; private set; }
        private List<ConnectionInfo> _list = new List<ConnectionInfo>();
        private List<ConnectionInfo> _backlist = new List<ConnectionInfo>();

        public ConnectionList(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            Path = path;
            Load();
        }
        public ConnectionList()
        {
            Path = DefaultConnectionListPath;
            Load();
        }

        //private List<ConnectionInfo> LoadInternal(string json)
        //{
        //    List<Dictionary<string, string>> dicts = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        //    List<ConnectionInfo> l = new List<ConnectionInfo>();
        //    foreach (Dictionary<string, string> d in dicts)
        //    {
        //        ConnectionInfo info = ConnectionInfo.NewConnectionInfo(d["Name"]);
        //        if (info == null)
        //        {
        //            info = new InvalidConnectionInfo(d["Name"]);
        //        }
        //        info.Load(d);
        //        l.Add(info);
        //    }
        //    //l.Sort(ConnectionInfo.CompareByName);
        //    return l;
        //}
        //private static readonly TimeSpan LOAD_TIMEOUT = new TimeSpan(0, 0, 10); //10秒
        //private FileStream OpenStream(FileMode mode, FileAccess access, FileShare share)
        //{
        //    DateTime timeout = DateTime.Now + LOAD_TIMEOUT;
        //    FileStream s = null;
        //    while (s == null && DateTime.Now <= timeout)
        //    {
        //        try
        //        {
        //            s = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.None);
        //        }
        //        catch (IOException)
        //        {
        //            return null;
        //        }
        //        catch (SecurityException)
        //        {
        //            // タイムアウトまでリトライ
        //            if (timeout < DateTime.Now)
        //            {
        //                throw;
        //            }
        //            Thread.Sleep(1);
        //        }
        //    }
        //    return s;
        //}
        public void Load()
        {
            LoadInternal();
            foreach (Type t in _connectionInfoClasses)
            {
                MethodInfo mi = t.GetMethod("GetKnownConnectionInfos", BindingFlags.Static | BindingFlags.Public);
                if (mi == null)
                {
                    continue;
                }
                ConnectionInfo[] infos = mi.Invoke(null, null) as ConnectionInfo[];
                if (infos == null)
                {
                    continue;
                }
                MergeByContent(_list, infos);
            }
            _backlist = new List<ConnectionInfo>(_list);
        }

        /// <summary>
        /// 接続情報の内容で比較して追加
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        private void MergeByContent(List<ConnectionInfo> destination, IEnumerable<ConnectionInfo> source)
        {
            List<ConnectionInfo> newList = new List<ConnectionInfo>();
            foreach (ConnectionInfo infoS in source)
            {
                bool found = false;
                foreach (ConnectionInfo infoD in destination)
                {
                    if (infoS.ContentEquals(infoD))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    newList.Add(infoS);
                }
            }
            destination.AddRange(newList);
        }

        private void Merge(List<ConnectionInfo> currentList, List<ConnectionInfo> newList, List<ConnectionInfo> oldList)
        {
            Dictionary<string, int> curDict = new Dictionary<string, int>();
            Dictionary<string, ConnectionInfo> oldDict = new Dictionary<string, ConnectionInfo>();
            for (int i = 0; i < currentList.Count; i++)
            {
                curDict.Add(currentList[i].Name, i);
            }
            foreach (ConnectionInfo info in oldList)
            {
                oldDict.Add(info.Name, info);
            }
            Dictionary<string, ConnectionInfo> newDict = new Dictionary<string, ConnectionInfo>();
            foreach (ConnectionInfo info in newList)
            {
                newDict.Add(info.Name, info);
            }

            foreach (ConnectionInfo info in newList)
            {
                if (!oldDict.ContainsKey(info.Name))
                {
                    if (!curDict.ContainsKey(info.Name))
                    {
                        //手元にないエントリがファイルにあった→追加
                        //(ただし読込時以降に手元で追加していない場合に限る)
                        currentList.Add(info);
                    }
                }
                else
                {
                    ConnectionInfo back = oldDict[info.Name];
                    if (!info.Equals(back))
                    {
                        int i;
                        if (curDict.TryGetValue(info.Name, out i) && 0 <= i && i < currentList.Count)
                        {
                            ConnectionInfo cur = currentList[i];
                            if (cur.Equals(back))
                            {
                                // 手元で変更していないエントリがファイル上では変更されていた→置換
                                // (手元の変更優先)
                                currentList[i] = info;
                            }
                        }
                    }
                }
            }
            foreach (ConnectionInfo back in oldList)
            {
                if (!newDict.ContainsKey(back.Name))
                {
                    int i;
                    if (curDict.TryGetValue(back.Name, out i) && 0 <= i && i < currentList.Count)
                    {
                        ConnectionInfo cur = currentList[i];
                        if (cur != null && cur.Equals(back))
                        {
                            //ファイル上にないエントリが手元にある&手元で変更していない→削除
                            currentList[i] = null;
                        }
                    }
                }
            }
            for (int i = currentList.Count - 1; 0 <= i; i--)
            {
                if (currentList[i] == null)
                {
                    currentList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// _listと接続情報一覧ファイルの内容をマージする
        /// </summary>
        /// <param name="stream"></param>
        private void Sync(FileStream stream)
        {
            string js = null;
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
            {
                js = sr.ReadToEnd();
            }
            List<ConnectionInfo> newList = LoadInternal();
            Merge(_list, newList, _backlist);
            _list.Sort(ConnectionInfo.CompareByName);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            //FileStream s = null;
            //if (File.Exists(Path))
            //{
            //    s = OpenStream(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            //    if (s == null)
            //    {
            //        return;
            //    }
            //    Sync(s);
            //    s.Seek(0, SeekOrigin.Begin);
            //    s.SetLength(0);
            //}
            //else
            //{
            //    s = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None);
            //}
            //using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8))
            //{
            //    sw.Write(JsonConvert.SerializeObject(_list));
            //}
        }
        public ConnectionInfo this[int index] { get { return _list[index]; } set { _list[index] = value; } }
        public ConnectionInfo this[string name]
        {
            get
            {
                foreach (ConnectionInfo info in _list)
                {
                    if (string.Compare(info.Name, name, true) == 0)
                    {
                        return info;
                    }
                }
                return null;
            }
        }

        #region ICollection<ConnectionInfo>の実装
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        ConnectionInfo IList<ConnectionInfo>.this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }

        public void Add(ConnectionInfo item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(ConnectionInfo item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(ConnectionInfo[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ConnectionInfo> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Remove(ConnectionInfo item)
        {
            return _list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(ConnectionInfo item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, ConnectionInfo item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }
    public class InvalidConnectionInfo: ConnectionInfo
    {
        private string _databaseType = "Unknown";
        private string _databaseDesc = "不明な種別";
        public InvalidConnectionInfo() { }
        public InvalidConnectionInfo(string databaseType)
        {
            _databaseType = string.Format("Unknown:{0}", databaseType);
            _databaseDesc = string.Format("不明な種別({0})", databaseType);
        }
        public override string DatabaseType
        {
            get
            {
                return _databaseType;
            }
        }

        public override string DatabaseDesc
        {
            get
            {
                return _databaseDesc;
            }
        }

        public override IDbConnection NewConnection()
        {
            return null;
        }

        public override Db2SourceContext NewDataSet()
        {
            return null;
        }

        public override string ToConnectionString()
        {
            return null;
        }
    }
}
