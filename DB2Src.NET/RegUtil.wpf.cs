using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Unicorn.Utility
{
    partial class RegistryBinding
    {
        private void Register(FrameworkElement root, FrameworkElement control, RegistryValueKind kind, string propertyName, ValueNameStyle style, IRegistryOperator op)
        {
            if (string.IsNullOrEmpty(control.Name))
            {
                throw new ArgumentException("名前のないコントロールはレジストリに保存できません");
            }
            string rPath = string.Empty;
            string cPath = control.Name;
            List<string> paths = new List<string>();
            FrameworkElement obj = control.Parent as FrameworkElement;
            while (obj != null && obj != root)
            {
                if (!string.IsNullOrEmpty(obj.Name) && (NameScope.GetNameScope(obj) != null))
                {
                    paths.Insert(0, obj.Name);
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
                cPath += "." + control.Name;
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
            ValueBinding b = new ValueBinding()
            {
                RegistryPath = rPath,
                ValueName = vName,
                ValueKind = kind,
                Control = control,
                ControlProperty = control.GetType().GetProperty(propertyName),
                Operator = op
            };
            Items.Add(b);
        }
        public void Register(Window window)
        {
            Register(window, window, RegistryValueKind.DWord, "Left", ValueNameStyle.Property, new Int32Operator());
            Register(window, window, RegistryValueKind.DWord, "Top", ValueNameStyle.Property, new Int32Operator());
            Register(window, window, RegistryValueKind.DWord, "Width", ValueNameStyle.Property, new Int32Operator());
            Register(window, window, RegistryValueKind.DWord, "Height", ValueNameStyle.Property, new Int32Operator());
        }
        public void Register(FrameworkElement root, TabControl control)
        {
            Register(root, control, RegistryValueKind.DWord, "SelectedIndex", ValueNameStyle.Control, new Int32Operator());
        }
        public void Register(FrameworkElement root, ComboBox control)
        {
            Register(root, control, RegistryValueKind.DWord, "SelectedIndex", ValueNameStyle.Control, new Int32Operator());
        }
        public void Register(FrameworkElement root, CheckBox control)
        {
            Register(root, control, RegistryValueKind.DWord, "IsChecked", ValueNameStyle.Control, new NullableBoolOperator());
        }
    }
}
