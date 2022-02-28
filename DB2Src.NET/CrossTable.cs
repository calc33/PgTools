using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Db2Source
{
    public class Axis
    {
        private sealed class NoAxisData : IComparable
        {
            public static NoAxisData Instance = new NoAxisData();
            /// <summary>
            /// あらゆるデータより後ろとして扱う
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int CompareTo(object obj)
            {
                if (obj is NoAxisData)
                {
                    return 0;
                }
                return 1;
            }

            /// <summary>
            /// NoAxisDataインスタンスはすべて同一とみなす
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return (obj is NoAxisData);
            }

            /// <summary>
            /// NoAxisDataインスタンスはすべて同一とみなす
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return 0;
            }
        }
        public class Data
        {
            public object Value { get; set; }
            public string Text { get; set; }
            //public IComparable Order { get; set; }

            public override bool Equals(object obj)
            {
                if (!(obj is Data))
                {
                    return false;
                }
                return Equals(Value, ((Data)obj).Value);
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

        public class DataArray : IList<Data>
        {
            private Data[] _array;

            public Data this[int index]
            {
                get { return _array[index]; }
                set { _array[index] = value; }
            }

            public int Count { get { return _array.Length; } }

            public bool IsReadOnly { get { return false; } }

            void ICollection<Data>.Add(Data item) { }

            void ICollection<Data>.Clear() { }

            public bool Contains(Data item)
            {
                return _array.Contains(item);
            }

            public void CopyTo(Data[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Data> GetEnumerator()
            {
                return ((IEnumerable<Data>)_array).GetEnumerator();
            }

            public int IndexOf(Data item)
            {
                return ((IList<Data>)_array).IndexOf(item);
            }

            void IList<Data>.Insert(int index, Data item) { }

            bool ICollection<Data>.Remove(Data item)
            {
                return false;
            }

            void IList<Data>.RemoveAt(int index) { }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _array.GetEnumerator();
            }

            //public DataArray()
            //{
            //    _array = new Data[0];
            //}

            //public DataArray(int length)
            //{
            //    _array = new Data[length];
            //}

            //public DataArray(Data[] array)
            //{
            //    if (array == null)
            //    {
            //        throw new ArgumentNullException("array");
            //    }
            //    _array = new Data[array.Length];
            //    array.CopyTo(_array, 0);
            //}

            public DataArray(object target, Axis[] axises)
            {
                if (target == null)
                {
                    throw new ArgumentNullException("target");
                }
                if (axises == null)
                {
                    throw new ArgumentNullException("axises");
                }
                _array = new Data[axises.Length];
                for (int i = 0; i < _array.Length; i++)
                {
                    _array[i] = axises[i].Require(target);
                }
            }
            internal DataArray(DataArray source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                _array = new Data[source.Count];
                source.CopyTo(_array, 0);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is DataArray))
                {
                    return false;
                }
                DataArray a = (DataArray)obj;
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
                foreach (Data data in _array)
                {
                    hash = hash * 17 + data.GetHashCode();
                }
                return hash;
            }
        }

        public class DataCollection : IList<Data>
        {
            private static readonly object NullKey = new object();
            private List<Data> _list = new List<Data>();
            private Dictionary<object, Data> _valueToData = null;
            private object _valueToDataLock = new object();

            public Data this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = value; }
            }

            public Data this[object value]
            {
                get
                {
                    UpdateValueToData();
                    object v = value ?? NullKey;
                    Data ret;
                    if (_valueToData.TryGetValue(v, out ret))
                    {
                        return ret;
                    }
                    return null;
                }
            }

            public Data Require(Axis owner, object record)
            {
                object v = owner.GetValue(record);
                Data ret = this[v];
                if (ret != null)
                {
                    return ret;
                }
                ret = new Data() { Value = v, Text = owner.GetText(v) };
                _list.Add(ret);
                _valueToData[v ?? NullKey] = ret;
                return ret;
            }

            public int Count { get { return _list.Count; } }

            public bool IsReadOnly { get { return false; } }

            public void Add(Data item)
            {
                _list.Add(item);
                InvalidateValueToData();
            }

            public void Clear()
            {
                _list.Clear();
                InvalidateValueToData();
            }

            public bool Contains(Data item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(Data[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Data> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            public int IndexOf(Data item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, Data item)
            {
                _list.Insert(index, item);
                InvalidateValueToData();
            }

            public bool Remove(Data item)
            {
                bool ret = _list.Remove(item);
                if (ret)
                {
                    InvalidateValueToData();
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                InvalidateValueToData();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_list).GetEnumerator();
            }

            private void InvalidateValueToData()
            {
                _valueToData = null;
            }
            private void UpdateValueToData()
            {
                if (_valueToData != null)
                {
                    return;
                }
                lock (_valueToDataLock)
                {
                    if (_valueToData != null)
                    {
                        return;
                    }
                    Dictionary<object, Data> dict = new Dictionary<object, Data>();
                    foreach (Data data in _list)
                    {
                        object v = data.Value ?? NullKey;
                        dict[v] = data;
                    }
                    _valueToData = dict;
                }
            }
        }

        public string Title { get; set; }
        public string PropertyName { get; set; }
        public object[] PropertyIndexes { get; set; }
        //public Type PropertyType { get; set; }
        public string FormatString { get; set; }
        public IFormatProvider FormatProvider { get; set; }
        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public string OrderPropertyName { get; set; }
        public Order OrderBy { get; set; }
        public DataCollection Items { get; } = new DataCollection();
        public Data NoData { get; } = new Data() { Value = NoAxisData.Instance, Text = string.Empty };

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

        private string FormatValue(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (FormatString == null && FormatProvider == null)
            {
                return value.ToString();
            }
            MethodInfo method = value.GetType().GetMethod("ToString", new Type[] { typeof(string), typeof(IFormatProvider) });
            if (method != null)
            {
                return (string)method.Invoke(value, new object[] { FormatString, FormatProvider });
            }
            if (FormatProvider == null)
            {
                method = value.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                if (method != null)
                {
                    return (string)method.Invoke(value, new object[] { FormatString });
                }
            }
            if (FormatString == null)
            {
                method = value.GetType().GetMethod("ToString", new Type[] { typeof(IFormatProvider) });
                if (method != null)
                {
                    return (string)method.Invoke(value, new object[] { FormatProvider });
                }
            }
            return value.ToString();
        }

        internal string GetText(object value)
        {
            object v = value;
            if (Converter != null)
            {
                v = Converter.Convert(v, typeof(string), ConverterParameter, CultureInfo.CurrentUICulture);
            }
            return FormatValue(v);
        }
        internal Data Require(object target)
        {
            return Items.Require(this, target);
        }
    }

    public partial class CrossTable
    {
        public class Record
        {
            public class ItemCollection : IList<object>
            {
                private Record _owner;
                private List<object> _list = new List<object>();

                public object this[int index]
                {
                    get
                    {
                        return _list[index];
                    }
                    set
                    {
                        _list[index] = value;
                        _owner.InvalidateSummaryResult();
                    }
                }

                public int Count { get { return _list.Count; } }

                public bool IsReadOnly { get { return false; } }

                public void Add(object item)
                {
                    _list.Add(item);
                    _owner.InvalidateSummaryResult();
                }

                public void Clear()
                {
                    _list.Clear();
                    _owner.InvalidateSummaryResult();
                }

                public bool Contains(object item)
                {
                    return _list.Contains(item);
                }

                public void CopyTo(object[] array, int arrayIndex)
                {
                    _list.CopyTo(array, arrayIndex);
                }

                public IEnumerator<object> GetEnumerator()
                {
                    return _list.GetEnumerator();
                }

                public int IndexOf(object item)
                {
                    return _list.IndexOf(item);
                }

                public void Insert(int index, object item)
                {
                    _list.Insert(index, item);
                    _owner.InvalidateSummaryResult();
                }

                public bool Remove(object item)
                {
                    bool ret = _list.Remove(item);
                    if (ret)
                    {
                        _owner.InvalidateSummaryResult();
                    }
                    return ret;
                }

                public void RemoveAt(int index)
                {
                    _list.RemoveAt(index);
                    _owner.InvalidateSummaryResult();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable)_list).GetEnumerator();
                }
                internal ItemCollection(Record owner)
                {
                    if (owner == null)
                    {
                        throw new ArgumentNullException("owner");
                    }
                    _owner = owner;
                }
            }
            public ItemCollection Items { get; private set; }
            public Axis.DataArray KeyAxis { get; internal set; }
            public SummaryOperatorBase[] Summaries { get; internal set; }

            public Record(IList<Axis> axises, Axis.DataArray key, IList<SummaryDefinition> summaryDefinitions)
            {
                Items = new ItemCollection(this);
                KeyAxis = key;
                Summaries = new SummaryOperatorBase[summaryDefinitions.Count];
                for (int i = 0; i < Summaries.Length; i++)
                {
                    Summaries[i] = summaryDefinitions[i].NewOperator(this, axises[i]);
                }
            }

            public void Add(object record)
            {
                Items.Add(record);
            }

            private void InvalidateSummaryResult()
            {
                foreach (SummaryOperatorBase op in Summaries)
                {
                    op.InvalidateResult();
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Record))
                {
                    return false;
                }
                Record rec = (Record)obj;
                return Equals(KeyAxis, rec.KeyAxis);
            }
            public override int GetHashCode()
            {
                return KeyAxis.GetHashCode();
            }
        }

        private SortedList<Axis.DataArray, Record> _records = null;
        private object _recordsLock = new object();

        private void AddRecordInternal(SortedList<Axis.DataArray, Record> list, Axis.DataArray key, object value)
        {
            Record rec;
            if (!list.TryGetValue(key, out rec))
            {
                rec = new Record(Axises, key, SummaryDefinitions);
                list.Add(key, rec);
            }
            rec.Add(value);
        }

        private void AddRecordRecursive(SortedList<Axis.DataArray, Record> list, Axis.DataArray key, int replacingIndex, object value)
        {
            if (replacingIndex == key.Count)
            {
                AddRecordInternal(list, key, value);
                return;
            }
            AddRecordRecursive(list, key, replacingIndex + 1, value);
            Axis.DataArray key2 = new Axis.DataArray(key);
            key2[replacingIndex] = Axises[replacingIndex].NoData;
            AddRecordRecursive(list, key2, replacingIndex + 1, value);
        }

        private void UpdateRecord()
        {
            if (_records != null)
            {
                return;
            }
            lock (_recordsLock)
            {
                if (_records != null)
                {
                    return;
                }
                Axis[] axises = Axises.ToArray();
                SortedList<Axis.DataArray, Record> l = new SortedList<Axis.DataArray, Record>();
                if (ItemsSource == null)
                {
                    _records = l;
                    return;
                }
                foreach (object obj in ItemsSource)
                {
                    Axis.DataArray key = new Axis.DataArray(obj, axises);
                    AddRecordRecursive(l, key, 0, obj);
                }
                _records = l;
            }
        }
        private void InvalidateRecord()
        {
            _records = null;
        }

        public List<Axis> AxisCandidates { get; } = new List<Axis>();
        public List<Axis> Axises { get; } = new List<Axis>();
        public List<SummaryDefinition> SummaryDefinitions { get; } = new List<SummaryDefinition>();
        private IEnumerable _itemsSource;
        public IEnumerable ItemsSource
        {
            get
            {
                return _itemsSource;
            }
            set
            {
                _itemsSource = value;
                InvalidateRecord();
            }
        }
    }
}
