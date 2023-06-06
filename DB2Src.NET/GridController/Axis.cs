using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Db2Source
{
    /// <summary>
    /// クロス集計の集計項目を定義する
    /// </summary>
    public class Axis
    {
        public static readonly Axis[] EmptyArray = new Axis[0];
        /// <summary>
        /// 小計を表現する際に小計の対象項目以外にはNoAxisValueをセットする
        /// </summary>
        internal sealed class NoAxisValue : IComparable
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
                    int i = 0;
                    foreach (AxisValue value in _list)
                    {
                        object v = value.Value ?? NullKey;
                        dict[v] = value;
                        value._index = i++;
                    }
                    _valueToAxis = dict;
                }
            }
            internal void UpdateIndex()
            {
                UpdateValueToAxis();
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

        /// <summary>
        /// 小計表示の有無
        /// </summary>
        public bool ShowSubtotal { get; set; } = true;

        /// <summary>
        /// 上位要素の小計欄中に展開して表示する
        /// </summary>
        public bool ExpandInSubtotal { get; set; } = false;

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
}
