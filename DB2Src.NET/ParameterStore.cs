using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Db2Source
{
    public class ParameterNameChangeEventArgs: EventArgs
    {
        public string OldValue { get; private set; }
        public string NewValue { get; set; }
        internal ParameterNameChangeEventArgs(string oldValue, string newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    public class ParameterStore: IDataParameter, ICloneable
    {
        public static ParameterStoreCollection AllParameters = new ParameterStoreCollection();
        public static ParameterStoreCollection GetParameterStores(IDbCommand command, ParameterStoreCollection stores, out bool modified)
        {
            modified = false;
            ParameterStoreCollection l = new ParameterStoreCollection();
            foreach (DbParameter p in command.Parameters)
            {
                ParameterStore ps = stores[p.ParameterName];
                ParameterStore psAll = AllParameters[p.ParameterName];
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

        private WeakReference<DbParameter> _target = null;
        private DbParameter GetTarget()
        {
            DbParameter p;
            if (_target == null || !_target.TryGetTarget(out p))
            {
                return null;
            }
            return p;
        }

        private DbType _dbType;
        private DbTypeInfo _dbTypeInfo;
        private ParameterDirection _direction;
        private bool _isNullable;
        private string _parameterName;
        private string _sourceColumn;
        private DataRowVersion _sourceVersion;
        private object _value;
        public DbType DbType
        {
            get { return _dbType; }
            set
            {
                _dbType = value;
                _dbTypeInfo = null;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.DbType = _dbType;
                }
            }
        }
        private void RequireDbTypeInfo()
        {
            if (_dbTypeInfo != null)
            {
                return;
            }
            DbTypeInfo.DbTypeToDbTypeInfo.TryGetValue(_dbType, out _dbTypeInfo);
        }
        public DbTypeInfo DbTypeInfo
        {
            get
            {
                RequireDbTypeInfo();
                return _dbTypeInfo;
            }
        }
        public ParameterDirection Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.Direction = _direction;
                }
            }
        }
        public bool IsNullable
        {
            get { return _isNullable; }
            set
            {
                _isNullable = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.IsNullable = _isNullable;
                }
            }
        }

        public string ParameterName
        {
            get { return _parameterName; }
            set
            {
                if (_parameterName == value)
                {
                    return;
                }
                string oldName = _parameterName;
                _parameterName = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(value) && p.ParameterName != value)
                    {
                        _target.SetTarget(null);
                    }
                }
                OnParameterNameChange(new ParameterNameChangeEventArgs(oldName, _parameterName));
            }
        }
        public event EventHandler<ParameterNameChangeEventArgs> ParameterNameChange;
        protected void OnParameterNameChange(ParameterNameChangeEventArgs e)
        {
            ParameterNameChange?.Invoke(this, e);
        }
        public string SourceColumn
        {
            get { return _sourceColumn; }
            set
            {
                _sourceColumn = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.SourceColumn = _sourceColumn;
                }
            }
        }
        public DataRowVersion SourceVersion
        {
            get { return _sourceVersion; }
            set
            {
                _sourceVersion = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.SourceVersion = _sourceVersion;
                }
            }
        }

        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.Value = _value;
                }
            }
        }
        public string Text
        {
            get
            {
                return DbTypeInfo?.ValueToString(Value);
            }
            set
            {
                if (DbTypeInfo == null)
                {
                    return;
                }
                Value = DbTypeInfo.Parse(value);
            }
        }
        public DbParameter Target
        {
            get
            {
                return GetTarget();
            }
            set
            {
                if (value != null && !string.IsNullOrEmpty(ParameterName) && ParameterName != value.ParameterName)
                {
                    throw new ArgumentException("ParameterNameが異なるパラメータをTargetにセットすることはできません");
                }
                _target = new WeakReference<DbParameter>(value);
                DbParameter p = GetTarget();
                if (p != null)
                {
                    p.DbType = DbType;
                    p.Direction = Direction;
                    p.IsNullable = IsNullable;
                    p.SourceColumn = SourceColumn;
                    p.SourceVersion = SourceVersion;
                    p.Value = Value;
                }
            }
        }

        public ParameterStore(DbParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }
            _dbType = parameter.DbType;
            _direction = parameter.Direction;
            _isNullable = parameter.IsNullable;
            _parameterName = parameter.ParameterName;
            _sourceColumn = parameter.SourceColumn;
            _sourceVersion = parameter.SourceVersion;
            _value = parameter.Value;
            _target = new WeakReference<DbParameter>(parameter);
            AllParameters.Add(this);
        }
        public ParameterStore(string parameterName)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException("parameterName");
            }
            _dbType = DbType.String;
            _direction = ParameterDirection.InputOutput;
            _isNullable = true;
            _parameterName = parameterName;
            _sourceColumn = null;
            _sourceVersion = DataRowVersion.Current;
            _value = null;
            _target = null;
            AllParameters.Add(this);
        }

        public object Clone()
        {
            ParameterStore obj = MemberwiseClone() as ParameterStore;
            obj._target = null;
            return obj;
        }
        public void CopyTo(ParameterStore destination)
        {
            if (destination == null)
            {
                return;
            }
            destination.DbType = DbType;
            destination.Direction = Direction;
            destination.IsNullable = IsNullable;
            destination.ParameterName = ParameterName;
            destination.SourceColumn = SourceColumn;
            destination.SourceVersion = SourceVersion;
            destination.Value = Value;
        }
    }
    public class ParameterStoreCollection: IList<ParameterStore>, IList
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
        private void ParameterStore_ParameterNameChange(object sender, ParameterNameChangeEventArgs e)
        {
            InvalidateNameDict();
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
            }
            InvalidateNameDict();
        }

        public void Clear()
        {
            foreach (ParameterStore obj in _list)
            {
                obj.ParameterNameChange -= ParameterStore_ParameterNameChange;
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
    }
}
