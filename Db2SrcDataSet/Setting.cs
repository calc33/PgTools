using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class ValueText
    {
        public object Value { get; set; }
        public string Text { get; set; }
        public ValueText(object value, string text)
        {
            Value = value;
            Text = text;
        }
    }

    public class ValueText<T>: ValueText
    {
        public new T Value
        {
            get
            {
                return (T)base.Value;
            }
            set
            {
                base.Value = value;
            }
        }
        public ValueText(T value, string text) : base(value, text) { }
    }

    public class ValueTextCollection: IList<ValueText>, IList
    {
        private List<ValueText> _list = new List<ValueText>();
        private Dictionary<object, ValueText> _valueToValueText;
        private object _valueToValueTextLock = new object();
        private void InvalidateValueToValueText()
        {
            lock (_valueToValueTextLock)
            {
                _valueToValueText = null;
            }
        }
        public CultureInfo DefaultCulture { get; set; } = CultureInfo.CurrentCulture;
        private void UpdateValueToValueText()
        {
            if (_valueToValueText != null)
            {
                return;
            }
            lock (_valueToValueTextLock)
            {
                if (_valueToValueText != null)
                {
                    return;
                }
                Dictionary<object, ValueText> dict = new Dictionary<object, ValueText>();
                foreach (ValueText obj in _list)
                {
                    dict[obj.Value] = obj;
                }
                _valueToValueText = dict;
            }
        }

        public string GetText(object value)
        {
            UpdateValueToValueText();
            ValueText v;
            if (!_valueToValueText.TryGetValue(value, out v))
            {
                return null;
            }
            return v.Text;
        }
        public ValueText this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
                InvalidateValueToValueText();
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
                _list[index] = value as ValueText;
                InvalidateValueToValueText();
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
                return ((IList<ValueText>)_list).IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)_list).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)_list).SyncRoot;
            }
        }

        public void Add(ValueText item)
        {
            _list.Add(item);
            InvalidateValueToValueText();
        }

        public void Clear()
        {
            _list.Clear();
            InvalidateValueToValueText();
        }

        public bool Contains(ValueText item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(ValueText[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ValueText> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(ValueText item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, ValueText item)
        {
            _list.Insert(index, item);
            InvalidateValueToValueText();
        }

        public bool Remove(ValueText item)
        {
            bool ret = _list.Remove(item);
            if (ret)
            {
                InvalidateValueToValueText();
            }
            return ret;
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            InvalidateValueToValueText();
        }

        int IList.Add(object value)
        {
            if (value != null && !(value is ValueText))
            {
                throw new ArgumentException("valueがValueText型ではありません");
            }
            int ret = ((IList)_list).Add(value);
            InvalidateValueToValueText();
            return ret;
        }

        bool IList.Contains(object value)
        {
            if (value != null && !(value is ValueText))
            {
                return false;
            }
            return _list.Contains(value as ValueText);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
            InvalidateValueToValueText();
        }

        void IList.Remove(object value)
        {
            ((IList)_list).Remove(value);
            InvalidateValueToValueText();
        }
    }

    public class ValueTextCollection<T>: ValueTextCollection, IList<ValueText<T>>
    {
        public new ValueText<T> this[int index]
        {
            get
            {
                return (ValueText<T>)base[index];
            }

            set
            {
                base[index] = value;
            }
        }

        public void Add(ValueText<T> item)
        {
            base.Add(item);
        }

        public bool Contains(ValueText<T> item)
        {
            return base.Contains(item);
        }

        public void CopyTo(ValueText<T>[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public int IndexOf(ValueText<T> item)
        {
            return base.IndexOf(item);
        }

        public void Insert(int index, ValueText<T> item)
        {
            base.Insert(index, item);
        }

        public bool Remove(ValueText<T> item)
        {
            return base.Remove(item);
        }

        IEnumerator<ValueText<T>> IEnumerable<ValueText<T>>.GetEnumerator()
        {
            return GetEnumerator() as IEnumerator<ValueText<T>>;
        }
    }
    public interface ISetting
    {
        string Name { get; }
        string Description { get; }
        object Value { get; set; }
        bool IsLookup { get; }
        ValueTextCollection DisplayTexts { get; }
    }
    public interface ISetting<T>
    {
        string Name { get; }
        string Description { get; }
        T Value { get; set; }
        bool IsLookup { get; }
        ValueTextCollection<T> DisplayTexts { get; }
    }

    public class Setting<T>: ISetting<T>, ISetting
    {
        public string Name { get; internal set; }
        public string Description { get; set; }
        public T Value { get; set; }
        public bool IsLookup { get; internal set; }
        public ValueTextCollection<T> DisplayTexts { get; } = new ValueTextCollection<T>();
        ValueTextCollection ISetting.DisplayTexts
        {
            get
            {
                return DisplayTexts;
            }
        }

        object ISetting.Value
        {
            get
            {
                return Value;
            }
            set
            {
                if (value != null && !(value is T))
                {
                    throw new ArgumentException(string.Format("valueが{0}型ではありません", typeof(T).FullName));
                }
                Value = (T)value;
            }
        }
    }
    public class RedirectedPropertySetting<T>: ISetting<T>, ISetting
    {
        public string Name { get; internal set; }
        public string Description { get; set; }
        public object Target { get; internal set; }
        private PropertyInfo _property;
        public PropertyInfo Property
        {
            get
            {
                return _property;
            }
            internal set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Property");
                }
                Type t = value.PropertyType;
                if (t != typeof(T) && !t.IsSubclassOf(typeof(T)))
                {
                    throw new ArgumentException("Propertyの型が一致しません");
                }
                _property = value;
            }
        }
        public T Value
        {
            get
            {
                return (T)Property.GetValue(Target);
            }
            set
            {
                Property.SetValue(Target, value);
            }
        }
        object ISetting.Value
        {
            get
            {
                return Value;
            }
            set
            {
                if (value != null && !(value is T))
                {
                    throw new ArgumentException(string.Format("valueが{0}型ではありません", typeof(T).FullName));
                }
                Value = (T)value;
            }
        }
        public bool IsLookup { get; internal set; }
        public ValueTextCollection<T> DisplayTexts { get; } = new ValueTextCollection<T>();
        ValueTextCollection ISetting.DisplayTexts
        {
            get
            {
                return DisplayTexts;
            }
        }
        internal RedirectedPropertySetting(string name, string description, object target, PropertyInfo property)
        {
            Name = name;
            Description = description;
            Target = target;
            Property = property;
            IsLookup = false;
        }
        internal RedirectedPropertySetting(string name, string description, object target, PropertyInfo property, KeyValuePair<T,string>[] displayTexts)
        {
            Name = name;
            Description = description;
            Target = target;
            Property = property;
            IsLookup = true;
            foreach (KeyValuePair<T,string> kv in displayTexts)
            {
                DisplayTexts.Add(new ValueText(kv.Key, kv.Value));
            }
        }
        internal RedirectedPropertySetting(string propertyName, string description, Type type)
        {
            Name = propertyName;
            Description = description;
            Target = null;
            Property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            IsLookup = false;
        }
        internal RedirectedPropertySetting(string propertyName, string description, object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            Name = propertyName;
            Description = description;
            Target = target;
            Property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            IsLookup = false;
        }
        internal RedirectedPropertySetting(string propertyName, string description, object target, KeyValuePair<T, string>[] displayTexts)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            Name = propertyName;
            Description = description;
            Target = target;
            Property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            IsLookup = true;
            foreach (KeyValuePair<T, string> kv in displayTexts)
            {
                DisplayTexts.Add(new ValueText(kv.Key, kv.Value));
            }
        }
    }

    public class SettingCollection: IList<ISetting>, IList
    {
        private List<ISetting> _list = new List<ISetting>();

        public ISetting this[int index]
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

        public void Add(ISetting item)
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

        public bool Contains(ISetting item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public void CopyTo(ISetting[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ISetting> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public int IndexOf(ISetting item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        public void Insert(int index, ISetting item)
        {
            _list.Insert(index, item);
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
        }

        public bool Remove(ISetting item)
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
}
