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
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(View), typeof(ViewControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(ViewControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(ViewControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(ViewControl));

        private ViewSetting _setting = null;

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

        private static readonly string[] _settingControlNames = new string[] { "checkBoxDrop", "checkBoxSourceMain", "checkBoxSourceComment", "checkBoxSourceTrigger" };
        public string[] SettingCheckBoxNames { get { return _settingControlNames; } }

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

        private CompleteFieldController _dropDownController;

        public ViewControl()
        {
            InitializeComponent();
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            dataGridColumns.ItemsSource = Target?.Columns;
            _dropDownController.Target = Target;
            UpdateTextBoxSource();
            UpdateTextBoxSelectSql();
            _setting = ViewSetting.Require(Target);
            _setting?.Load(this);
            AutoFetch();
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ViewControl)?.OnTargetPropertyChanged(e);
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
            if (DataGridControllerResult == null)
            {
                return;
            }
            if (Target == null)
            {
                return;
            }
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
                    MessageBox.Show(Target.Context.GetExceptionMessage(t), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
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

        private bool _isTextBoxSourceUpdating;
        private void UpdateTextBoxSource()
        {
            try
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
                    if (IsChecked(checkBoxDrop))
                    {
                        foreach (string s in ctx.GetDropSQL(Target, true, string.Empty, ";", 0, false, true))
                        {
                            buf.Append(s);
                        }
                        buf.AppendLine();
                    }
                    if (IsChecked(checkBoxSourceMain))
                    {
                        foreach (string s in ctx.GetSQL(Target, string.Empty, ";", 0, true))
                        {
                            buf.Append(s);
                        }
                    }
                    if (IsChecked(checkBoxSourceComment))
                    {
                        if (!string.IsNullOrEmpty(Target.CommentText))
                        {
                            foreach (string s in ctx.GetSQL(Target.Comment, string.Empty, ";", 0, true))
                            {
                                buf.Append(s);
                            }
                        }
                        foreach (Column c in Target.Columns)
                        {
                            if (!string.IsNullOrEmpty(c.CommentText))
                            {
                                foreach (string s in ctx.GetSQL(c.Comment, string.Empty, ";", 0, true))
                                {
                                    buf.Append(s);
                                }
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
            finally
            {
                _isTextBoxSourceUpdating = false;
            }
        }

        private void DelayedUpdateTextBoxSource()
        {
            if (_isTextBoxSourceUpdating)
            {
                return;
            }
            _isTextBoxSourceUpdating = true;
            Dispatcher.InvokeAsync(UpdateTextBoxSource, DispatcherPriority.ApplicationIdle);
        }

        private bool _isTextBoxSelectSqlUpdating;
        private void UpdateTextBoxSelectSql()
        {
            try
            {
                if (textBoxSelectSql == null)
                {
                    return;
                }
                if (Target == null)
                {
                    return;
                }
                textBoxSelectSql.Text = Target.GetSelectSQL(null, string.Empty, string.Empty, null, HiddenLevel.Visible, MainWindow.Current.IndentOffset, 80);
            }
            finally
            {
                _isTextBoxSelectSqlUpdating = false;
            }
        }

        private void DelayedUpdateTextBoxSelectSql()
        {
            if (_isTextBoxSelectSqlUpdating)
            {
                return;
            }
            _isTextBoxSelectSqlUpdating = true;
            Dispatcher.InvokeAsync(UpdateTextBoxSelectSql, DispatcherPriority.ApplicationIdle);
        }
        private void AutoFetch()
        {
            if (!(checkBoxAutoFetch.IsChecked ?? false))
            {
                return;
            }
            // 10秒以上かかったら次回からは「起動時に検索」のチェックを外す
            DateTime timeLimit = DateTime.Now.AddSeconds(10);
            try
            {
                Fetch();
            }
            finally
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (timeLimit < DateTime.Now)
                    {
                        checkBoxAutoFetch.IsChecked = false;
                        _setting.Save(this);
                    }
                }, DispatcherPriority.ApplicationIdle);
            }
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
                    MessageBox.Show((string)Resources["messageInvalidLimitRow"], Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxLimitRow.Focus();
                }
                limit = l;
            }
            int offset;
            string sql = Target.GetSelectSQL(null, textBoxCondition.Text, string.Empty, limit, HiddenLevel.Visible, out offset, 0, 80);
            try
            {
                using (IDbConnection conn = ctx.NewConnection(true))
                {
                    using (IDbCommand cmd = ctx.GetSqlCommand(sql, null, conn))
                    {
                        try
                        {
                            UpdateDataGridResult(cmd);
                        }
                        catch (Exception t)
                        {
                            ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, cmd);
                            Db2SrcDataSetController.ShowErrorPosition(t, textBoxCondition, ctx, offset);
                        }
                    }
                }
            }
            catch (Exception t)
            {
                ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, null);
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

        private void checkBoxSource_Unchecked(object sender, RoutedEventArgs e)
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
            CommandBinding b;
            b = new CommandBinding(ApplicationCommands.Find, FindCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(QueryCommands.NormalizeSQL, textBoxConditionCommandNormalizeSql_Executed);
            textBoxCondition.CommandBindings.Add(b);
            _dropDownController = new CompleteFieldController(Target, textBoxCondition);
            MainWindow.Current.IndentPropertyChanged += MainWindow_IndentPropertyChanged;
            MainWindow.Current.IndentCharPropertyChanged += MainWindow_IndentPropertyChanged;
            MainWindow.Current.IndentOffsetPropertyChanged += MainWindow_IndentPropertyChanged;
        }

        private void MainWindow_IndentPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateTextBoxSource();
            DelayedUpdateTextBoxSelectSql();
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            Db2SourceContext ctx = Target.Context;
            List<string> sqls = new List<string>();
            if ((Target.Comment != null) && Target.Comment.IsModified)
            {
                sqls.AddRange(ctx.GetSQL(Target.Comment, string.Empty, string.Empty, 0, false));
            }
            for (int i = 0; i < Target.Columns.Count; i++)
            {
                Column newC = Target.Columns[i, false];
                if ((newC.Comment != null) && newC.Comment.IsModified)
                {
                    sqls.AddRange(ctx.GetSQL(newC.Comment, string.Empty, string.Empty, 0, false));
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

        private void textBoxConditionCommandNormalizeSql_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            textBoxCondition.Text = Target.Context.NormalizeSQL(textBoxCondition.Text);
            e.Handled = true;
        }

        private void FindCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridControllerResult.ShowSearchWinodow();
        }

        private void FindNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridControllerResult.SearchGridTextForward();
        }

        private void FindPreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridControllerResult.SearchGridTextBackward();
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
            _setting.Save(this);
        }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
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
            if (Target == null)
            {
                return;
            }
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

        private void menuItemDropView_Click(object sender, RoutedEventArgs e)
        {
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)Resources["messageDropView"], Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            MenuItem menu = sender as MenuItem;
            DropTarget((bool)menu.Tag);
        }

        private void buttonRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            Target?.Context?.Refresh(Target);
        }

        private void dataGridTrigger_LayoutUpdated(object sender, EventArgs e)
        {
            if (Target == null || Target.Triggers.Count == 0)
            {
                return;
            }
            if (listBoxTrigger.SelectedItem != null)
            {
                return;
            }
            listBoxTrigger.SelectedItem = Target.Triggers[0];
        }

        private SelectColumnWindow _selectColumnWindow = null;
        private object _selectColumnWindowLock = new object();
        private SelectColumnWindow GetColumnFilterWindow()
        {
            if (_selectColumnWindow != null)
            {
                return _selectColumnWindow;
            }
            lock (_selectColumnWindowLock)
            {
                if (_selectColumnWindow != null)
                {
                    return _selectColumnWindow;
                }
                _selectColumnWindow = new SelectColumnWindow();

                _selectColumnWindow.Owner = Window.GetWindow(this);
                return _selectColumnWindow;
            }
        }

        private void buttonFilterColumns_Click(object sender, RoutedEventArgs e)
        {
            SelectColumnWindow w = GetColumnFilterWindow();
            if (w.IsVisible)
            {
                return;
            }
            w.Grid = dataGridResult;
            w.SelectedColumn = w.Grid.CurrentColumn;
            WindowLocator.LocateNearby(buttonFilterColumns, w, NearbyLocation.DownLeft);
            w.Closed += ColumnFilterWindow_Closed;
            w.Show();
        }

        private void ColumnFilterWindow_Closed(object sender, EventArgs e)
        {
            _selectColumnWindow = null;
        }

        private void buttonSearchWord_Click(object sender, RoutedEventArgs e)
        {
            ApplicationCommands.Find.Execute(null, dataGridResult);
        }
    }
}
