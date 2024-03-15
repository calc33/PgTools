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
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)bool.Parse(value);
        }
        private static object ParseSByte(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)sbyte.Parse(value);
        }
        private static object ParseInt16(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)short.Parse(value);
        }
        private static object ParseInt32(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)int.Parse(value);
        }
        private static object ParseInt64(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)long.Parse(value);
        }
        private static object ParseByte(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)byte.Parse(value);
        }
        private static object ParseUInt16(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)ushort.Parse(value);
        }
        private static object ParseUInt32(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)uint.Parse(value);
        }
        private static object ParseUInt64(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)ulong.Parse(value);
        }
        private static object ParseSingle(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)float.Parse(value);
        }
        private static object ParseDouble(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)double.Parse(value);
        }
        private static object ParseDecimal(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)decimal.Parse(value);
        }
        private static object ParseDate(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)DateTime.ParseExact(value, Db2SourceContext.ParseDateFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static object ParseDateTime(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)DateTime.ParseExact(value, Db2SourceContext.ParseDateTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        }
        private static string ObjectToString(object value)
        {
            return (value == null || value is DBNull) ? null : value.ToString();
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
        public static readonly DependencyProperty DbTypeProperty = DependencyProperty.Register("DbType", typeof(DbType), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnDbTypePropertyChanged)));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(ParameterDirection), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnDirectionPropertyChanged)));
        public static readonly DependencyProperty ParameterNameProperty = DependencyProperty.Register("ParameterName", typeof(string), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnParameterNamePropertyChanged)));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnValuePropertyChanged)));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnTextPropertyChanged)));
        public static readonly DependencyProperty IsNullProperty = DependencyProperty.Register("IsNull", typeof(bool), typeof(ParameterStore), new PropertyMetadata(new PropertyChangedCallback(OnIsNullPropertyChanged)));
        public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register("IsError", typeof(bool), typeof(ParameterStore));

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

        private static void OnDbTypePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnDbTypePropertyChanged(e);
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

        private static void OnDirectionPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnDirectionPropertyChanged(e);
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

        protected void OnParameterNamePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            ParameterNameChange?.Invoke(this, e);
        }

        private static void OnParameterNamePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnParameterNamePropertyChanged(e);
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
                SetValue(IsNullProperty, IsNullValue(value));
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

        private static void OnValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnValuePropertyChanged(e);
        }

        private static bool IsNullValue(object value)
        {
            return (value == null || value == DBNull.Value);
        }

        public bool IsNull
        {
            get { return (bool)GetValue(IsNullProperty); }
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> IsNullChanged;
        private void OnIsNullPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            IsNullChanged?.Invoke(this, e);
        }

        private static void OnIsNullPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnIsNullPropertyChanged(e);
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
        private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (DbTypeInfo == null)
            {
                return;
            }
            try
            {
                object v = DbTypeInfo.Parse(Text);
                if (Equals(GetValue(ValueProperty), v))
                {
                    return;
                }
                SetValue(ValueProperty, v);
                SetValue(IsNullProperty, IsNullValue(v));
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

        private static void OnTextPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ParameterStore)?.OnTextPropertyChanged(e);
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
                    p.Value = Value ?? DBNull.Value;
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
            _target = new WeakReference<DbParameter>(parameter);
            Value = parameter.Value;
            ParameterStoreCollection.AllParameters.Add(this);
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
            _target = null;
            Value = null;
            ParameterStoreCollection.AllParameters.Add(this);
        }

        public ParameterStore(QueryHistory.Parameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }
            DbType = parameter.DbType;
            Direction = ParameterDirection.InputOutput;
            IsNullable = true;
            ParameterName = parameter.Name;
            _sourceColumn = null;
            _sourceVersion = DataRowVersion.Current;
            _target = null;
            Value = parameter.Value;
            ParameterStoreCollection.AllParameters.Add(this);
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

        public override string ToString()
        {
            return string.Format("{0}: {1}", ParameterName, Value);
        }
    }
}
