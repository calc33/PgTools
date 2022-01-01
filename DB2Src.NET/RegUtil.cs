using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Unicorn.Utility
{
    public struct RegistryPath
    {
        public RegistryKey Key { get; set; }
        public string Path { get; set; }

        public RegistryPath(RegistryKey key, string path)
        {
            Key = key;
            Path = path;
        }
        public RegistryPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            int p = path.IndexOf('\\');
            if (p == -1)
            {
                throw new ArgumentException(string.Format("pathの値が不正です:{0}", path));
            }
            if (p == 0)
            {
                Key = Registry.CurrentUser;
                Path = path;
                return;
            }
            string k = path.Substring(0, p).ToUpper();
            Path = path.Substring(p);
            switch (k)
            {
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    Key = Registry.CurrentUser;
                    return;
                case "KEY_LOCAL_MACHINE":
                case "HKLM":
                    Key = Registry.LocalMachine;
                    return;
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    Key = Registry.ClassesRoot;
                    return;
                case "HKEY_USERS":
                    Key = Registry.Users;
                    return;
                case "HKEY_CURRENT_CONFIG":
                    Key = Registry.CurrentConfig;
                    return;
                //case "HKEY_DYN_DATA":
                //    Key = Registry.DynData;
                //    return;
                case "HKEY_PERFORMANCE_DATA":
                    Key = Registry.PerformanceData;
                    return;
                default:
                    throw new ArgumentException(string.Format("レジストリキー{0}は存在しません", k));
            }
        }

        public RegistryKey OpenKey(string path, bool writable, bool createNew)
        {
            string dir = System.IO.Path.Combine(Path, path);
            if (string.IsNullOrEmpty(dir) || dir == "\\")
            {
                return Key;
            }
            if ((dir[0] == '\\'))
            {
                dir = dir.Remove(0, 1);
            }
            RegistryKey ret = Key.OpenSubKey(dir, writable);
            if (ret == null && writable && createNew)
            {
                ret = Key.CreateSubKey(dir);
            }
            return ret;
        }

        public override string ToString()
        {
            return Key.Name + Path;
        }
    }

    public interface IRegistryOperator
    {
        RegistryValueKind GetValueKind();
        void Read(RegistryKey key, string name, object control, PropertyInfo property);
        void Write(RegistryKey key, string name, object control, PropertyInfo property);
    }
    public class StringOperator: IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.String;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            string v = (string)property.GetValue(control);
            v = RegistryBinding.ReadString(key, name, v);
            property.SetValue(control, v);
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            string v = property.GetValue(control)?.ToString() ?? string.Empty;
            key.SetValue(name, v, RegistryValueKind.String);
        }
    }
    public class DoubleOperator : IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.String;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            string v = property.GetValue(control)?.ToString();
            v = RegistryBinding.ReadString(key, name, v);
            try
            {
                double d = double.Parse(v);
                property.SetValue(control, d);
            }
            catch (FormatException) { }
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            string v = property.GetValue(control)?.ToString() ?? string.Empty;
            key.SetValue(name, v, RegistryValueKind.String);
        }
    }
    public class Int32Operator: IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.DWord;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            int v = (int)property.GetValue(control);
            v = RegistryBinding.ReadInt(key, name, v);
            property.SetValue(control, v);
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            object v = property.GetValue(control);
            v = Convert.ToInt32(v);
            key.SetValue(name, v, RegistryValueKind.DWord);
        }
    }
    public class BoolOperator : IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.DWord;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            bool b = (bool)property.GetValue(control);
            int v = b ? 1 : 0;
            v = RegistryBinding.ReadInt(key, name, v);
            switch (v)
            {
                case 0:
                    property.SetValue(control, false);
                    break;
                default:
                    property.SetValue(control, true);
                    break;
            }
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            bool v = (bool)property.GetValue(control);
            int val = v ? 1 : 0;
            key.SetValue(name, val, RegistryValueKind.DWord);
        }
    }

    public class NullableBoolOperator: IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.DWord;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            bool? b = (bool?)property.GetValue(control);
            int v = b.HasValue ? (b.Value ? 1 : 0) : -1;
            v = RegistryBinding.ReadInt(key, name, v);
            switch (v)
            {
                case -1:
                    property.SetValue(control, null);
                    break;
                case 0:
                    property.SetValue(control, false);
                    break;
                default:
                    property.SetValue(control, true);
                    break;
            }
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            bool? v = (bool?)property.GetValue(control);
            int val = v.HasValue ? (v.Value ? 1 : 0) : -1;
            key.SetValue(name, val, RegistryValueKind.DWord);
        }
    }

    public struct ValueBinding
    {
        public string RegistryPath { get; set; }
        public string ValueName { get; set; }
        public RegistryValueKind ValueKind { get; set; }
        public object Control { get; set; }
        public PropertyInfo ControlProperty { get; set; }
        internal IRegistryOperator Operator { get; set; }

        public void Load(RegistryFinder finder)
        {
            RegistryKey key = finder.FindKey(RegistryPath, ValueName, false);
            if (key == null)
            {
                return;
            }
            Operator.Read(key, ValueName, Control, ControlProperty);
        }
        public void Save(RegistryFinder finder)
        {
            RegistryKey key = finder.OpenKeyAt(0, RegistryPath, true);
            if (key == null)
            {
                return;
            }
            Operator.Write(key, ValueName, Control, ControlProperty);
        }
    }
    public class ValueBindingCollection: IList<ValueBinding>
    {
        private List<ValueBinding> _list = new List<ValueBinding>();

        public ValueBinding this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
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

        public void Add(ValueBinding item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(ValueBinding item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(ValueBinding[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ValueBinding> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(ValueBinding item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, ValueBinding item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(ValueBinding item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
    public enum ValueNameStyle
    {
        Control,
        Property,
        ControlDotProperty
    }
    public partial class RegistryBinding
    {
        internal static string ReadString(RegistryKey key, string name, string defaultValue)
        {
            object ret = key.GetValue(name);
            return (ret != null) ? ret.ToString() : defaultValue;
        }
        internal static int ReadInt(RegistryKey key, string name, int defaultValue)
        {
            object ret = key.GetValue(name);
            if (ret is int)
            {
                return (int)ret;
            }
            if (ret == null)
            {
                return defaultValue;
            }
            return int.Parse(ret.ToString());
        }
        public string Path { get; set; }
        public ValueBindingCollection Items { get; } = new ValueBindingCollection();

        public void Register(string path, string name, object control, string propertyName, IRegistryOperator op)
        {
            PropertyInfo prop = control.GetType().GetProperty(propertyName);
            if (prop == null)
            {
                throw new ArgumentException(string.Format("propertyName={0} は存在しません", propertyName));
            }
            ValueBinding b = new ValueBinding()
            {
                RegistryPath = path,
                ValueName = name,
                ValueKind = op.GetValueKind(),
                Control = control,
                ControlProperty = prop,
                Operator = op
            };
            Items.Add(b);
        }
        public void Register(string path, string name, Type type, string propertyName, IRegistryOperator op)
        {
            PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
            {
                throw new ArgumentException(string.Format("propertyName={0} は存在しません", propertyName));
            }
            ValueBinding b = new ValueBinding()
            {
                RegistryPath = path,
                ValueName = name,
                ValueKind = op.GetValueKind(),
                Control = null,
                ControlProperty = prop,
                Operator = op
            };
            Items.Add(b);
        }

        public void Load(RegistryFinder finder)
        {
            List<Exception> errs = new List<Exception>();
            foreach (ValueBinding b in Items)
            {
                try
                {
                    b.Load(finder);
                }
                catch (Exception t)
                {
                    errs.Add(t);
                }
            }
            if (errs.Count != 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }
        public void Save(RegistryFinder finder)
        {
            List<Exception> errs = new List<Exception>();
            foreach (ValueBinding b in Items)
            {
                try
                {
                    b.Save(finder);
                }
                catch (Exception t)
                {
                    errs.Add(t);
                }
            }
            if (errs.Count != 0)
            {
                throw new AggregateException(errs.ToArray());
            }
        }
    }
    public class RegistryFinder
    {
        private static string GetDefaultBaseDir()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string s = Path.GetFileNameWithoutExtension(asm.Location);
            return @"\Software\Unicorn\" + s;
        }
        public RegistryPath[] SearchPaths { get; set; } = new RegistryPath[] {
            new RegistryPath(Registry.CurrentUser, GetDefaultBaseDir()),
            new RegistryPath(Registry.LocalMachine, GetDefaultBaseDir())
        };

        public RegistryKey FindKey(string path, bool writable)
        {
            List<Exception> errs = new List<Exception>();
            foreach (RegistryPath p in SearchPaths)
            {
                try
                {
                    RegistryKey ret = p.OpenKey(path, writable, false);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
                catch (ObjectDisposedException)
                {
                    continue;
                }
                catch (SecurityException t)
                {
                    errs.Add(t);
                }
                catch (Exception t)
                {
                    errs.Add(t);
                }
            }
            if (0 < errs.Count)
            {
                throw new AggregateException(errs.ToArray());
            }
            return null;
        }
        public RegistryKey FindKey(string path, string name, bool writable)
        {
            List<Exception> errs = new List<Exception>();
            foreach (RegistryPath p in SearchPaths)
            {
                try
                {
                    RegistryKey ret = p.OpenKey(path, writable, false);
                    if (ret == null)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        return ret;
                    }
                    foreach (string s in ret.GetValueNames())
                    {
                        if (string.Compare(s, name, true) == 0)
                        {
                            return ret;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    continue;
                }
                catch (SecurityException t)
                {
                    errs.Add(t);
                }
                catch (Exception t)
                {
                    errs.Add(t);
                }
            }
            if (0 < errs.Count)
            {
                throw new AggregateException(errs.ToArray());
            }
            return null;
        }

        public RegistryKey OpenKeyAt(int index, string path, bool createNew)
        {
            if (index == 0 && SearchPaths.Length == 0)
            {
                return null;
            }
            if (index < 0 || SearchPaths.Length <= index)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            RegistryKey ret = SearchPaths[index].OpenKey(path, true, createNew);
            return ret;
        }

        public object GetValue(string path, string name)
        {
            
            foreach (RegistryPath obj in SearchPaths)
            {
                RegistryKey reg = obj.OpenKey(path, false, false);
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
            RegistryPath obj = SearchPaths[index];
            return obj.OpenKey(path, true, true);
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
    }
}
