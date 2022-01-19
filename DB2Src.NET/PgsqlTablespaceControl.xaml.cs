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
    /// PgsqlTablespaceControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlTablespaceControl : UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlTablespace), typeof(PgsqlTablespaceControl));
        public static readonly DependencyProperty UserItemsProperty = DependencyProperty.Register("UserItems", typeof(DisplayItem[]), typeof(PgsqlTablespaceControl));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PgsqlTablespaceControl));

        public PgsqlTablespace Target
        {
            get { return (PgsqlTablespace)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public DisplayItem[] UserItems
        {
            get { return (DisplayItem[])GetValue(UserItemsProperty); }
            set { SetValue(UserItemsProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public PgsqlTablespaceControl()
        {
            InitializeComponent();
            if (App.CurrentDataSet != null)
            {
                UserItems = DisplayItem.ToDisplayItemArray(App.CurrentDataSet.Users, null, null, null);
            }
        }
    }
}
