using System;
using System.Collections;
using System.Collections.Generic;

namespace Db2Source
{
    public abstract class NamedObject : IComparable
    {
        public string Identifier
        {
            get
            {
                return GetIdentifier();
            }
        }
        protected abstract string GetIdentifier();
        protected void InvalidateIdentifier()
        {
            IdentifierInvalidated?.Invoke(this, EventArgs.Empty);
        }
        protected internal int _serial;
        internal event EventHandler IdentifierInvalidated;
        internal NamedObject(NamedCollection owner)
        {
            if (owner != null)
            {
                _serial = owner._serialSeq++;
                owner.Add(this);
            }
        }
        public virtual bool IsModified() { return false; }
        public virtual void Release() { }

        public override string ToString()
        {
            return Identifier;
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
        private object _listLock = new object();
        Dictionary<string, NamedObject> _nameDict = null;
        internal int _serialSeq = 1;

        protected internal void UpdateList()
        {
            if (_nameDict != null)
            {
                return;
            }
            lock (_listLock)
            {
                if (_nameDict != null)
                {
                    return;
                }
                Dictionary<string, NamedObject> dict = new Dictionary<string, NamedObject>();
                Dictionary<int, bool> delIds = new Dictionary<int, bool>();
                foreach (NamedObject item in _list)
                {
                    if (string.IsNullOrEmpty(item.Identifier))
                    {
                        continue;
                    }
                    //重複している場合には古い方(_serialが小さい方)を削除
                    string id = item.Identifier;
                    NamedObject old;
                    if (!dict.TryGetValue(id, out old))
                    {
                        dict[id] = item;
                        continue;
                    }
                    if (old._serial == item._serial)
                    {
                        continue;
                    }
                    if (old._serial < item._serial)
                    {
                        delIds[old._serial] = true;
                        dict[id] = item;
                    }
                    else
                    {
                        delIds.Add(item._serial, true);
                    }
                }
                if (delIds.Count != 0)
                {
                    for (int i = _list.Count - 1; 0 <= i; i--)
                    {
                        NamedObject item = _list[i];
                        if (delIds.ContainsKey(item._serial))
                        {
                            _list.RemoveAt(i);
                            item.Release();
                        }
                    }
                }
                _nameDict = dict;
            }
        }
        private void InvalidateList()
        {
            _nameDict = null;
        }

        public NamedObject this[int index]
        {
            get
            {
                UpdateList();
                return _list[index];
            }
        }
        public NamedObject this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                UpdateList();
                NamedObject ret;
                if (!_nameDict.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }

        internal void ItemIdentifierInvalidated(object sender, EventArgs e)
        {
            InvalidateList();
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
            UpdateList();
            _list.Sort();
        }

        #region ICollection<T>の実装
        public int Count
        {
            get
            {
                UpdateList();
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
            item.IdentifierInvalidated += ItemIdentifierInvalidated;
            InvalidateList();
        }

        public void Clear()
        {
            _list.Clear();
            InvalidateList();
            _serialSeq = 1;
        }

        public bool Contains(NamedObject item)
        {
            UpdateList();
            return _list.Contains(item);
        }

        public void CopyTo(NamedObject[] array, int arrayIndex)
        {
            UpdateList();
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<NamedObject> GetEnumerator()
        {
            UpdateList();
            return _list.GetEnumerator();
        }

        public bool Remove(NamedObject item)
        {
            bool ret = _list.Remove(item);
            if (ret)
            {
                InvalidateList();
            }
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            UpdateList();
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
            UpdateList();
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
