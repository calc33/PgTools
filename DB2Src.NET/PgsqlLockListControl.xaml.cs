using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// PgsqlLockListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlLockListControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(LockList), typeof(PgsqlLockListControl));
        public static readonly DependencyProperty WaitSinceProperty = DependencyProperty.Register("WaitSince", typeof(DateTime?), typeof(PgsqlLockListControl));

        public LockList Target
        {
            get
            {
                return (LockList)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        public DateTime? WaitSince
        {
            get
            {
                return (DateTime?)GetValue(WaitSinceProperty);
            }
            set
            {
                SetValue(WaitSinceProperty, value);
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
                Target = value as LockList;
            }
        }

        public string SelectedTabKey { get { return string.Empty; } set { } }

        public string[] SettingCheckBoxNames { get { return StrUtil.EmptyStringArray; } }

        public ILock[] Locks { get; private set; }

        public PgsqlLockListControl()
        {
            InitializeComponent();
        }

        private void AddSubNode(ItemCollection items, NpgsqlLockInfo info, DataTemplate template, ref DateTime? waitSince)
        {
            if (info.WaitSince.HasValue && (!waitSince.HasValue || info.WaitSince.Value < waitSince.Value))
            {
                waitSince = info.WaitSince.Value;
            }
            TreeViewItem item = new TreeViewItem()
            {
                HeaderTemplate = template,
                Header = info
            };
            items.Add(item);
            if (info?.Waited is null)
            {
                return;
            }
            foreach (NpgsqlLockInfo lockInfo in info.Waited)
            {
                AddSubNode(item.Items, lockInfo, template, ref waitSince);
            }
            item.IsExpanded = true;
        }

        private void UpdateTreeViewLocks()
        {
            DateTime? waitSince = null;
            DataTemplate tmpl = (DataTemplate)FindResource("TreeViewItemTemplateLockInfo");
            treeViewLocks.Items.Clear();
            foreach (NpgsqlLockInfo info in Locks)
            {
                AddSubNode(treeViewLocks.Items, info, tmpl, ref waitSince);
            }
            WaitSince = waitSince;
        }

        private void Refresh()
        {
            if (!(Target is null))
            {
                using (IDbConnection conn = Target.Context.NewConnection(true))
                {
                    Locks = Target?.GetLocks(conn);
                }
            }
            UpdateTreeViewLocks();
        }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu ctx = buttonStop.ContextMenu;
            ctx.PlacementTarget = buttonStop;
            ctx.Placement = PlacementMode.Bottom;
            ctx.IsOpen = true;
        }

        private void KillSessionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(treeViewLocks.SelectedItem is null);
        }

        private void KillSessionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NpgsqlLockInfo lockInfo = (treeViewLocks.SelectedItem as TreeViewItem)?.Header as NpgsqlLockInfo;
            ISession session = lockInfo?.Session;
            if (session is null)
            {
                return;
            }
            string msg = string.Format(Properties.Resources.MessageBoxText_TerminateSession, session.ToString());
            MessageBoxResult result = MessageBox.Show(msg, Properties.Resources.MessageBoxCaption_TerminateSession, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            using (IDbConnection conn = Target.Context.NewConnection(true))
            {
                session.Kill(conn);
            }
        }

        private void AbortQueryCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(treeViewLocks.SelectedItem is null);
        }

        private void AbortQueryCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NpgsqlLockInfo lockInfo = (treeViewLocks.SelectedItem as TreeViewItem)?.Header as NpgsqlLockInfo;
            ISession session = lockInfo?.Session;
            if (session is null)
            {
                return;
            }
            string msg = string.Format(Properties.Resources.MessageBoxText_AbortQuery, session.ToString());
            MessageBoxResult result = MessageBox.Show(msg, Properties.Resources.MessageBoxCaption_AbortQuery, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            using (IDbConnection conn = Target.Context.NewConnection(true))
            {
                session.AbortQuery(conn);
            }
        }
    }
    public class DurationTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] texts = (parameter as string ?? "時間,分,秒").Split(',');
            if (!(value is DateTime time))
            {
                return null;
            }
            TimeSpan duration = DateTime.Now - time;
            if (duration.TotalSeconds < 60)
            {
                return string.Format("{0}{1}}", (int)duration.TotalSeconds, texts[2]);
            }
            if (duration.TotalMinutes < 10)
            {
                return string.Format("{0}{1}{2}{3}", (int)duration.TotalMinutes, texts[1], duration.Seconds, texts[2]);
            }
            if (duration.TotalHours < 1)
            {
                return string.Format("{0}{1}", (int)duration.TotalMinutes, texts[1]);
            }
            if (duration.TotalHours < 2)
            {
                return string.Format("{0}{1}{2}{3}", (int)duration.TotalHours, texts[0], duration.Minutes, texts[1]);
            }
            return string.Format("{0}{1}", (int)duration.TotalHours, texts[0]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
