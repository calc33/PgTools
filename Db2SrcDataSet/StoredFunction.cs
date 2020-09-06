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
        public string Name { get; internal set; }
        public ParameterDirection Direction { get; set; }
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

    public partial class StoredFunction: SchemaObject, IDbTypeDef
    {
        public class ParameterCollection: IList<Parameter>, IList
        {
            private StoredFunction _owner;
            private List<Parameter> _items;
            protected internal void OnParameterChantged(CollectionOperationEventArgs<Parameter> e)
            {
                _owner.OnParameterChantged(e);
            }
            internal ParameterCollection(StoredFunction owner)
            {
                _owner = owner;
                _items = new List<Parameter>();
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
                    OnParameterChantged(e);
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
                    OnParameterChantged(e);
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
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, (Parameter)value, null));
                return ret;
            }

            public void Add(Parameter item)
            {
                int ret = ((IList)_items).Add(item);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, ret, item, null));
            }

            public void Clear()
            {
                _items.Clear();
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Clear, -1, null, null));
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
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, (Parameter)value, null));
            }

            public void Insert(int index, Parameter item)
            {
                ((IList<Parameter>)_items).Insert(index, item);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Add, index, item, null));
            }

            public void Remove(object value)
            {
                ((IList)_items).Remove(value);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, (Parameter)value));
            }

            public bool Remove(Parameter item)
            {
                bool ret = _items.Remove(item);
                if (ret)
                {
                    OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, -1, null, item));
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                Parameter old = _items[index];
                _items.RemoveAt(index);
                OnParameterChantged(new CollectionOperationEventArgs<Parameter>("Parameters", CollectionOperation.Remove, index, null, old));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<Parameter>)_items).GetEnumerator();
            }
            #endregion
        }

        private string _internalName;
        private string _definition;
        private string _oldDefinition;
        public string BaseType { get; set; }
        public int? DataLength { get; set; }
        public int? Precision { get; set; }
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
                //OnPropertyChanged(e);
            }
        }

        public Type ValueType { get; set; }
        public ParameterCollection Parameters { get; private set; }
        public event EventHandler<CollectionOperationEventArgs<Parameter>> ParameterChantged;
        protected void OnParameterChantged(CollectionOperationEventArgs<Parameter> e)
        {
            ParameterChantged?.Invoke(this, e);
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
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }
        public virtual string HeaderDef
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(Name);
                buf.Append('(');
                bool needComma = false;
                foreach (Parameter p in Parameters)
                {
                    if (needComma)
                    {
                        buf.Append(',');
                    }
                    buf.Append(p.Name);
                    buf.Append(' ');
                    buf.Append(p.DataType);
                    needComma = true;
                }
                buf.Append(") return ");
                buf.Append(DataType);
                return buf.ToString();
            }
        }
        public override string DisplayName
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(Name);
                buf.Append('(');
                bool needComma = false;
                foreach (Parameter p in Parameters)
                {
                    if (needComma)
                    {
                        buf.Append(',');
                    }
                    string s = p.DirectionStr;
                    if (!string.IsNullOrEmpty(s))
                    {
                        buf.Append(s);
                        buf.Append(' ');
                    }
                    buf.Append(p.DataType);
                    needComma = true;
                }
                buf.Append(')');
                return buf.ToString();
            }
        }
        protected internal override void UpdateIdenfitier()
        {
            Identifier = _internalName;
        }
        public override bool IsModified()
        {
            return _definition != _oldDefinition;
        }
        public string[] ExtraInfo { get; set; }

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
                Parameters[i].DbParameter = _dbCommand.Parameters[i] as IDbDataParameter;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StoredFunction(Db2SourceContext context, string owner, string schema, string objectName, string internalName, string definition, bool isLoaded) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects)
        {
            Parameters = new ParameterCollection(this);
            _internalName = internalName;
            UpdateIdenfitier();
            _definition = definition;
            if (isLoaded)
            {
                _oldDefinition = _definition;
            }
        }
    }
}
