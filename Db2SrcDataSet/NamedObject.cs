using System;
using System.Collections;
using System.Collections.Generic;

namespace Db2Source
{
    public class NamedObject : IComparable
    {
        private string _identifier;
        public string Identifier
        {
            get { return _identifier; }
            set
            {
                if (_identifier == value)
                {
                    return;
                }
                _identifier = value;
                IdentifierChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        internal event EventHandler IdentifierChanged;
        internal NamedObject(NamedCollection owner)
        {
            if (owner != null)
            {
                owner.Add(this);
            }
        }
        internal NamedObject(NamedCollection owner, string identifier)
        {
            Identifier = identifier;
            if (owner != null)
            {
                owner.Add(this);
            }
        }
        public virtual bool IsModified() { return false; }
        public virtual void Release() { }

        public override string ToString()
        {
            return _identifier;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Identifier == ((NamedObject)obj).Identifier;
        }
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(Identifier))
            {
                return 0;
            }
            return Identifier.GetHashCode();
        }
        public virtual int CompareTo(object obj)
        {
            if (obj == null)
            {
                return -1;
            }
            if (GetType() != obj.GetType())
            {
                return -1;
            }
            return string.Compare(Identifier, (((NamedObject)obj).Identifier));
        }
    }
    public class NamedCollection : ICollection<NamedObject>
    {
        internal List<NamedObject> _list = new List<NamedObject>();
        Dictionary<string, NamedObject> _nameDict = null;

        private void RequireNameDict()
        {
            if (_nameDict != null)
            {
                return;
            }
            _nameDict = new Dictionary<string, NamedObject>();
            foreach (NamedObject item in _list)
            {
                if (string.IsNullOrEmpty(item.Identifier))
                {
                    continue;
                }
                _nameDict[item.Identifier] = item;
            }
        }

        public NamedObject this[int index] { get { return _list[index]; } }
        public NamedObject this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameDict();
                NamedObject ret;
                if (!_nameDict.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }

        internal void ItemIdentifierChanged(object sender, EventArgs e)
        {
            _nameDict = null;
        }

        public void ReleaseAll()
        {
            foreach (NamedObject o in _list)
            {
                o.Release();
            }
        }
        public void Sort()
        {
            _list.Sort();
        }

        #region ICollection<T>の実装
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

        public void Add(NamedObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            _list.Add(item);
            item.IdentifierChanged += ItemIdentifierChanged;
            _nameDict = null;
        }

        public void Clear()
        {
            _list.Clear();
            _nameDict = null;
        }

        public bool Contains(NamedObject item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(NamedObject[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<NamedObject> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Remove(NamedObject item)
        {
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameDict = null;
            }
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }

    public class NamedCollection<T> : NamedCollection, ICollection<T> where T : NamedObject
    {
        internal class EnumeratorWrapper : IEnumerator<T>
        {
            private IEnumerator _base;
            internal EnumeratorWrapper(IEnumerator enumerator)
            {
                _base = enumerator;
            }

            public T Current
            {
                get
                {
                    return (T)_base.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return _base.Current;
                }
            }

            public void Dispose()
            {
                ((IDisposable)_base).Dispose();
            }

            public bool MoveNext()
            {
                return _base.MoveNext();
            }

            public void Reset()
            {
                _base.Reset();
            }
        }
        public new T this[int index] { get { return (T)base[index]; } }
        public new T this[string name] { get { return (T)base[name]; } }

        #region ICollection<T>の実装
        //public int Count {
        //    get {
        //        return _list.Count;
        //    }
        //}

        //public bool IsReadOnly {
        //    get {
        //        return false;
        //    }
        //}

        public void Add(T item)
        {
            base.Add(item);
        }

        //public void Clear() {
        //    _list.Clear();
        //    _nameDict = null;
        //}

        public bool Contains(T item)
        {
            return base.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return new EnumeratorWrapper(_list.GetEnumerator());
        }

        public bool Remove(T item)
        {
            return base.Remove(item);
        }

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return _list.GetEnumerator();
        //}
        #endregion
    }
}
