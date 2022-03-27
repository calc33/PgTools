using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Db2Source
{
    public class MovableTabItem: TabItem
    {
        public static readonly DependencyProperty IsMovingProperty = DependencyProperty.Register("IsMoving", typeof(bool), typeof(MovableTabItem));
        public bool IsMoving
        {
            get
            {
                return (bool)GetValue(IsMovingProperty);
            }
            set
            {
                SetValue(IsMovingProperty, value);
            }
        }
        public static TabItem NewTabItem(TabControl parent, string header, UIElement element, Style tabItemStyle)
        {
            MovableTabItem item = new MovableTabItem();
            item.Content = element;
            item.Header = new TextBlock() { Text = header };
            item.Style = tabItemStyle;
            parent.Items.Add(item);
            return item;
        }

        private static Dictionary<Type, Type> _schemaObjectToControl = new Dictionary<Type, Type>();

        private static object TabItemLock = new object();
        public static TabItem RequireTabItem(SchemaObject target, Style tabItemStyle, TabControl tabControl, Db2SourceContext dataSet)
        {
            ISchemaObjectWpfControl ctrl = target.Control as ISchemaObjectWpfControl;
            if (ctrl != null)
            {
                ctrl.Target = dataSet.Refresh(target);
                return ctrl.Parent as TabItem;
            }

            lock (TabItemLock)
            {
                if (ctrl != null)
                {
                    return ctrl.Parent as TabItem;
                }
                ctrl = NewControl(target, tabControl);
                if (ctrl == null)
                {
                    return null;
                }
                TabItem item = NewTabItem(tabControl, target.FullName, ctrl as UIElement, tabItemStyle);
                item.Tag = target;
                return item;
            }
        }
        public static void RegisterSchemaObjectControl(Type schemaObjectClass, Type controlClass)
        {
            if (!schemaObjectClass.IsSubclassOf(typeof(SchemaObject)) && schemaObjectClass != typeof(SchemaObject))
            {
                throw new ArgumentException("schemaObjectClassはSchemaObjectを継承していません");
            }
            if (!typeof(ISchemaObjectWpfControl).IsAssignableFrom(controlClass))
            {
                throw new ArgumentException("controlClassがIWpfSchemaObjectControlを実装していません");
            }
            _schemaObjectToControl[schemaObjectClass] = controlClass;
        }
        public static void UnregisterSchemaObjectControl(Type schemaObjectClass)
        {
            _schemaObjectToControl.Remove(schemaObjectClass);
        }

        public static ISchemaObjectWpfControl NewControl(SchemaObject target, TabControl parent)
        {
            Type t;
            if (!_schemaObjectToControl.TryGetValue(target.GetType(), out t))
            {
                return null;
            }
            ISchemaObjectWpfControl ret = t.GetConstructor(new Type[0]).Invoke(null) as ISchemaObjectWpfControl;
            if (ret == null)
            {
                return null;
            }
            ret.Target = target;
            target.Control = ret;
            return ret;
        }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        ~MovableTabItem() { }
    }
}
