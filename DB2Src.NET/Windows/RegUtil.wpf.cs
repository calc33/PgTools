using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Db2Source
{
    partial class RegistryBinding
    {
        private static string GetElementName(FrameworkElement element)
        {
            return (element is Window || element is UserControl) ? element.GetType().Name : element.Name;
        }

        private void Register(FrameworkElement root, FrameworkElement control, string propertyName, ValueNameStyle style, IRegistryOperator op)
        {
            string cName = GetElementName(control);
            if (string.IsNullOrEmpty(cName))
            {
                throw new ArgumentException("名前のないコントロールはレジストリに保存できません");
            }
            string rPath = string.Empty;
            string cPath = cName;
            List<string> paths = new List<string>();
            FrameworkElement obj = control.Parent as FrameworkElement;
            while (obj != null && obj != root)
            {
                string name = GetElementName(obj);
                if (!string.IsNullOrEmpty(name) && (NameScope.GetNameScope(obj) != null))
                {
                    paths.Insert(0, name);
                }
                obj = obj.Parent as FrameworkElement;
            }
            if (0 < paths.Count)
            {
                rPath = paths[0];
                cPath = paths[0];
                for (int i = 1; i < paths.Count; i++)
                {
                    rPath += "\\" + paths[i];
                    cPath += "." + paths[i];
                }
                cPath += "." + cName;
            }
            string vName;
            switch (style)
            {
                case ValueNameStyle.Control:
                    vName = cPath;
                    break;
                case ValueNameStyle.Property:
                    if (root != control)
                    {
                        rPath = System.IO.Path.Combine(rPath, control.Name);
                    }
                    vName = propertyName;
                    break;
                case ValueNameStyle.ControlDotProperty:
                    vName = cPath + "." + propertyName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("style");
            }
            Register(rPath, vName, control, propertyName, op);
        }
        public void Register(Window window)
        {
            //Register(window, window, "Left", ValueNameStyle.Property, new Int32Operator());
            //Register(window, window, "Top", ValueNameStyle.Property, new Int32Operator());
            Register(window, window, "Width", ValueNameStyle.Property, new DoubleOperator());
            Register(window, window, "Height", ValueNameStyle.Property, new DoubleOperator());
            Register(string.Empty, "Maximized", window, "WindowState", new WindowStateOperator());
        }
        public void Register(FrameworkElement root, TabControl control)
        {
            Register(root, control, "SelectedIndex", ValueNameStyle.Control, new Int32Operator());
        }
        public void Register(FrameworkElement root, ComboBox control)
        {
            Register(root, control, "SelectedIndex", ValueNameStyle.Control, new Int32Operator());
        }
        public void Register(FrameworkElement root, CheckBox control)
        {
            Register(root, control, "IsChecked", ValueNameStyle.Control, new NullableBoolOperator());
        }
        public void Register(FrameworkElement root, TextBox control)
        {
            Register(root, control, "Text", ValueNameStyle.Control, new StringOperator());
        }
        public void Register(FrameworkElement root, Grid control)
        {
            Register(root, control, "RowDefinitions", ValueNameStyle.Property, new RowDefinitionCollectionOperator());
            Register(root, control, "ColumnDefinitions", ValueNameStyle.Property, new ColumnDefinitionCollectionOperator());
        }
    }

    public class RowDefinitionCollectionOperator : IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.String;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Grid grid = control as Grid;
            if (grid == null)
            {
                return;
            }
            if (grid.RowDefinitions.Count == 0)
            {
                return;
            }
            string v = RegistryBinding.ReadString(key, name, null);
            if (string.IsNullOrEmpty(v))
            {
                return;
            }
            string[] l = StringList.FromCsv(v).ToArray();
            int n = Math.Min(l.Length, grid.RowDefinitions.Count);
            GridLengthConverter converter = new GridLengthConverter();
            for (int i = 0; i < n; i++)
            {
                RowDefinition def = grid.RowDefinitions[i];
                def.Height = (GridLength)converter.ConvertFrom(l[i]);
            }
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Grid grid = control as Grid;
            if (grid == null)
            {
                return;
            }
            if (grid.RowDefinitions.Count == 0)
            {
                return;
            }
            List<string> l = new List<string>(grid.RowDefinitions.Count);
            foreach (RowDefinition def in grid.RowDefinitions)
            {
                l.Add(def.Height.ToString());
            }
            string csv = new StringList(l.ToArray()).ToCsv(false);
            key.SetValue(name, csv, RegistryValueKind.String);
        }
    }

    public class ColumnDefinitionCollectionOperator : IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.String;
        }
        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Grid grid = control as Grid;
            if (grid == null)
            {
                return;
            }
            if (grid.ColumnDefinitions.Count == 0)
            {
                return;
            }
            string v = RegistryBinding.ReadString(key, name, null);
            if (string.IsNullOrEmpty(v))
            {
                return;
            }
            string[] l = StringList.FromCsv(v).ToArray();
            int n = Math.Min(l.Length, grid.ColumnDefinitions.Count);
            GridLengthConverter converter = new GridLengthConverter();
            for (int i = 0; i < n; i++)
            {
                ColumnDefinition def = grid.ColumnDefinitions[i];
                def.Width = (GridLength)converter.ConvertFrom(l[i]);
            }
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Grid grid = control as Grid;
            if (grid == null)
            {
                return;
            }
            if (grid.ColumnDefinitions.Count == 0)
            {
                return;
            }
            List<string> l = new List<string>(grid.ColumnDefinitions.Count);
            foreach (ColumnDefinition def in grid.ColumnDefinitions)
            {
                l.Add(def.Width.ToString());
            }
            string csv = new StringList(l.ToArray()).ToCsv(false);
            key.SetValue(name, csv, RegistryValueKind.String);
        }
    }
    public class WindowStateOperator : IRegistryOperator
    {
        public RegistryValueKind GetValueKind()
        {
            return RegistryValueKind.DWord;
        }

        public void Read(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Window window = control as Window;
            if (window == null)
            {
                return;
            }
            int v = window.WindowState == WindowState.Maximized ? 1 : 0;
            v = RegistryBinding.ReadInt(key, name, v);
            window.WindowState = (v == 1) ? WindowState.Maximized : WindowState.Normal;
        }

        public void Write(RegistryKey key, string name, object control, PropertyInfo property)
        {
            Window window = control as Window;
            if (window == null)
            {
                return;
            }
            key.SetValue(name, window.WindowState == WindowState.Maximized ? 1 : 0, GetValueKind());
        }
    }
}
