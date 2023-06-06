using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Db2Source
{
    /// <summary>
    /// Equals()で全要素が一致しているかどうかを比較できるAxisValueの配列
    /// </summary>
    public class AxisValueArray : IList<AxisValue>
    {
        private AxisValue[] _array;

        public bool IsSubtotalOf(AxisValueArray value)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                AxisValue v1 = this[i];
                AxisValue v2 = value[i];
                if (v1.IsNoValue && !v2.IsNoValue)
                {
                    return true;
                }
                if (!Equals(v1, v2))
                {
                    return false;
                }
            }
            return false;
        }
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

        internal AxisValueArray(AxisValueArray source, AxisCollection axises)
        {
            int n = axises.Count;
            _array = new AxisValue[n];
            for (int i = 0; i < n; i++)
            {
                Axis axis = axises[i];
                if (axis != null)
                {
                    _array[i] = this[axis.Index];
                }
            }
        }

        internal AxisValueArray(CrossTable table, params AxisEntry[] entries) : this(table.Axises)
        {
            foreach (AxisEntry entry in entries)
            {
                foreach (AxisValue value in entry.Values)
                {
                    int i = value.Owner.Index;
                    if (i == -1)
                    {
                        continue;
                    }
                    _array[i] = value;
                }
            }
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
}
