using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Db2Source
{
    public partial class ParameterStoreCollection : IList<ParameterStore>, IList
    {
        private List<ParameterStore> _list = new List<ParameterStore>();
        private Dictionary<string, ParameterStore> _nameDict = null;
        private object _nameDictLock = new object();

        private void RequireNameDict()
        {
            if (_nameDict != null)
            {
                return;
            }
            _nameDict = new Dictionary<string, ParameterStore>();
            foreach (ParameterStore obj in _list)
            {
                if (obj == null || string.IsNullOrEmpty(obj.ParameterName))
                {
                    continue;
                }
                _nameDict[obj.ParameterName] = obj;
            }
        }
        private void InvalidateNameDict()
        {
            lock (_nameDictLock)
            {
                _nameDict = null;
            }
        }
        private void ParameterStore_ParameterNameChange(object sender, DependencyPropertyChangedEventArgs e)
        {
            InvalidateNameDict();
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> ParameterTextChanged;
        private void ParameterStore_TextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ParameterTextChanged?.Invoke(sender, e);
        }
        public ParameterStore this[string name]
        {
            get
            {
                lock (_nameDictLock)
                {
                    RequireNameDict();
                    ParameterStore ret;
                    if (!_nameDict.TryGetValue(name, out ret))
                    {
                        return null;
                    }
                    return ret;
                }
            }
        }

        public ParameterStore this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
                InvalidateNameDict();
            }
        }

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
                return false;
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
                return _list[index];
            }

            set
            {
                if (value != null && !(value is ParameterStore))
                {
                    throw new ArgumentException("value");
                }
                _list[index] = value as ParameterStore;
            }
        }

        public void Add(ParameterStore item)
        {
            _list.Add(item);
            if (item != null)
            {
                item.ParameterNameChange += ParameterStore_ParameterNameChange;
                item.TextChanged += ParameterStore_TextChanged;
            }
            InvalidateNameDict();
        }

        public void Clear()
        {
            foreach (ParameterStore obj in _list)
            {
                obj.ParameterNameChange -= ParameterStore_ParameterNameChange;
                obj.TextChanged -= ParameterStore_TextChanged;
            }
            _list.Clear();
            InvalidateNameDict();
        }

        public bool Contains(ParameterStore item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(ParameterStore[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ParameterStore> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(ParameterStore item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, ParameterStore item)
        {
            _list.Insert(index, item);
            if (item != null)
            {
                item.ParameterNameChange += ParameterStore_ParameterNameChange;
            }
            InvalidateNameDict();
        }

        public bool Remove(ParameterStore item)
        {
            bool ret = _list.Remove(item);
            if (ret && item != null)
            {
                item.ParameterNameChange -= ParameterStore_ParameterNameChange;
            }
            InvalidateNameDict();
            return ret;
        }

        public void RemoveAt(int index)
        {
            ParameterStore obj = _list[index];
            _list.RemoveAt(index);
            obj.ParameterNameChange -= ParameterStore_ParameterNameChange;
            InvalidateNameDict();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int Add(object value)
        {
            if (value != null && !(value is ParameterStore))
            {
                throw new ArgumentException("value");
            }
            int ret = ((IList)_list).Add(value as ParameterStore);
            InvalidateNameDict();
            return ret;
        }

        public bool Contains(object value)
        {
            return _list.Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
            InvalidateNameDict();
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
            InvalidateNameDict();
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        private const string SELECT_QUERY_PARAMETER = "SELECT NAME, DB_TYPE, DIRECTION, VALUE FROM QUERY_PARAMETER";
        private const string UPSERT_QUERY_PARAMETER = "INSERT INTO QUERY_PARAMETER (NAME, DB_TYPE, DIRECTION, VALUE) VALUES (@NAME, @DB_TYPE, @DIRECTION, @VALUE)"
            + " ON CONFLICT(NAME) DO UPDATE SET DB_TYPE = @DB_TYPE, DIRECTION = @DIRECTION, VALUE = @VALUE";
        public void Save()
        {
            using (SQLiteConnection connection = SchemaObjectSetting.NewConnection())
            {
                foreach (var item in _list)
                {
                    SchemaObjectSetting.ExecuteScalar(connection, UPSERT_QUERY_PARAMETER,
                        new ParameterDef("NAME", DbType.String, item.ParameterName),
                        new ParameterDef("DB_TYPE", DbType.Int32, (int)item.DbType),
                        new ParameterDef("DIRECTION", DbType.Int32, (int)item.Direction),
                        new ParameterDef("VALUE", DbType.String, item.Value == null ? DBNull.Value : (object)item.Text));
                }
            }
        }
        public void Load()
        {
            using (SQLiteConnection connection = SchemaObjectSetting.NewConnection())
            {
                using (SQLiteDataReader reader = SchemaObjectSetting.ExecuteReader(connection, SELECT_QUERY_PARAMETER))
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        ParameterStore store = this[name];
                        if (store == null)
                        {
                            store = new ParameterStore(name);
                            Add(store);
                        }
                        store.DbType = (DbType)reader.GetInt32(1);
                        store.Direction = (ParameterDirection)reader.GetInt32(2);
                        if (reader.IsDBNull(3))
                        {
                            store.Value = null;
                        }
                        else
                        {
                            store.Text = reader.GetString(3);
                        }
                    }
                }

            }
        }

        public static ParameterStoreCollection AllParameters = new ParameterStoreCollection();

        public static ParameterStoreCollection GetParameterStores(IDbCommand command, ParameterStoreCollection stores, out bool modified)
        {
            modified = false;
            ParameterStoreCollection l = new ParameterStoreCollection();
            foreach (DbParameter p in command.Parameters)
            {
                ParameterStore ps = stores[p.ParameterName];
                ParameterStore psAll = ParameterStoreCollection.AllParameters[p.ParameterName];
                if (ps != null)
                {
                    ps = ps.Clone() as ParameterStore;
                    ps.Target = p;
                }
                else if (psAll != null)
                {
                    ps = psAll.Clone() as ParameterStore;
                    ps.Target = p;
                    modified = true;
                }
                else
                {
                    ps = new ParameterStore(p);
                    modified = true;
                }
                l.Add(ps);

                if (psAll != null)
                {
                    ps.CopyTo(psAll);
                }
                else
                {
                    AllParameters.Add(ps.Clone() as ParameterStore);
                }
            }
            return l;
        }

        public static ParameterStoreCollection GetParameterStores(string[] paramNames, ParameterStoreCollection stores, out bool modified)
        {
            modified = false;
            ParameterStoreCollection l = new ParameterStoreCollection();
            foreach (string param in paramNames)
            {
                ParameterStore ps = stores[param];
                ParameterStore psAll = AllParameters[param];
                if (ps != null)
                {
                    ps = ps.Clone() as ParameterStore;
                    ps.Target = null;
                }
                else if (psAll != null)
                {
                    ps = psAll.Clone() as ParameterStore;
                    ps.Target = null;
                    modified = true;
                }
                else
                {
                    ps = new ParameterStore(param);
                    modified = true;
                }
                l.Add(ps);

                if (psAll != null)
                {
                    ps.CopyTo(psAll);
                }
                else
                {
                    AllParameters.Add(ps.Clone() as ParameterStore);
                }
            }
            return l;
        }

        public static ParameterStoreCollection GetParameterStores(QueryHistory.Query query, ParameterStoreCollection stores, out bool modified)
        {
            modified = false;
            ParameterStoreCollection l = new ParameterStoreCollection();
            foreach (QueryHistory.Parameter p in query.Parameters)
            {
                ParameterStore ps = stores[p.Name];
                ParameterStore psAll = AllParameters[p.Name];
                if (ps != null)
                {
                    ps = ps.Clone() as ParameterStore;
                    ps.Target = null;
                    ps.Value = p.Value;
                }
                else if (psAll != null)
                {
                    ps = psAll.Clone() as ParameterStore;
                    ps.Target = null;
                    ps.Value = p.Value;
                    modified = true;
                }
                else
                {
                    ps = new ParameterStore(p);
                    modified = true;
                }
                l.Add(ps);

                if (psAll != null)
                {
                    ps.CopyTo(psAll);
                }
                else
                {
                    AllParameters.Add(ps.Clone() as ParameterStore);
                }
            }
            return l;
        }
    }
}
