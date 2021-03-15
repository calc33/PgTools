using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// PgsqlTypeControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlTypeControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(PgsqlTypeControl));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Type_), typeof(PgsqlTypeControl));

        public bool IsEditing
        {
            get
            {
                return (bool)GetValue(IsEditingProperty);
            }
            set
            {
                SetValue(IsEditingProperty, value);
            }
        }

        public Type_ Target
        {
            get
            {
                return (Type_)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        SchemaObject ISchemaObjectControl.Target
        {
            get
            {
                return (SchemaObject)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }
        public string SelectedTabKey
        {
            get
            {
                return (tabControlMain.SelectedItem as TabItem)?.Header?.ToString();
            }
            set
            {
                foreach (TabItem item in tabControlMain.Items)
                {
                    if (item.Header.ToString() == value)
                    {
                        tabControlMain.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        public PgsqlTypeControl()
        {
            InitializeComponent();
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSource_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSource_Unhecked(object sender, RoutedEventArgs e)
        {

        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void OnTabClosed(object sender) { }
    }
}
