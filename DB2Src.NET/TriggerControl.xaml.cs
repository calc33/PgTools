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
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Trigger), typeof(TriggerControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(TriggerControl));

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

        private void TargetPropertyChanged(DependencyPropertyChangedEventArgs e)
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
        private void IsEditingPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (IsEditing)
            {
                Target.Backup();
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
            }
            if (e.Property == IsEditingProperty)
            {
                IsEditingPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
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
                string recoverMsg = "(修復しました)";
                string[] s = Target.GetRecoverSQL(string.Empty, string.Empty, 0, false);
                if (s != null && s.Length != 0)
                {
                    try
                    {
                        ctx.ExecSqlsWithLog(s);
                    }
                    catch (Exception t2)
                    {
                        recoverMsg = string.Format("(修復失敗: {0})", ctx.GetExceptionMessage(t2));
                    }
                }
                MessageBox.Show(string.Format("エラー: {0}{1}", ctx.GetExceptionMessage(t), recoverMsg));
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
            Window owner = App.FindVisualParent<Window>(this);
            Db2SourceContext ctx = Target.Context;
            string[] sql = ctx.GetDropSQL(Target, string.Empty, string.Empty, 0, cascade, false);
            SqlLogger logger = new SqlLogger();
            bool failed = false;
            try
            {
                ctx.ExecSqls(sql, logger.Log);
            }
            catch (Exception t)
            {
                logger.Buffer.AppendLine(ctx.GetExceptionMessage(t));
                failed = true;
            }
            string s = logger.Buffer.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(s))
            {
                if (failed)
                {
                    MessageBox.Show(owner, s, Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(owner, s, Properties.Resources.MessageBoxCaption_Result, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
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
            Window owner = App.FindVisualParent<Window>(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)Resources["messageDropTrigger"], Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            DropTarget(false);
        }
    }
}
