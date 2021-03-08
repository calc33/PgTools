using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// ViewControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ViewControl: UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(View), typeof(ViewControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(ViewControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(ViewControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(ViewControl));

        public View Target
        {
            get
            {
                return (View)GetValue(TargetProperty);
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
        public DataGridController DataGridControllerResult
        {
            get
            {
                return (DataGridController)GetValue(DataGridControllerResultProperty);
            }
            set
            {
                SetValue(DataGridControllerResultProperty, value);
            }
        }
        public double DataGridResultMaxHeight
        {
            get
            {
                return (double)GetValue(DataGridResultMaxHeightProperty);
            }
            set
            {
                SetValue(DataGridResultMaxHeightProperty, value);
            }
        }
        public ViewControl()
        {
            InitializeComponent();
        }

        private void TargetChanged(DependencyPropertyChangedEventArgs e)
        {
            dataGridColumns.ItemsSource = Target.Columns;
            UpdateTextBoxSource();
            UpdateTextBoxSelectSql();
            Dispatcher.Invoke(Fetch, DispatcherPriority.Normal);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private void UpdateTextBlockWarningLimit()
        {
            bool flag = false;
            if (IsChecked(checkBoxLimitRow))
            {
                int n;
                flag = (int.TryParse(textBoxLimitRow.Text, out n) && (n <= DataGridControllerResult.Rows.Count));
            }
            textBlockWarningLimit.Visibility = flag ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateDataGridResult(IDbCommand command)
        {
            DateTime start = DateTime.Now;
            try
            {
                try
                {
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        DataGridControllerResult.Load(reader);
                    }
                }
                catch (Exception t)
                {
                    MessageBox.Show(Target.Context.GetExceptionMessage(t), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            finally
            {
                DateTime end = DateTime.Now;
                TimeSpan time = end - start;
                string s = string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
                textBlockGridResult.Text = string.Format("{0}件見つかりました。  所要時間 {1}", DataGridControllerResult.Rows.Count, s);
            }
        }

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }
        private void UpdateTextBoxSource()
        {
            if (textBoxSource == null)
            {
                return;
            }
            if (Target == null)
            {
                textBoxSource.Text = string.Empty;
                return;
            }
            Db2SourceContext ctx = Target.Context;
            try
            {
                StringBuilder buf = new StringBuilder();
                if (IsChecked(checkBoxSourceMain))
                {
                    buf.Append(ctx.GetSQL(Target, string.Empty, ";", 0, true));
                }
                if (IsChecked(checkBoxSourceComment))
                {
                    if (!string.IsNullOrEmpty(Target.CommentText))
                    {
                        buf.Append(ctx.GetSQL(Target.Comment, string.Empty, ";", 0, true));
                    }
                    foreach (Column c in Target.Columns)
                    {
                        if (!string.IsNullOrEmpty(c.CommentText))
                        {
                            buf.Append(ctx.GetSQL(c.Comment, string.Empty, ";", 0, true));
                        }
                    }
                    buf.AppendLine();
                }
                if (IsChecked(checkBoxSourceTrigger))
                {

                }
                textBoxSource.Text = buf.ToString();
            }
            catch (Exception t)
            {
                textBoxSource.Text = t.ToString();
            }
        }

        private void UpdateTextBoxSelectSql()
        {
            if (textBoxSelectSql == null)
            {
                return;
            }
            textBoxSelectSql.Text = Target.GetSelectSQL(null, string.Empty, string.Empty, null, HiddenLevel.Visible);
        }

        public void Fetch(string condition)
        {
            textBoxCondition.Text = condition;
            Fetch();
        }

        public void Fetch()
        {
            if (Target == null)
            {
                return;
            }
            Db2SourceContext ctx = Target.Context;
            if (ctx == null)
            {
                return;
            }
            int? limit = null;
            if (IsChecked(checkBoxLimitRow) && !string.IsNullOrEmpty(textBoxLimitRow.Text))
            {
                int l;
                if (!int.TryParse(textBoxLimitRow.Text, out l))
                {
                    MessageBox.Show("件数が数字ではありません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxLimitRow.Focus();
                }
                limit = l;
            }
            int offset;
            string sql = Target.GetSelectSQL(null, textBoxCondition.Text, string.Empty, limit, HiddenLevel.Visible, out offset);
            try
            {
                using (IDbConnection conn = ctx.NewConnection(true))
                {
                    using (IDbCommand cmd = ctx.GetSqlCommand(sql, null, conn))
                    {
                        UpdateDataGridResult(cmd);
                    }
                }
            }
            catch (Exception t)
            {
                ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, sql);
                Db2SrcDataSetController.ShowErrorPosition(t, textBoxCondition, ctx, offset);
            }
            finally
            {
                UpdateTextBlockWarningLimit();
            }
        }
        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }
        private void dataGridColumns_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DataGrid g = sender as DataGrid;
            double w = e.NewSize.Width;
            double h = Math.Max(g.FontSize + 4.0, e.NewSize.Height / 2.0);
            DataGridResultMaxHeight = h;
            foreach (DataGridColumn c in g.Columns)
            {
                c.MaxWidth = w;
            }
        }

        private void checkBoxSource_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private void checkBoxSource_Unhecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private static readonly Regex NumericStrRegex = new Regex("[0-9]+");
        private void textBoxLimitRow_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = NumericStrRegex.IsMatch(e.Text);
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataGridControllerResult = new DataGridController();
            DataGridControllerResult.Grid = dataGridResult;
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            List<string> sqls = new List<string>();
            if ((Target.Comment != null) && Target.Comment.IsModified())
            {
                sqls.Add(ctx.GetSQL(Target.Comment, string.Empty, string.Empty, 0, false));
            }
            for (int i = 0; i < Target.Columns.Count; i++)
            {
                Column newC = Target.Columns[i, false];
                if ((newC.Comment != null) && newC.Comment.IsModified())
                {
                    sqls.Add(ctx.GetSQL(newC.Comment, string.Empty, string.Empty, 0, false));
                }
            }
            try
            {
                if (sqls.Count != 0)
                {
                    ctx.ExecSqlsWithLog(sqls);
                }
            }
            finally
            {
                ctx.Revert(Target);
            }
            IsEditing = false;
        }

        private void buttonCopyAll_Click(object sender, RoutedEventArgs e)
        {
            DataGridCommands.CopyTable.Execute(null, dataGridResult);
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void OnTabClosed(object sender) { }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            ctx.Revert(Target);
            IsEditing = false;
        }

        private void buttonOptions_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu;
            menu = (ContextMenu)Resources["dropViewContextMenu"];
            menu.PlacementTarget = buttonOptions;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;

        }

        private void DropTarget(bool cascade)
        {
            Window owner = App.FindVisualParent<Window>(this);
            Db2SourceContext ctx = Target.Context;
            string sql = ctx.GetDropSQL(Target, string.Empty, string.Empty, 0, cascade, false);
            SqlLogger logger = new SqlLogger();
            bool failed = false;
            try
            {
                ctx.ExecSql(sql, logger.Log);
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

        private void menuItemDropView_Click(object sender, RoutedEventArgs e)
        {
            Window owner = App.FindVisualParent<Window>(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)Resources["messageDropView"], Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            MenuItem menu = sender as MenuItem;
            DropTarget((bool)menu.Tag);
        }
    }
}
