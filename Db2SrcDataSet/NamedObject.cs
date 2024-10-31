using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Db2Source
{
    public struct NamedObjectId : IComparable, IComparable<NamedObjectId>
    {
        public NamespaceIndex Index { get; private set; }
        public string Identifier { get; private set; }

        public NamedObjectId(NamedObject obj)
        {
            Index = obj.GetCollectionIndex();
            Identifier = obj.FullIdentifier;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Index.ToString(), Identifier);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NamedObjectId id))
            {
                return false;
            }
            return Index == id.Index && Identifier == id.Identifier;
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode() * 13 + Identifier.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (!(obj is NamedObjectId other))
            {
                return -1;
            }
            return CompareTo(other);
        }

        public int CompareTo(NamedObjectId other)
        {
            int ret = Index - other.Index;
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(Identifier, other.Identifier);
            return ret;
        }
    }

    public abstract partial class NamedObject : IComparable, IDisposable, INotifyPropertyChanged
    {
        public string FullIdentifier
        {
            get
            {
                return GetFullIdentifier();
            }
        }
        public string Identifier
        {
            get
            {
                return GetIdentifier();
            }
        }
        protected abstract string GetFullIdentifier();
        protected abstract string GetIdentifier();
        protected abstract int GetIdentifierDepth();

        public abstract NamespaceIndex GetCollectionIndex();

        protected void InvalidateIdentifier()
        {
            IdentifierInvalidated?.Invoke(this, EventArgs.Empty);
        }
        protected internal int _serial;
        protected internal bool _released;
        protected bool IsDisposed;

        internal event EventHandler IdentifierInvalidated;

        public bool IsReleased
        {
            get
            {
                return _released;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract bool HasBackup();
        public abstract void Backup(bool force);

        public abstract void Restore();

        protected internal static bool ArrayEquals<T>(T[] a, T[] b) where T : class
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }
            return true;
        }

        protected internal static bool DictionaryEquals<K, V>(Dictionary<K, V> a, Dictionary<K, V> b) where K : class where V : class
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }
            if (b == null)
            {
                throw new ArgumentNullException("b");
            }
            if (a.Count != b.Count)
            {
                return false;
            }
            foreach (KeyValuePair<K, V> kv in a)
            {
                V v;
                if (!b.TryGetValue(kv.Key, out v) || !Equals(kv.Value, v))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual bool ContentEquals(NamedObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            if (FullIdentifier != obj.FullIdentifier)
            {
                return false;
            }
            return true;
        }

        internal NamedObject(NamedCollection owner)
        {
            if (owner != null)
            {
                _serial = owner._serialSeq++;
                owner.Add(this);
            }
        }
        public virtual bool IsModified { get { return false; } }
        public virtual bool IsEnabled { get { return true; } }
        public virtual void Release()
        {
            _released = true;
        }

        public override string ToString()
        {
            return FullIdentifier;
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
            return FullIdentifier == ((NamedObject)obj).FullIdentifier;
        }
        public override int GetHashCode()
        {
            string id = FullIdentifier;
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }
            return id.GetHashCode();
        }
        public virtual int CompareTo(object obj)
        {
            if (obj == null)
            {
                return -1;
            }
            int ret = string.Compare(FullIdentifier, ((NamedObject)obj).FullIdentifier);
            if (ret != 0)
            {
                return ret;
            }
            return string.Compare(GetType().FullName, obj.GetType().FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                // TODO: マネージド状態を破棄します (マネージド オブジェクト)
            }

            // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
            // TODO: 大きなフィールドを null に設定します
            IsDisposed = true;
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
        internal class Filtered<T> : IEnumerable<T> where T : NamedObject
        {
            private class FilteredEnumerator : IEnumerator<T>
            {
                private NamedCollection _owner;
                private string _filter;
                private int _index;

                public T Current
                {
                    get
                    {
                        return (0 <= _index && _index < _owner.Count) ? (T)_owner[_index] : null;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return (0 <= _index && _index < _owner.Count) ? _owner[_index] : null;
                    }
                }

                public void Dispose()
                {
                    _owner = null;
                    _filter = null;
                }

                public bool MoveNext()
                {
                    _index++;
                    return (_index < _owner.Count) && _owner[_index].FullIdentifier.StartsWith(_filter);
                }

                private int FindFirstRecursive(int i0, int i1)
                {
                    if (i1 <= i0)
                    {
                        return i0;
                    }
                    if (i0 + 1 == i1)
                    {
                        if (string.Compare(_owner[i1].FullIdentifier, _filter) < 0)
                        {
                            return i1;
                        }
                        return i0;

                    }
                    int i = (i0 + i1) / 2;
                    if (string.Compare(_owner[i].FullIdentifier, _filter) < 0)
                    {
                        return FindFirstRecursive(i, i1);
                    }
                    else
                    {
                        return FindFirstRecursive(i0, i - 1);
                    }
                }
                public void Reset()
                {
                    for (_index = FindFirstRecursive(0, _owner.Count - 1); 0 <= _index && string.Compare(_owner[_index].FullIdentifier, _filter) > 0; _index--) ;
                }

                internal FilteredEnumerator(NamedCollection owner, string filter)
                {
                    _owner = owner;
                    _filter = filter;
                    Reset();
                }
            }

            private NamedCollection _owner;
            private string _filter;

            public IEnumerator<T> GetEnumerator()
            {
                return new FilteredEnumerator(_owner, _filter);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new FilteredEnumerator(_owner, _filter);
            }

            internal Filtered(NamedCollection owner, string filter)
            {
                _owner = owner;
                _filter = filter + ".";
            }
        }

        internal List<NamedObject> _list = new List<NamedObject>();
        private readonly object _listLock = new object();
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
                bool needSort = false;
                NamedObject prev = null;
                for (int i = _list.Count - 1; 0 <= i; i--)
                {
                    NamedObject obj = _list[i];
                    if (_list[i]._released)
                    {
                        _list.RemoveAt(i);
                    }
                    else
                    {
						if (prev != null && prev.CompareTo(obj) < 0)
						{
							needSort = true;
						}
                        prev = obj;
					}
				}
				Dictionary<string, NamedObject> dict = new Dictionary<string, NamedObject>();
                Dictionary<int, bool> delIds = new Dictionary<int, bool>();
                List<NamedObject> source = new List<NamedObject>(_list);
                foreach (NamedObject item in source)
                {
                    string id = item.FullIdentifier;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    //重複している場合には古い方(_serialが小さい方)を削除
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
                        old.ReplaceTo(item);
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
                if (needSort)
                {
					_list.Sort();
				}
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
                string s = name ?? string.Empty;
                if (string.IsNullOrEmpty(s))
                {
                    return null;
                }
                UpdateList();
                NamedObject ret;
                if (!_nameDict.TryGetValue(s, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        public NamedObject this[string schema, string name]
        {
            get
            {
                string s = Db2SourceContext.JointIdentifier(schema, name);
                if (string.IsNullOrEmpty(s))
                {
                    return null;
                }
                UpdateList();
                NamedObject ret;
                if (!_nameDict.TryGetValue(s, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        public NamedObject this[string schema, string table, string name]
        {
            get
            {
                string s = Db2SourceContext.JointIdentifier(schema, table, name);
                if (string.IsNullOrEmpty(s))
                {
                    return null;
                }
                UpdateList();
                NamedObject ret;
                if (!_nameDict.TryGetValue(s, out ret))
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
            NamedObject[] l = _list.ToArray();
            foreach (NamedObject o in l)
            {
                o.Release();
            }
        }

        public IEnumerable<NamedObject> GetFiltered(string filter)
        {
            return new Filtered<NamedObject>(this, filter);
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
            foreach (NamedObject item in _list)
            {
                item.IdentifierInvalidated -= ItemIdentifierInvalidated;
            }
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
            private readonly IEnumerator _base;
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
        public new T this[string schema, string name] { get { return (T)base[schema, name]; } }
        public new T this[string schema, string table, string name] { get { return (T)base[schema, table, name]; } }

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

        public void AddRange(IEnumerable<T> list)
        {
            foreach (T item in list)
            {
                Add(item);
            }
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

        public new IEnumerable<T> GetFiltered(string filter)
        {
            return new Filtered<T>(this, filter);
        }

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return _list.GetEnumerator();
        //}
        #endregion
    }

    public class FilteredNamedCollection<T> where T : NamedObject
    {
        NamedCollection _baseList;
        public T this[string name]
        {
            get
            {
                return _baseList[name] as T;
            }
        }
        public T this[string schema, string name]
        {
            get
            {
                return _baseList[schema, name] as T;
            }
        }
		public T this[string schema, string table, string name]
		{
			get
			{
				return _baseList[schema, table, name] as T;
			}
		}
        internal FilteredNamedCollection(NamedCollection baseList)
		{
			_baseList = baseList;
		}
	}
}
