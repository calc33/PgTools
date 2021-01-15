using System;
using System.Collections;
using System.Collections.Generic;

namespace Db2Source
{
    public abstract class NamedObject : IComparable, IDisposable
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
        protected internal bool _released;
        protected bool disposedValue;

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
        public virtual void Release()
        {
            _released = true;
        }

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
                return string.Compare(GetType().FullName, obj.GetType().FullName);
            }
            return string.Compare(Identifier, (((NamedObject)obj).Identifier));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~NamedObject()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
                for(int i = _list.Count - 1; 0 <= i; i--)
                {
                    if (_list[i]._released)
                    {
                        _list.RemoveAt(i);
                    }
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
                    foreach (NamedObject item in _list)
                    {
                        if (item._released)
                        {
                            continue;
                        }
                        if (delIds.ContainsKey(item._serial))
                        {
                            item.Release();
                        }
                    }
                    for (int i = _list.Count - 1; 0 <= i; i--)
                    {
                        NamedObject item = _list[i];
                        if (item._released)
                        {
                            _list.RemoveAt(i);
                        }
                    }
                }
                _nameDict = dict;
            }
        }
        public void Invalidate()
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
            Invalidate();
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
            Invalidate();
        }

        public void Clear()
        {
            _list.Clear();
            Invalidate();
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
                Invalidate();
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
