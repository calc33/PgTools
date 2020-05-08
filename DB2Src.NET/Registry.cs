using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Db2Source
{
    public class RegistryManager
    {
        internal class RegistryPath
        {
            private string _path;
            private bool _isRegistryValid;
            private RegistryKey _rootKey;
            private string _subKey;

            internal RegistryPath(string path)
            {
                _path = path;
            }
            public string Path
            {
                get { return _path; }
                set
                {
                    if (_path == value)
                    {
                        return;
                    }
                    _path = value;
                    InvalidateRegistry();
                }
            }
            private void InvalidateRegistry()
            {
                _isRegistryValid = false;
            }
            private void UpdateRegistry()
            {
                if (_isRegistryValid)
                {
                    return;
                }
                _isRegistryValid = true;
                _rootKey = null;
                _subKey = null;
                int p = Path.IndexOf('\\');
                if (p == -1)
                {
                    return;
                }
                string root = Path.Substring(0, p);
                _rootKey = GetRootKey(root);
                if (_rootKey == null)
                {
                    return;
                }
                _subKey = Path.Substring(p + 1);
                _isRegistryValid = true;
                return;
            }
            public RegistryKey RootKey
            {
                get
                {
                    UpdateRegistry();
                    return _rootKey;
                }
            }
            public string SubKey
            {
                get
                {
                    UpdateRegistry();
                    return _subKey;
                }
            }
            public RegistryKey OpenReadonly(string path)
            {
                string key = SubKey + '\\' + path;
                return RootKey.OpenSubKey(key);
            }
            public RegistryKey OpenWritable(string path)
            {
                string key = SubKey + '\\' + path;
                return RootKey.CreateSubKey(key);
            }
        }
        public class RegistryPathCollection: IList<string>
        {
            internal class RegistryPathEnumerator: IEnumerator<string>
            {
                private RegistryPathCollection _owner;
                private int _index;
                internal RegistryPathEnumerator(RegistryPathCollection owner)
                {
                    _owner = owner;
                    _index = -1;
                }
                public string Current
                {
                    get
                    {
                        return _owner[_index];
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return _owner[_index];
                    }
                }

                public void Dispose()
                {
                    _owner = null;
                    _index = -1;
                }

                public bool MoveNext()
                {
                    _index++;
                    return _index < _owner.Count;
                }

                public void Reset()
                {
                    _index = -1;
                }
            }
            internal List<RegistryPath> _list = new List<RegistryPath>();

            public string this[int index]
            {
                get
                {
                    return _list[index].Path;
                }

                set
                {
                    _list[index].Path = value;
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
                    return false;
                }
            }

            public void Add(string item)
            {
                _list.Add(new RegistryPath(item));
            }

            public void Clear()
            {
                _list.Clear();
            }

            public bool Contains(string item)
            {
                foreach (RegistryPath obj in _list)
                {
                    if (obj.Path == item)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if (arrayIndex < 0)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex");
                }
                if (Count < array.Length - arrayIndex)
                {
                    throw new ArgumentException();
                }
                for (int i = 0; i < array.Length; i++)
                {
                    array[arrayIndex + i] = this[i];
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                return new RegistryPathEnumerator(this);
            }

            public int IndexOf(string item)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    if (_list[i].Path == item)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void Insert(int index, string item)
            {
                _list.Insert(index, new RegistryPath(item));
            }

            public bool Remove(string item)
            {
                int p = IndexOf(item);
                if (p == -1)
                {
                    return false;
                }
                _list.RemoveAt(p);
                return true;
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new RegistryPathEnumerator(this);
            }
        }
        private static readonly Dictionary<string, RegistryKey> _nameToRootKey = new Dictionary<string, RegistryKey>()
        {
            { "HKEY_CLASSES_ROOT", Registry.ClassesRoot },
            { "HKCR", Registry.ClassesRoot },
            { "HKEY_CURRENT_USER", Registry.CurrentUser },
            { "HKCU", Registry.CurrentUser },
            { "HKEY_LOCAL_MACHINE", Registry.LocalMachine },
            { "HKLM", Registry.LocalMachine },
            { "HKEY_USERS", Registry.Users },
            { "HKU", Registry.Users },
        };
        public static RegistryKey GetRootKey(string keyName)
        {
            RegistryKey key;
            if (!_nameToRootKey.TryGetValue(keyName.ToUpper(), out key))
            {
                return null;
            }
            return key;
        }
        public RegistryManager(params string[] paths)
        {
            foreach(string s in paths)
            {
                Paths.Add(s);
            }
        }
        public RegistryManager() { }
        public RegistryPathCollection Paths { get; } = new RegistryPathCollection();
        public RegistryKey Find(string path)
        {
            foreach (RegistryPath obj in Paths._list)
            {
                RegistryKey reg = obj.OpenReadonly(path);
                if (reg != null)
                {
                    return reg;
                }
            }
            return null;
        }
        public RegistryKey Find(string path, string name)
        {
            foreach (RegistryPath obj in Paths._list)
            {
                RegistryKey reg = obj.OpenReadonly(path);
                if (reg != null && reg.GetValue(name) != null)
                {
                    return reg;
                }
            }
            return null;
        }
        public object GetValue(string path, string name)
        {
            foreach (RegistryPath obj in Paths._list)
            {
                RegistryKey reg = obj.OpenReadonly(path);
                if (reg == null)
                {
                    continue;
                }
                object v = reg.GetValue(name);
                if (v != null)
                {
                    return v;
                }
            }
            return null;
        }
        public int GetInt32(string path, string name, int defaultValue)
        {
            object v = GetValue(path, name);
            if (v == null)
            {
                return defaultValue;
            }
            return Convert.ToInt32(v);
        }
        public long GetInt64(string path, string name, long defaultValue)
        {
            object v = GetValue(path, name);
            if (v == null)
            {
                return defaultValue;
            }
            return Convert.ToInt64(v);
        }
        public bool GetBool(string path, string name, bool defaultValue)
        {
            object v = GetValue(path, name);
            if (v == null)
            {
                return defaultValue;
            }
            return Convert.ToInt32(v) != 0;
        }
        public string GetString(string path, string name, string defaultValue)
        {
            object v = GetValue(path, name);
            if (v == null)
            {
                return defaultValue;
            }
            return v.ToString();
        }
        public RegistryKey OpenWritableAt(int index, string path)
        {
            RegistryPath obj = Paths._list[index];
            return obj.OpenWritable(path);
        }
        public void SetValue(int index, string path, string name, int value)
        {
            RegistryKey key = OpenWritableAt(index, path);
            key.SetValue(name, value, RegistryValueKind.DWord);
        }
        public void SetValue(int index, string path, string name, long value)
        {
            RegistryKey key = OpenWritableAt(index, path);
            key.SetValue(name, value, RegistryValueKind.QWord);
        }
        public void SetValue(int index, string path, string name, bool value)
        {
            RegistryKey key = OpenWritableAt(index, path);
            key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
        }
        public void SetValue(int index, string path, string name, string value)
        {
            RegistryKey key = OpenWritableAt(index, path);
            key.SetValue(name, value, RegistryValueKind.String);
        }
        private string GetWindowPath(Window window)
        {
            return !string.IsNullOrEmpty(window.Name) ? window.Name : window.GetType().Name;
        }
        private string GetControlPath(FrameworkElement target)
        {
            Window w = Window.GetWindow(target);
            if (w == null)
            {
                throw new ArgumentException("targetはWindowに属していません");
            }
            return GetWindowPath(w) + "\\Controls";
        }
        public void SaveStatus(CheckBox target)
        {
            if (string.IsNullOrEmpty(target.Name))
            {
                throw new ArgumentException("targetに名前がついていません");
            }
            string path = GetControlPath(target);
            bool? v = target.IsChecked;
            SetValue(0, path, target.Name, v.HasValue && v.Value);
        }
        public void LoadStatus(CheckBox target)
        {
            if (string.IsNullOrEmpty(target.Name))
            {
                throw new ArgumentException("targetに名前がついていません");
            }
            string path = GetControlPath(target);
            bool? v = target.IsChecked;
            target.IsChecked = GetBool(path, target.Name, v.HasValue && v.Value);
        }
        public void SaveStatus(TextBox target)
        {
            if (string.IsNullOrEmpty(target.Name))
            {
                throw new ArgumentException("targetに名前がついていません");
            }
            string path = GetControlPath(target);
            SetValue(0, path, target.Name, target.Text);
        }
        public void LoadStatus(TextBox target)
        {
            if (string.IsNullOrEmpty(target.Name))
            {
                throw new ArgumentException("targetに名前がついていません");
            }
            string path = GetControlPath(target);
            target.Text = GetString(path, target.Name, target.Text);
        }
        public void SaveStatus(Window target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            string path = GetWindowPath(target);
            if (target.WindowState == WindowState.Maximized)
            {
                SetValue(0, path, "Maximized", true);
            }
            else
            {
                SetValue(0, path, "Maximized", false);
                //SetValue(0, path, "Left", (int)target.Left);
                //SetValue(0, path, "Top", (int)target.Top);
                SetValue(0, path, "Width", (int)target.ActualWidth);
                SetValue(0, path, "Height", (int)target.ActualHeight);
            }
        }
        public void LoadStatus(Window target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            string path = GetWindowPath(target);
            bool b = GetBool(path, "Maximized", false);
            if (b)
            {
                target.WindowState = WindowState.Maximized;
            }
            else
            {
                target.WindowState = WindowState.Normal;
                //target.Left = GetInt32(path, "Left", (int)target.Left);
                //target.Top = GetInt32(path, "Top", (int)target.Top);
                target.Width = GetInt32(path, "Width", (int)target.ActualWidth);
                target.Height = GetInt32(path, "Height", (int)target.ActualHeight);
            }
        }
    }
}
