using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Db2Source
{
    public partial class Parameter : /*NamedObject,*/ IComparable, IDbTypeDef
    {
        public StoredFunction Owner { get; private set; }
        public int Index { get; internal set; }
        private string _name;
        public string Name
        {
            get { return _name; }
            internal set
            {
                if (_name == value)
                {
                    return;
                }
                _name = value;
                Invalidate();
            }
        }
        private ParameterDirection _direction;
        public ParameterDirection Direction
        {
            get { return _direction; }
            set
            {
                if (_direction == value)
                {
                    return;
                }
                _direction = value;
                Invalidate();
            }
        }
        private static readonly string[] ParameterDirectionStr = { null, null, "out", "inout", null, null, "result" };
        public static string GetParameterDirectionStr(ParameterDirection direction)
        {
            int i = (int)direction;
            if (i < 0 || ParameterDirectionStr.Length <= i)
            {
                return null;
            }
            return ParameterDirectionStr[i];
        }
        public virtual string DirectionStr
        {
            get
            {
                return GetParameterDirectionStr(Direction);
            }
        }
        public string BaseType { get; set; }
        public int? DataLength { get; set; } = null;
        public int? Precision { get; set; } = null;
        public bool? WithTimeZone { get; set; }
        public bool IsSupportedType { get; set; }

        public void UpdateDataType()
        {
            _dataType = DbTypeDefUtil.ToTypeText(this);
        }
        private string _dataType;
        public string DataType
        {
            get
            {
                return _dataType;
            }
            set
            {
                if (_dataType == value)
                {
                    return;
                }
                //PropertyChangedEventArgs e = new PropertyChangedEventArgs("DataType", value, _dataType);
                _dataType = value;
                Invalidate();
                //OnPropertyChanged(e);
            }
        }

        public Type ValueType { get; set; }
        public string DefaultValue { get; set; }
        public string StringFormat
        {
            get
            {
                Dictionary<string, PropertyInfo> dict = Owner?.Context.BaseTypeToProperty;
                if (dict == null)
                {
                    return null;
                }
                PropertyInfo prop;
                if (!dict.TryGetValue(BaseType, out prop))
                {
                    return null;
                }
                if (prop == null)
                {
                    return null;
                }
                return (string)prop.GetValue(null);
            }
        }

        internal IDbDataParameter _dbParameter;
        public IDbDataParameter DbParameter
        {
            get
            {
                Owner.RequireDbCommand();
                return _dbParameter;
            }
            internal set
            {
                _dbParameter = value;
            }
        }

        private void Invalidate()
        {
            Owner?.InvalidateName();
        }

        public Parameter(StoredFunction owner) : base()
        {
            Owner = owner;
            owner.Parameters.Add(this);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Parameter))
            {
                return -1;
            }
            Parameter p = (Parameter)obj;
            int ret = Owner.CompareTo(p.Owner);
            if (ret != 0)
            {
                return ret;
            }
            ret = Index - p.Index;
            if (ret != 0)
            {
                return ret;
            }
            //ret = string.Compare(Identifier, p.Identifier);
            ret = string.Compare(Name, p.Name);
            return ret;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Parameter))
            {
                return false;
            }
            Parameter p = (Parameter)obj;
            //return ((Owner == null && p.Owner == null) || (Owner != null && Owner.Equals(p.Owner))) && (Identifier == p.Identifier);
            return ((Owner == null && p.Owner == null) || (Owner != null && Owner.Equals(p.Owner))) && (Name == p.Name);
        }
        public override int GetHashCode()
        {
            //return ((Owner != null) ? Owner.GetHashCode() : 0) + Identifier.GetHashCode();
            return ((Owner != null) ? Owner.GetHashCode() : 0) + Name.GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
    }

    public interface IReturnType
    {
        string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine);
        string GetDefName();
    }

    public class SimpleReturnType : IReturnType
    {
        public string DataType { get; set; }

        public string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine)
        {
            return prefix + DataType;
        }

        public string GetDefName()
        {
            return DataType;
        }

        public SimpleReturnType() { }
        public SimpleReturnType(string dataType)
        {
            DataType = dataType;
        }
    }

    public partial class StoredFunction: SchemaObject/*, IDbTypeDef*/
    {
        public class ParameterCollection: IList<Parameter>, IList
        {
            private StoredFunction _owner;
            private List<Parameter> _items;
            private Dictionary<string, Parameter> _nameToItem = null;
            private object _nameToItemLock = new object();
            protected internal void OnParameterChanged(CollectionOperationEventArgs<Parameter> e)
            {
                InvalidateNameToItem();
                _owner.OnParameterChanged(e);
            }
            internal ParameterCollection(StoredFunction owner)
            {
                _owner = owner;
                _items = new List<Parameter>();
            }
            private void InvalidateNameToItem()
            {
                _nameToItem = null;
            }
            private void RequireNameToItem()
            {
                if (_nameToItem != null)
                {
                    return;
                }
                lock (_nameToItemLock)
                {
                    if (_nameToItem != null)
                    {
                        return;
                    }
                    Dictionary<string, Parameter> dict = new Dictionary<string, Parameter>();
                    foreach (Parameter p in _items)
                    {
                        if (string.IsNullOrEmpty(p.Name))
                        {
                            continue;
                        }
                        dict[p.Name.ToLower()] = p;
                    }
                    _nameToItem = dict;
                }
            }

            public Parameter this[string name]
            {
                get
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return null;
                    }
                    RequireNameToItem();
                    Parameter p;
                    if (!_nameToItem.TryGetValue(name.ToLower(), out p))
                    {
                        return null;
                    }
                    return p;
                }
            }

            public string GetInputDataTypeText(string prefix, string separator, string postfix)
            {
                bool needSeparator = false;
                StringBuilder buf = new StringBuilder();
                buf.Append(prefix);
                foreach (Parameter p in _items)
                {
                    if (p.Direction == ParameterDirection.Output)
                    {
                        continue;
                    }
                    if (needSeparator)
                    {
                        buf.Append(separator);
                    }
                    buf.Append(p.DataType);
                    needSeparator = true;
                }
                buf.Append(postfix);
                return buf.ToString();
            }

            public string GetDataTypeText(string prefix, string separator, string postfix)
            {
                if (Count == 0)
                {
                    return prefix + postfix;
                }
                StringBuilder buf = new StringBuilder();
                buf.Append(prefix);
                bool needSeparator = false;
                foreach (Parameter p in _items)
                {
                    if (needSeparator)
                    {
                        buf.Append(separator);
                    }
                    string s = p.DirectionStr;
                    if (!string.IsNullOrEmpty(s))
                    {
                        buf.Append(s);
                        buf.Append(' ');
                    }
                    buf.Append(p.DataType);
                    needSeparator = true;
                }
                buf.Append(postfix);
                return buf.ToString();
            }

            public string GetParamDefText(string prefix, string separator, string postfix)
            {
                if (Count == 0)
                {
                    return prefix + postfix;
                }
                StringBuilder buf = new StringBuilder();
                buf.Append(prefix);
                bool needSeparator = false;
                foreach (Parameter p in _items)
                {
                    if (needSeparator)
                    {
                        buf.Append(separator);
                    }
                    string s = p.DirectionStr;
                    if (!string.IsNullOrEmpty(s))
                    {
                        buf.Append(s);
                        buf.Append(' ');
                    }
                    buf.Append(p.Name);
                    buf.Append(' ');
                    buf.Append(p.DataType);
                    needSeparator = true;
                }
                buf.Append(postfix);
                return buf.ToString();
            }

            #region Interfaceの実装

            public Parameter this[int index]
            {
                get
                {
                    return _items[index];
                }

                set
                {
                    if (_items[index] == value)
                    {
                        return;
                    }
                    CollectionOperationEventArgs<Parameter> e = new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Update, index, value, _items[index]);
                    _items[index] = value;
                    OnParameterChanged(e);
                }
            }

            object IList.this[int index]
            {
                get
                {
                    return ((IList)_items)[index];
                }

                set
                {
                    if (((IList)_items)[index] == value)
                    {
                        return;
                    }
                    CollectionOperationEventArgs<Parameter> e = new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Update, index, (Parameter)value, _items[index]);
                    ((IList)_items)[index] = value;
                    OnParameterChanged(e);
                }
            }

            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public bool IsFixedSize
            {
                get
                {
                    return ((IList)_items).IsFixedSize;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((IList<Parameter>)_items).IsReadOnly;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((IList)_items).IsSynchronized;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return ((IList)_items).SyncRoot;
                }
            }

            int IList.Add(object value)
            {
                int ret = ((IList)_items).Add(value);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, (Parameter)value, null));
                return ret;
            }

            public void Add(Parameter item)
            {
                int ret = ((IList)_items).Add(item);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, item, null));
            }

            public void Clear()
            {
                _items.Clear();
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Clear, -1, null, null));
            }

            public bool Contains(object value)
            {
                return ((IList)_items).Contains(value);
            }

            public bool Contains(Parameter item)
            {
                return ((IList<Parameter>)_items).Contains(item);
            }

            public void CopyTo(Array array, int index)
            {
                ((IList)_items).CopyTo(array, index);
            }

            public void CopyTo(Parameter[] array, int arrayIndex)
            {
                ((IList<Parameter>)_items).CopyTo(array, arrayIndex);
            }

            public IEnumerator<Parameter> GetEnumerator()
            {
                return ((IList<Parameter>)_items).GetEnumerator();
            }

            public int IndexOf(object value)
            {
                return ((IList)_items).IndexOf(value);
            }

            public int IndexOf(Parameter item)
            {
                return ((IList<Parameter>)_items).IndexOf(item);
            }

            public void Insert(int index, object value)
            {
                ((IList)_items).Insert(index, value);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, (Parameter)value, null));
            }

            public void Insert(int index, Parameter item)
            {
                ((IList<Parameter>)_items).Insert(index, item);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, item, null));
            }

            public void Remove(object value)
            {
                ((IList)_items).Remove(value);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, (Parameter)value));
            }

            public bool Remove(Parameter item)
            {
                bool ret = _items.Remove(item);
                if (ret)
                {
                    OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, item));
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                Parameter old = _items[index];
                _items.RemoveAt(index);
                OnParameterChanged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, index, null, old));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<Parameter>)_items).GetEnumerator();
            }
            #endregion
        }

        private string _fullIdentifier;
        private string _identifier;
        //private string _nameExtension;
        private string _definition;
        private string _oldDefinition;

        public IReturnType ReturnType { get; set; }

        public bool HasOutputParameter()
        {
            foreach (Parameter p in Parameters)
            {
                if (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output)
                {
                    return true;
                }
            }
            return false;
        }

        public Type ValueType { get; set; }
        public ParameterCollection Parameters { get; private set; }
        public event EventHandler<CollectionOperationEventArgs<Parameter>> ParameterChanged;
        protected void OnParameterChanged(CollectionOperationEventArgs<Parameter> e)
        {
            InvalidateName();
            ParameterChanged?.Invoke(this, e);
        }
        public string Language { get; set; }
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                _definition = value;
                OnPropertyChanged("Definition");
            }
        }
        private string _headerDef = null;
        private string _displayName = null;

        internal void InvalidateName()
        {
            _fullIdentifier = null;
            _identifier = null;
            _displayName = null;
            _headerDef = null;
            InvalidateIdentifier();
        }

        private void UpdateIdentifier()
        {
            if (_identifier != null)
            {
                return;
            }
            string id = Name + Parameters.GetInputDataTypeText("(", ",", ")");
            _identifier = id;
            if (!string.IsNullOrEmpty(SchemaName))
            {
                _fullIdentifier = SchemaName + "." + _identifier;
            }
            else
            {
                _fullIdentifier = _identifier;
            }
        }

        private void UpdateHeaderDef()
        {
            if (_headerDef != null)
            {
                return;
            }
            _headerDef = Name + Parameters.GetParamDefText("(", ", ", ") return ") + ReturnType.GetDefName();
        }

        public virtual string HeaderDef
        {
            get
            {
                UpdateHeaderDef();
                return _headerDef;
            }
        }

        private void UpdateDisplayName()
        {
            if (_displayName != null)
            {
                return;
            }
            _displayName = Name + Parameters.GetDataTypeText("(", ",", ")");
        }
        public override string DisplayName
        {
            get
            {
                UpdateDisplayName();
                return _displayName;
            }
        }
        protected override string GetFullIdentifier()
        {
            UpdateIdentifier();
            return _fullIdentifier;
        }
        protected override string GetIdentifier()
        {
            UpdateIdentifier();
            return _identifier;
        }

        public override bool IsModified
        {
            get
            {
                return _definition != _oldDefinition;
            }
        }

        public override bool HasBackup()
        {
            return true;
        }

        public override void Backup(bool force)
        {
            _oldDefinition = _definition;
        }
        public override void Restore()
        {
            _definition = _oldDefinition;
        }
        public string[] ExtraInfo { get; set; }

        private string[] GetInputParamTypes()
        {
            List<string> l = new List<string>();
            foreach (Parameter p in Parameters)
            {
                if (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input)
                {
                    l.Add(p.DataType);
                }
            }
            return l.ToArray();
        }

        protected override Comment NewComment(string commentText)
        {
            return new StoredFunctionComment(Context, SchemaName, Name, GetInputParamTypes(), commentText, false);
        }

        private IDbCommand _dbCommand;
        internal void RequireDbCommand()
        {
            if (_dbCommand != null)
            {
                return;
            }
            _dbCommand = Context.GetSqlCommand(this, null, null, null);
            for (int i = 0; i < _dbCommand.Parameters.Count; i++)
            {
                IDbDataParameter pS = _dbCommand.Parameters[i] as IDbDataParameter;
                Parameter pD = Parameters[pS.ParameterName];
                if (pD != null)
                {
                    pD.DbParameter = pS;
                }
            }
        }
        public IDbCommand DbCommand
        {
            get
            {
                RequireDbCommand();
                return _dbCommand;
            }
        }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StoredFunction(Db2SourceContext context, string owner, string schema, string objectName, string definition, bool isLoaded) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects)
        {
            Parameters = new ParameterCollection(this);
            _definition = definition;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
}
