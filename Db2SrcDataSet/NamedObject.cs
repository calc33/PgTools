﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Db2Source
{
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

        protected internal static bool ArrayEquals<T>(T[] a, T[] b) where T: class
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

        protected internal static bool DictionaryEquals<K, V>(Dictionary<K, V> a, Dictionary<K, V> b) where K: class where V: class
        {
            if (a == null) {
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
            if (GetType() != obj.GetType())
            {
                return string.Compare(GetType().FullName, obj.GetType().FullName);
            }
            return string.Compare(FullIdentifier, ((NamedObject)obj).FullIdentifier);
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
                for (int i = _list.Count - 1; 0 <= i; i--)
                {
                    if (_list[i]._released)
                    {
                        _list.RemoveAt(i);
                    }
                }
                Dictionary<string, NamedObject> dict = new Dictionary<string, NamedObject>();
                Dictionary<int, bool> delIds = new Dictionary<int, bool>();
                List<NamedObject> source = new List<NamedObject>(_list);
                foreach (NamedObject item in source)
                {
                    //string id = item.FullIdentifier;
                    string id = item.Identifier;
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
                //if (string.IsNullOrEmpty(name))
                //{
                //    return null;
                //}
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

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return _list.GetEnumerator();
        //}
        #endregion
    }
}
