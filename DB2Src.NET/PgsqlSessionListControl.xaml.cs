using System;
using System.Collections.Generic;
using System.Data;
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
    /// PgsqlSessionListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlSessionListControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(SessionList), typeof(PgsqlSessionListControl));

        public SessionList Target
        {
            get
            {
                return (SessionList)GetValue(TargetProperty);
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
                return Target;
            }
            set
            {
                Target = value as SessionList;
            }
        }
        public string SelectedTabKey { get { return string.Empty; } set { } }

        public PgsqlSessionListControl()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            dataGridSessionList.ItemsSource = Target?.GetSessions();
        }

        private void ButtonTerminate_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            DataGridCell cell = App.FindLogicalParent<DataGridCell>(sender as DependencyObject);
            NpgsqlSession obj = cell.DataContext as NpgsqlSession;
            if (obj == null)
            {
                return;
            }
            MessageBoxResult ret = MessageBox.Show(string.Format("セッション(Pid={0})を強制切断します。よろしいですか?", obj.Pid), "強制切断", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            using (IDbConnection conn = Target.Context.NewConnection(true))
            {
                obj.Kill(conn);
            }
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void OnTabClosed(object sender) { }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
