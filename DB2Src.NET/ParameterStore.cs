using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Db2Source
{
    public class DbTypeInfo
    {
        public DbType DbType { get; private set; }
        public string Name { get; private set; }
        private delegate object ParseDbType(string value);
        private delegate string DbTypeToString(object value);
        private ParseDbType _parse;
        private DbTypeToString _valueToString;
        private static object ParseString(string value)
        {
            return value;
        }
        private static object ParseBool(string value)
        {
            return bool.Parse(value);
        }
        private static object ParseSByte(string value)
        {
            return sbyte.Parse(value);
        }
        private static object ParseInt16(string value)
        {
            return short.Parse(value);
        }
        private static object ParseInt32(string value)
        {
            return int.Parse(value);
        }
        private static object ParseInt64(string value)
        {
            return long.Parse(value);
        }
        private static object ParseByte(string value)
        {
            return byte.Parse(value);
        }
        private static object ParseUInt16(string value)
        {
            return ushort.Parse(value);
        }
        private static object ParseUInt32(string value)
        {
            return uint.Parse(value);
        }
        private static object ParseUInt64(string value)
        {
            return ulong.Parse(value);
        }
        private static object ParseSingle(string value)
        {
            return float.Parse(value);
        }
        private static object ParseDouble(string value)
        {
            return double.Parse(value);
        }
        private static object ParseDecimal(string value)
        {
            return decimal.Parse(value);
        }
        private static object ParseDate(string value)
        {
            return DateTime.ParseExact(value, Db2SourceContext.ParseDateFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static object ParseDateTime(string value)
        {
            return DateTime.ParseExact(value, Db2SourceContext.ParseDateTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static string ObjectToString(object value)
        {
            return value?.ToString();
        }
        private static string DateToString(object value)
        {
            if (!(value is DateTime))
            {
                return null;
            }
            return ((DateTime)value).ToString(Db2SourceContext.DateFormat);
        }
        private static string DateTimeToString(object value)
        {
            if (!(value is DateTime))
            {
                return null;
            }
            return ((DateTime)value).ToString(Db2SourceContext.DateTimeFormat);
        }
        public object Parse(string value)
        {
            return _parse?.Invoke(value);
        }
        public string ValueToString(object value)
        {
            return _valueToString?.Invoke(value);
        }
        public static readonly DbTypeInfo[] DbTypeInfos = new DbTypeInfo[]
        {
            //new DbTypeInfo() {DbType = DbType.AnsiString, Name="AnsiString", _parse = ParseString, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Binary, Name="Binary", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Byte, Name="Byte", _parse = ParseByte, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Boolean, Name="Boolean", _parse = ParseBool, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Currency, Name="Currency", _parse = ParseInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Date, Name="日付", _parse = ParseDate, _valueToString = DateToString },
            new DbTypeInfo() {DbType = DbType.DateTime, Name="日付時刻", _parse = ParseDateTime, _valueToString = DateTimeToString },
            new DbTypeInfo() {DbType = DbType.Decimal, Name="実数(Decimal)", _parse = ParseDecimal, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Double, Name="実数(Double)", _parse = ParseDouble, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Guid, Name="Guid", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Int16, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Int32, Name="整数(32bit)", _parse = ParseInt32, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.Int64, Name="整数(64bit)", _parse = ParseInt64, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Object, Name="Object", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.SByte, Name="Sbyte", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Single, Name="Single", _parse = ParseSingle, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.String, Name="文字列", _parse = ParseString, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Time, Name="Time", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.UInt16, Name="UInt16", _parse = ParseUInt16, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.UInt32, Name="符号なし整数(32bit)", _parse = ParseUInt32, _valueToString = ObjectToString },
            new DbTypeInfo() {DbType = DbType.UInt64, Name="符号なし整数(64bit)", _parse = ParseUInt64, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.VarNumeric, Name="VarNumeric", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.AnsiStringFixedLength, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.StringFixedLength, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.Xml, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.DateTime2, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
            //new DbTypeInfo() {DbType = DbType.DateTimeOffset, Name="Int16", _parse = ParseInt16, _valueToString = ObjectToString },
        };
        private static Dictionary<DbType, DbTypeInfo> InitDbTypeToInto()
        {
            Dictionary<DbType, DbTypeInfo> ret = new Dictionary<DbType, DbTypeInfo>();
            foreach (DbTypeInfo info in DbTypeInfos)
            {
                ret.Add(info.DbType, info);
            }
            return ret;
        }
        public static readonly Dictionary<DbType, DbTypeInfo> DbTypeToDbTypeInfo = InitDbTypeToInto();
        private static Dictionary<string, DbTypeInfo> InitTypeNameToDbTypeInfo()
        {
            Dictionary<string, DbTypeInfo> ret = new Dictionary<string, DbTypeInfo>();
            foreach (DbTypeInfo info in DbTypeInfos)
            {
                ret.Add(info.DbType.ToString().ToLower(), info);
            }
            return ret;
        }
        public static readonly Dictionary<string, DbTypeInfo> TypeNameToDbTypeInfo = InitTypeNameToDbTypeInfo();
        public static DbTypeInfo GetDbTypeInfo(DbType type)
        {
            DbTypeInfo ret;
            if (!DbTypeToDbTypeInfo.TryGetValue(type, out ret))
            {
                return null;
            }
            return ret;
        }
        public static DbTypeInfo GetDbTypeInfo(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            DbTypeInfo ret;
            if (!TypeNameToDbTypeInfo.TryGetValue(typeName.ToLower(), out ret))
            {
                return null;
            }
            return ret;
        }
    }

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
    public class ParameterStore: DependencyObject, IDataParameter, ICloneable
    {
        public static readonly DependencyProperty DbTypeProperty = DependencyProperty.Register("DbType", typeof(DbType), typeof(ParameterStore));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(ParameterDirection), typeof(ParameterStore));
        public static readonly DependencyProperty ParameterNameProperty = DependencyProperty.Register("ParameterName", typeof(string), typeof(ParameterStore));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ParameterStore));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ParameterStore));
        public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register("IsError", typeof(bool), typeof(ParameterStore));

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

        private DbTypeInfo _dbTypeInfo;
        private bool _isNullable;
        private string _sourceColumn;
        private DataRowVersion _sourceVersion;
        private object _value;

        public DbType DbType
        {
            get { return (DbType)GetValue(DbTypeProperty); }
            set { SetValue(DbTypeProperty, value); }
        }

        private void OnDbTypePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateDbTypeInfo();
            DbParameter p = GetTarget();
            if (p != null)
            {
                p.DbType = DbType;
            }
        }

        private void InvalidateDbTypeInfo()
        {
            _dbTypeInfo = null;
        }
        private void RequireDbTypeInfo()
        {
            if (_dbTypeInfo != null)
            {
                return;
            }
            DbTypeInfo.DbTypeToDbTypeInfo.TryGetValue(DbType, out _dbTypeInfo);
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
            get { return (ParameterDirection)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        private void OnDirectionPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DbParameter p = GetTarget();
            if (p != null)
            {
                p.Direction = Direction;
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
            get { return (string)GetValue(ParameterNameProperty); }
            set { SetValue(ParameterNameProperty, value); }
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> ParameterNameChange;

        protected void OnParameterNamePropertyChange(DependencyPropertyChangedEventArgs e)
        {
            ParameterNameChange?.Invoke(this, e);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == DbTypeProperty)
            {
                OnDbTypePropertyChanged(e);
            }
            if (e.Property == DirectionProperty)
            {
                OnDirectionPropertyChanged(e);
            }
            if (e.Property == ParameterNameProperty)
            {
                OnParameterNamePropertyChange(e);
            }
            if (e.Property == TextProperty)
            {
                OnTextPropertyChange(e);
            }
            if (e.Property == ValueProperty)
            {
                OnValuePropertyChanged(e);
            }
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
            get { return GetValue(ValueProperty); }
            set
            {
                if (Value != value)
                {
                    SetValue(ValueProperty, value);
                }
                SetValue(TextProperty, DbTypeInfo?.ValueToString(value));
                SetIsError(false);
            }
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;
        private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DbParameter p = GetTarget();
            if (p != null)
            {
                p.Value = Value;
            }
            ValueChanged?.Invoke(this, e);
        }
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set
            {
                if (Text == value)
                {
                    return;
                }
                SetValue(TextProperty, value);
            }
        }
        public event EventHandler<DependencyPropertyChangedEventArgs> TextChanged;
        private void OnTextPropertyChange(DependencyPropertyChangedEventArgs e)
        {
            if (DbTypeInfo == null)
            {
                return;
            }
            try
            {
                object v = DbTypeInfo.Parse(Text);
                SetValue(ValueProperty, v);
                SetIsError(false);
                string txt = DbTypeInfo?.ValueToString(Value);
                if (Text != txt)
                {
                    SetValue(TextProperty, txt);
                }
            }
            catch
            {
                SetIsError(true);
            }
            TextChanged?.Invoke(this, e);
        }
        private void SetIsError(bool value)
        {
            SetValue(IsErrorProperty, value);
        }
        public bool IsError { get { return (bool)GetValue(IsErrorProperty); } }
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
            DbType = parameter.DbType;
            Direction = parameter.Direction;
            IsNullable = parameter.IsNullable;
            ParameterName = parameter.ParameterName;
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
            DbType = DbType.String;
            Direction = ParameterDirection.InputOutput;
            IsNullable = true;
            ParameterName = parameterName;
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
    }
}
