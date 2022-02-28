using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace Db2Source
{
    public class Axis
    {
        private sealed class NoAxisValue : IComparable
        {
            public static NoAxisValue Instance = new NoAxisValue();
            /// <summary>
            /// あらゆるデータより後ろとして扱う
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int CompareTo(object obj)
            {
                if (obj is NoAxisValue)
                {
                    return 0;
                }
                return 1;
            }

            /// <summary>
            /// NoAxisValueインスタンスはすべて同一とみなす
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return (obj is NoAxisValue);
            }

            /// <summary>
            /// NoAxisValueインスタンスはすべて同一とみなす
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return 0;
            }
        }

        public class AxisValueArray : IList<AxisValue>
        {
            private AxisValue[] _array;

            public AxisValue this[int index]
            {
                get { return _array[index]; }
                set { _array[index] = value; }
            }

            public int Count { get { return _array.Length; } }

            public bool IsReadOnly { get { return false; } }

            void ICollection<AxisValue>.Add(AxisValue item) { }

            void ICollection<AxisValue>.Clear() { }

            public bool Contains(AxisValue item)
            {
                return _array.Contains(item);
            }

            public void CopyTo(AxisValue[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }

            public IEnumerator<AxisValue> GetEnumerator()
            {
                return ((IEnumerable<AxisValue>)_array).GetEnumerator();
            }

            public int IndexOf(AxisValue item)
            {
                return ((IList<AxisValue>)_array).IndexOf(item);
            }

            void IList<AxisValue>.Insert(int index, AxisValue item) { }

            bool ICollection<AxisValue>.Remove(AxisValue item)
            {
                return false;
            }

            void IList<AxisValue>.RemoveAt(int index) { }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _array.GetEnumerator();
            }

            internal AxisValueArray(IList<Axis> axises)
            {
                if (axises == null)
                {
                    throw new ArgumentNullException("axises");
                }
                _array = new AxisValue[axises.Count];
                for (int i = 0; i < axises.Count; i++)
                {
                    _array[i] = axises[i].NoValue;
                }
            }

            public AxisValueArray(object target, Axis[] axises)
            {
                if (target == null)
                {
                    throw new ArgumentNullException("target");
                }
                if (axises == null)
                {
                    throw new ArgumentNullException("axises");
                }
                _array = new AxisValue[axises.Length];
                for (int i = 0; i < _array.Length; i++)
                {
                    _array[i] = axises[i].Require(target);
                }
            }
            internal AxisValueArray(AxisValueArray source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                _array = new AxisValue[source.Count];
                source.CopyTo(_array, 0);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is AxisValueArray))
                {
                    return false;
                }
                AxisValueArray a = (AxisValueArray)obj;
                if (Count != a.Count)
                {
                    return false;
                }
                for (int i = 0; i < _array.Length; i++)
                {
                    if (!Equals(this[i], a[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                int hash = 0;
                foreach (AxisValue v in _array)
                {
                    hash = hash * 17 + v.GetHashCode();
                }
                return hash;
            }
        }

        public class AxisValueCollection : IList<AxisValue>
        {
            private static readonly object NullKey = new object();
            private List<AxisValue> _list = new List<AxisValue>();
            private Dictionary<object, AxisValue> _valueToAxis = null;
            private object _valueToAxisLock = new object();

            public AxisValue this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = value; }
            }

            public AxisValue this[object value]
            {
                get
                {
                    UpdateValueToAxis();
                    object v = value ?? NullKey;
                    AxisValue ret;
                    if (_valueToAxis.TryGetValue(v, out ret))
                    {
                        return ret;
                    }
                    return null;
                }
            }

            public AxisValue Require(Axis owner, object record)
            {
                object v = owner.GetValue(record);
                AxisValue ret = this[v];
                if (ret != null)
                {
                    return ret;
                }
                ret = new AxisValue(owner, v);
                _list.Add(ret);
                _valueToAxis[v ?? NullKey] = ret;
                return ret;
            }

            public int Count { get { return _list.Count; } }

            public bool IsReadOnly { get { return false; } }

            public void Add(AxisValue item)
            {
                _list.Add(item);
                InvalidateValueToAxis();
            }

            public void Clear()
            {
                _list.Clear();
                InvalidateValueToAxis();
            }

            public bool Contains(AxisValue item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(AxisValue[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<AxisValue> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            public int IndexOf(AxisValue item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, AxisValue item)
            {
                _list.Insert(index, item);
                InvalidateValueToAxis();
            }

            public bool Remove(AxisValue item)
            {
                bool ret = _list.Remove(item);
                if (ret)
                {
                    InvalidateValueToAxis();
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                InvalidateValueToAxis();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_list).GetEnumerator();
            }

            private void InvalidateValueToAxis()
            {
                _valueToAxis = null;
            }
            private void UpdateValueToAxis()
            {
                if (_valueToAxis != null)
                {
                    return;
                }
                lock (_valueToAxisLock)
                {
                    if (_valueToAxis != null)
                    {
                        return;
                    }
                    Dictionary<object, AxisValue> dict = new Dictionary<object, AxisValue>();
                    foreach (AxisValue value in _list)
                    {
                        object v = value.Value ?? NullKey;
                        dict[v] = value;
                    }
                    _valueToAxis = dict;
                }
            }
        }

        private const int InvalidIndex = int.MinValue;

        private CrossTable _owner;
        private int _index = InvalidIndex;

        internal void InvalidateIndex()
        {
            _index = InvalidIndex;
        }

        private void UpdateIndex()
        {
            if (_index != InvalidIndex)
            {
                return;
            }
            _index = _owner.Axises.IndexOf(this);
        }

        public int Index
        {
            get
            {
                UpdateIndex();
                return _index;
            }
        }

        public string Title { get; set; }
        public string PropertyName { get; set; }
        public object[] PropertyIndexes { get; set; }
        //public Type PropertyType { get; set; }
        public string StringFormat { get; set; }
        public IFormatProvider FormatProvider { get; set; }
        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        //public string OrderPropertyName { get; set; }
        //public Order OrderBy { get; set; }
        public AxisValueCollection Items { get; } = new AxisValueCollection();
        public AxisValue NoValue { get; }

        internal Axis(CrossTable owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            _owner = owner;
            NoValue = new AxisValue(this, NoAxisValue.Instance, string.Empty);
        }

        internal object GetValue(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            Type t = target.GetType();
            PropertyInfo property = t.GetProperty(PropertyName);
            if (property.PropertyType.IsArray)
            {
                return property.GetValue(target, PropertyIndexes);
            }
            else
            {
                return property.GetValue(target);
            }
        }

        internal string GetValueText(object target)
        {
            return ToText(GetValue(target));
        }

        private string FormatValue(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (StringFormat == null && FormatProvider == null)
            {
                return value.ToString();
            }
            MethodInfo method = value.GetType().GetMethod("ToString", new Type[] { typeof(string), typeof(IFormatProvider) });
            if (method != null)
            {
                return (string)method.Invoke(value, new object[] { StringFormat, FormatProvider });
            }
            if (FormatProvider == null)
            {
                method = value.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                if (method != null)
                {
                    return (string)method.Invoke(value, new object[] { StringFormat });
                }
            }
            if (StringFormat == null)
            {
                method = value.GetType().GetMethod("ToString", new Type[] { typeof(IFormatProvider) });
                if (method != null)
                {
                    return (string)method.Invoke(value, new object[] { FormatProvider });
                }
            }
            return value.ToString();
        }

        internal string ToText(object value)
        {
            object v = value;
            if (Converter != null)
            {
                v = Converter.Convert(v, typeof(string), ConverterParameter, CultureInfo.CurrentUICulture);
            }
            return FormatValue(v);
        }
        internal AxisValue Require(object target)
        {
            return Items.Require(this, target);
        }
    }

    partial class CrossTable
    {
        public class AxisCollection: IList<Axis>
        {
            private CrossTable _owner;
            private List<Axis> _list = new List<Axis>();

            public Axis this[int index]
            {
                get
                {
                    return _list[index];
                }
                set
                {
                    Axis old = _list[index];
                    _list[index] = value;
                    old?.InvalidateIndex();
                    value?.InvalidateIndex();
                    _owner.InvalidateCells();
                }
            }

            public int Count { get { return _list.Count; } }

            public bool IsReadOnly { get { return false; } }

            private void Invalidate()
            {
                foreach (Axis item in _list)
                {
                    item.InvalidateIndex();
                }
                _owner.InvalidateCells();
            }

            public void Add(Axis item)
            {
                _list.Add(item);
                item.InvalidateIndex();
                _owner.InvalidateCells();
            }

            public void Clear()
            {
                Invalidate();
                _list.Clear();
            }

            public bool Contains(Axis item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(Axis[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Axis> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            public int IndexOf(Axis item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, Axis item)
            {
                _list.Insert(index, item);
                Invalidate();
            }

            public bool Remove(Axis item)
            {
                Invalidate();
                return _list.Remove(item);
            }

            public void RemoveAt(int index)
            {
                Invalidate();
                _list.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_list).GetEnumerator();
            }

            internal AxisCollection(CrossTable owner)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                _owner = owner;
            }
        }
    }

    public class AxisValue
    {
        public Axis Owner { get; }
        public object Value { get; }
        public string Text { get; set; }
        //public IComparable Order { get; set; }

        public AxisValue(Axis owner, object value)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
            Value = value;
            Text = Owner.ToText(value);
        }

        public AxisValue(Axis owner, object value, string text)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
            Value = value;
            Text = text;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AxisValue))
            {
                return false;
            }
            return Equals(Value, ((AxisValue)obj).Value);
        }
        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }
            return Value.GetHashCode();
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
