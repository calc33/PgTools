using System;
using System.Collections.Generic;
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
    /// TriggerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TriggerControl : UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Trigger), typeof(TriggerControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(TriggerControl), new PropertyMetadata(new PropertyChangedCallback(OnIsEditingPropertyChanged)));

        public Trigger Target
        {
            get
            {
                return (Trigger)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

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

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Trigger t = Target;
            checkBoxInsertTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Insert) != 0);
            checkBoxDeleteTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Delete) != 0);
            checkBoxTruncateTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Truncate) != 0);
            checkBoxUpdateTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Update) != 0);
            if (t != null && t.Procedure != null)
            {
                StringBuilder buf = new StringBuilder();
                foreach (string s in t.Context.GetSQL(t.Procedure, string.Empty, string.Empty, 0, true))
                {
                    buf.Append(s);
                }
                textBoxTriggerBodySQL.Text = buf.ToString();
            }
            else
            {
                textBoxTriggerBodySQL.Text = string.Empty;
            }
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TriggerControl)?.OnTargetPropertyChanged(e);
        }

        private void OnIsEditingPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (IsEditing)
            {
                Target.Backup(false);
            }
        }

        private static void OnIsEditingPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TriggerControl)?.OnIsEditingPropertyChanged(e);
        }

        private void buttonApplyTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                IsEditing = false;
                return;
            }
            Db2SourceContext ctx = Target.Context;
            string[] sqls = Target.GetAlterSQL(string.Empty, string.Empty, 0, false);
            if (sqls == null || sqls.Length == 0)
            {
                return;
            }
            try
            {
                ctx.ExecSqlsWithLog(sqls);
            }
            catch (Exception t)
            {
                string recoverMsg = (string)Resources["messageRecovered"];
                string[] s = Target.GetRecoverSQL(string.Empty, string.Empty, 0, false);
                if (s != null && s.Length != 0)
                {
                    try
                    {
                        ctx.ExecSqlsWithLog(s);
                    }
                    catch (Exception t2)
                    {
                        recoverMsg = string.Format((string)Resources["messageRecoveryFailed"], ctx.GetExceptionMessage(t2));
                    }
                }
                Window owner = Window.GetWindow(this);
                MessageBox.Show(owner, string.Format((string)Resources["messageFailed"], ctx.GetExceptionMessage(t), recoverMsg), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public TriggerControl()
        {
            InitializeComponent();
        }

        private void buttonRevertTrigger_Click(object sender, RoutedEventArgs e)
        {
            Target.Restore();
            IsEditing = false;
        }

        private void buttonDropTrigger_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = (ContextMenu)Resources["contextMenuDropTrigger"];
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
        }

        public void DropTarget(bool cascade)
        {
            Window owner = Window.GetWindow(this);
            Db2SourceContext ctx = Target.Context;
            string[] sql = ctx.GetDropSQL(Target, true, string.Empty, string.Empty, 0, cascade, false);
            StringBuilderLogger logger = new StringBuilderLogger();
            bool failed = false;
            try
            {
                ctx.ExecSqls(sql, logger.Log);
            }
            catch (Exception t)
            {
                logger.LogException(t, ctx);
                failed = true;
            }
            logger.ShowLogByMessageBox(owner, failed);
            if (failed)
            {
                return;
            }
            TabItem tab = App.FindLogicalParent<TabItem>(this);
            if (tab != null)
            {
                (tab.Parent as TabControl).Items.Remove(tab);
                Target.Release();
                MainWindow.Current.FilterTreeView(true);
            }
        }

        private void menuItemDropTrigger_Click(object sender, RoutedEventArgs e)
        {
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, string.Format((string)Resources["messageDropTrigger"], Target.Name), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            DropTarget(false);
        }
    }
}
