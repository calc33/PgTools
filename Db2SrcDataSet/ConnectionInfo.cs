using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly List<ExtraCommand> _list = new List<ExtraCommand>();

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
    public abstract partial class ConnectionInfo: IComparable, INotifyPropertyChanged
    {
        public static readonly char CategoryPathSeparatorChar = Path.DirectorySeparatorChar;
        private static readonly Dictionary<string, Type> _databaseTypeToType = new Dictionary<string, Type>();
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

        private string _name;
        [JsonIgnore]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name == value)
                {
                    return;
                }
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        [JsonIgnore]
        public virtual string Description
        {
            get
            {
                return string.Format("{0}:{1}", DatabaseType, GetDefaultName());
            }
        }

        public virtual string GetDatabaseIdentifier()
        {
            return ServerName;
        }
        public virtual string GetDefaultName()
        {
            return string.Format("{0}@{1}", UserName, GetDatabaseIdentifier());
        }

        public virtual string GetTreeNodeHeader()
        {
            return GetDefaultName();
        }

        /// <summary>
        /// データベースの種別を識別する文字列
        /// </summary>
        [JsonIgnore]
        public abstract string DatabaseType { get; }
        /// <summary>
        /// 画面に表示する際のデータベース名称
        /// </summary>
        [JsonIgnore]
        public abstract string DatabaseDesc { get; }

        private string _serverName;
        private string _userName;
        private bool _isPasswordHidden;
        [InputField("サーバー", 10)]
        public string ServerName
        {
            get
            {
                return _serverName;
            }
            set
            {
                if (_serverName == value)
                {
                    return;
                }
                _serverName = value;
                OnPropertyChanged("ServerName");
                KeyPropertyChanged();
            }
        }
        [InputField("ユーザー", 20)]
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (_userName == value)
                {
                    return;
                }
                _userName = value;
                OnPropertyChanged("UserName");
                KeyPropertyChanged();
            }
        }

        [InputField("パスワード", 30, true)]
        [JsonIgnore]
        public string Password { get; set; }
        [JsonPropertyName("Password")]
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
        public bool IsPasswordHidden
        {
            get
            {
                return _isPasswordHidden;
            }
            set
            {
                if (_isPasswordHidden == value)
                {
                    return;
                }
                _isPasswordHidden = value;
                OnPropertyChanged("IsPasswordHidden");
            }
        }

        [JsonIgnore]
        public ExtraCommandCollecion ExtraCommands { get; } = new ExtraCommandCollecion();

        private RGB? _backgroundColor;

        protected virtual RGB GetDefaultBackgroundColor()
        {
            // 接続情報のHash値から色を決定
            int hash = GetHashCode();
            uint h = 0;
            foreach (byte b in BitConverter.GetBytes(hash))
            {
                h *= 17;
                h += b;
            }
            while (256 <= h)
            {
                h = h / 256 + h % 256;
            }
            return ColorConverter.FromHSV(h / 256f * 360f, 0.3f, 0.9f);
        }

        [JsonIgnore]
        public RGB BackgroundColor
        {
            get
            {
                if (_backgroundColor.HasValue)
                {
                    return _backgroundColor.Value;
                }
                //UpdateDefaultBackgroundColor();
                return GetDefaultBackgroundColor();
            }
            set
            {
                if (BackgroundColor.Equals(value))
                {
                    return;
                }
                _backgroundColor = value;
                OnPropertyChanged("BackgroundColor");
                OnPropertyChanged("BkColor");
                OnPropertyChanged("UseDefaultBackgroundColor");
            }
        }

        public uint? BkColor
        {
            get
            {
                return _backgroundColor?.ColorCode;
            }
            set
            {
                if (!value.HasValue)
                {
                    _backgroundColor = null;
                    return;
                }
                BackgroundColor = new RGB(value.Value);
            }
        }

        [JsonIgnore]
        public bool UseDefaultBackgroundColor
        {
            get { return !_backgroundColor.HasValue; }
            set
            {
                if (UseDefaultBackgroundColor == value)
                {
                    return;
                }
                if (!value)
                {
                    _backgroundColor = GetDefaultBackgroundColor();
                }
                else
                {
                    _backgroundColor = null;
                }
            }
        }

        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
        public bool? IsWindowMaximized { get; set; }
        public string WorkingDirectory { get; set; }
        [JsonIgnore]
        public DateTime LastConnected { get; set; } = DateTime.FromOADate(0);
        [JsonPropertyName("LastConnected")]
        [ReadOnly(true)]
        [DefaultValue(0)]
        public double LastConnectedSerial { get { return LastConnected.ToOADate(); } set { LastConnected = DateTime.FromOADate(value); } }
        public bool IsLastConnectionFailed { get; set; }
        [ReadOnly(true)]
        [DefaultValue(0)]
        public long ConnectionCount { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void OnDefaultCategoryPathChanged()
        {
            if (!_isDefaultCategory)
            {
                return;
            }
            string v = GetDefaultCategoryPath();
            if (_categoryPath == v)
            {
                return;
            }
            _categoryPath = v;
            OnPropertyChanged("CategoryPath");
        }

        private bool _isDefaultCategory = true;
        private string _categoryPath;

        [InputField("グループ", 100)]
        public string CategoryPath
        {
            get
            {
                if (_isDefaultCategory)
                {
                    return GetDefaultCategoryPath();
                }
                return _categoryPath;
            }
            set
            {
                string v = CategoryPath;
                if (v == value)
                {
                    return;
                }
                if (value == null)
                {
                    _isDefaultCategory = true;
                    return;
                }
                if (GetDefaultCategoryPath() == value)
                {
                    _isDefaultCategory = true;
                    return;
                }
                _categoryPath = value;
                _isDefaultCategory = false;
                OnPropertyChanged("CategoryPath");
            }
        }

        public string[] GetSeparetedCategoryPath()
        {
            string v = CategoryPath;
            if (string.IsNullOrEmpty(v))
            {
                return StrUtil.EmptyStringArray;
            }
            return v.Split(CategoryPathSeparatorChar);
        }

        public abstract string GetDefaultCategoryPath();

        public virtual void Merge(ConnectionInfo item)
        {
            if (!string.IsNullOrEmpty(item.Password))
            {
                Password = item.Password;
            }
            WindowLeft = item.WindowLeft;
            WindowTop = item.WindowTop;
            WindowWidth = item.WindowWidth;
            WindowHeight = item.WindowHeight;
            IsWindowMaximized = item.IsWindowMaximized;
            if (LastConnected < item.LastConnected)
            {
                LastConnected = item.LastConnected;
            }
            ConnectionCount += item.ConnectionCount;
        }

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
        protected static int GetStrHash(params string[] value)
        {
            if (value == null || value.Length == 0)
            {
                return 0;
            }
            int h = 0;
            foreach (string s in value)
            {
                h *= 13;
                if (!string.IsNullOrEmpty(s))
                {
                    h += s.GetHashCode();
                }
            }
            return h;
        }
        public override int GetHashCode()
        {
            return GetStrHash(Name, DatabaseType, ServerName, UserName);
            //+ Password.GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
        public abstract string ToConnectionString(bool includePassord);
        public abstract IDbConnection NewConnection(bool withOpening);
        public async Task<IDbConnection> NewConnectionAsync(bool withOpening)
        {
            return await Task.Run(() => { return NewConnection(withOpening); });
        }
        public static int CompareByName(ConnectionInfo item1, ConnectionInfo item2)
        {
            if (item1 == null || item2 == null)
            {
                return (item1 == null ? 1 : 0) - (item2 == null ? 1 : 0);
            }
            return string.Compare(item1.Name, item2.Name, true, CultureInfo.CurrentCulture);
        }

        public static int CompareByCategory(ConnectionInfo x, ConnectionInfo y)
        {
            int ret = string.Compare(x.CategoryPath, y.CategoryPath);
            if (ret != 0)
            {
                return ret;
            }
            ret = CompareByName(x, y);
            return ret;
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

    public sealed partial class ConnectionList: IList<ConnectionInfo>, IList
    {
        public class ConnectionInfoClassCollection: IReadOnlyList<Type>
        {
            private readonly List<Type> _list = new List<Type>();

            public void Add(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("connectionInfoType");
                }
                if (!type.IsSubclassOf(typeof(ConnectionInfo)))
                {
                    throw new ArgumentException(string.Format("ConnectionInfo型を継承していないクラスです: {0}", type.FullName));
                }
                _list.Add(type);
            }
            public bool Remove(Type type)
            {
                return _list.Remove(type);
            }

            #region IReadOnlyList
            public Type this[int index]
            {
                get
                {
                    return _list[index];
                }
            }

            public int Count
            {
                get
                {
                    return _list.Count;
                }
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _list.GetEnumerator();
            }
            #endregion
        }
        private static string InitDefaultConnectionListPath()
        {
            string path = System.IO.Path.Combine(Db2SourceContext.AppDataDir, "ConnectionList.db");
            return path;
        }
        public static readonly string DefaultConnectionListPath = InitDefaultConnectionListPath();
        public static readonly ConnectionInfoClassCollection ConnectionInfoTypes = new ConnectionInfoClassCollection();
        public static void Register(Type connectionInfoType)
        {
            ConnectionInfoTypes.Add(connectionInfoType);
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

        public void Load()
        {
            _list = LoadInternal();
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
            if (newList == null)
            {
                return;
            }
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
        public ConnectionInfo Merge(ConnectionInfo item)
        {
            int p = IndexOf(item);
            if (p != -1)
            {
                return this[p];
            }
            for (int i = 0; i < _list.Count; i++)
            {
                ConnectionInfo info = _list[i];
                if (info.ContentEquals(item))
                {
                    info.Merge(item);
                    return info;
                }
            }
            _list.Add(item);
            return item;
        }
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            SaveInternal();
        }
        public void Save(ConnectionInfo info, bool? connected)
        {
            if (connected.HasValue)
            {
                info.IsLastConnectionFailed = !connected.Value;
            }
            SaveInternal(info);
        }

        public bool FillPassword(ConnectionInfo info)
        {
            if (info == null)
            {
                return false;
            }
            foreach (ConnectionInfo c in _list)
            {
                if (info.ContentEquals(c))
                {
                    info.Password = c.Password;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 引数infoと同じ内容の登録済み設定を返す
        /// 見つからない場合はinfoを返す
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public ConnectionInfo Find(ConnectionInfo info)
        {
            if (info == null)
            {
                return null;
            }
            foreach (ConnectionInfo item in _list)
            {
                if (item.ContentEquals(info))
                {
                    return item;
                }
            }
            return info;
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

        public bool IsFixedSize
        {
            get
            {
                return ((IList)_list).IsFixedSize;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
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

        public int Add(object value)
        {
            return ((IList)_list).Add(value);
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }
        #endregion
    }
    public class InvalidConnectionInfo: ConnectionInfo
    {
        private readonly string _databaseType = "Unknown";
        private readonly string _databaseDesc = "不明な種別";
        public InvalidConnectionInfo() { }
        public InvalidConnectionInfo(string databaseType)
        {
            _databaseType = string.Format("Unknown:{0}", databaseType);
            _databaseDesc = string.Format("不明な種別({0})", databaseType);
        }
        [JsonIgnore]
        public override string DatabaseType
        {
            get
            {
                return _databaseType;
            }
        }

        [JsonIgnore]
        public override string DatabaseDesc
        {
            get
            {
                return _databaseDesc;
            }
        }

        public override IDbConnection NewConnection(bool withOpening)
        {
            return null;
        }

        public override Db2SourceContext NewDataSet()
        {
            return null;
        }

        public override string ToConnectionString(bool includePassord)
        {
            return null;
        }

        public override string GetDefaultCategoryPath()
        {
            return string.Empty;
        }
    }
}
