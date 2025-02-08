using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

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

        private QueryFaith _fetchingFaith = QueryFaith.Idle;
        private readonly object _fetchingFaithLock = new object();
        private DispatcherTimer _fetcingCooldownTimer = null;
        private CancellationTokenSource _fetchingCancellation = null;

        private void UpdateButtonFetch()
        {
            buttonFetch.IsEnabled = (_fetchingFaith != QueryFaith.Startup);
            if (_fetchingFaith != QueryFaith.Abortable)
            {
                buttonFetch.ContentTemplate = (DataTemplate)FindResource("ImageSearch20");
            }
            else
            {
                buttonFetch.ContentTemplate = (DataTemplate)FindResource("ImageAbort20");
            }
        }

        private void FetchingCooldownTimer_Timer(object sender, EventArgs e)
        {
            _fetcingCooldownTimer?.Stop();
            _fetcingCooldownTimer = null;
            EndStartupFetching();
        }

        /// <summary>
        /// クエリ実行開始状態にする(_fetchingFaith: Idle → Startup)
        /// クエリ開始後1秒間は中断不可
        /// </summary>
        private void StartFetching()
        {
            lock (_fetchingFaithLock)
            {
                if (_fetchingFaith != QueryFaith.Idle)
                {
                    return;
                }
                _fetchingFaith = QueryFaith.Startup;
                _fetchingCancellation?.Dispose();
                _fetchingCancellation = new CancellationTokenSource();
            }
            UpdateButtonFetch();
            _fetcingCooldownTimer?.Stop();
            _fetcingCooldownTimer = new DispatcherTimer(TimeSpan.FromSeconds(1.0), DispatcherPriority.Normal, FetchingCooldownTimer_Timer, Dispatcher);
            _fetcingCooldownTimer.Start();
        }

        /// <summary>
        /// クエリ中断可にする(クエリ実行開始後1秒間は中断不可)
        /// _fetchingFaith: Startup → Abortable
        /// </summary>
        private void EndStartupFetching()
        {
            lock (_fetchingFaithLock)
            {
                if (_fetchingFaith != QueryFaith.Startup)
                {
                    return;
                }
                _fetchingFaith = QueryFaith.Abortable;
            }
            Dispatcher.InvokeAsync(UpdateButtonFetch);
        }

        /// <summary>
        /// クエリの実行を中断(_fetchingFaith: Abortableの時のみ中断可能)
        /// </summary>
        private void AbortFetching()
        {
            if (_fetchingFaith != QueryFaith.Abortable)
            {
                return;
            }
            _fetchingCancellation?.Cancel();
            //CurrentDataSet.AbortQuery(cmd);
        }

        /// <summary>
        /// クエリの実行が終了した(_fetchingFaith: Startup/Abortable→Ilde)
        /// </summary>
        private void EndFetching()
        {
            lock (_fetchingFaithLock)
            {
                _fetchingFaith = QueryFaith.Idle;

                _fetchingCancellation?.Dispose();
                _fetchingCancellation = null;

                _fetcingCooldownTimer?.Stop();
                _fetcingCooldownTimer = null;
            }
            Dispatcher.InvokeAsync(UpdateButtonFetch);
            _inAutoFetching = false;
        }

        private async Task ExecuteCommandAsync(Dispatcher dispatcher, Db2SourceContext dataSet, DataGridController controller, IDbCommand command)
        {
            DateTime start = DateTime.Now;
            try
            {
                using (IDataReader reader = await dataSet.ExecuteReaderAsync(command, _fetchingCancellation.Token))
                {
                    await controller.LoadAsync(dispatcher, reader, Target, _fetchingCancellation.Token);
                }
            }
            catch (Exception t)
            {
                await dispatcher.InvokeAsync(() =>
                {
                    dataSet.OnLog(dataSet.GetExceptionMessage(t), LogStatus.Error, command);
                });
                return;
            }
            finally
            {
                DateTime end = DateTime.Now;
                TimeSpan time = end - start;
                await dispatcher.InvokeAsync(() =>
                {
                    string s = string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
                    textBlockGridResult.Text = string.Format("{0}件見つかりました。  所要時間 {1}", controller.Rows.Count, s);
                });
                command.Dispose();
            }
        }

        private async Task ExecuteSqlAsync(Dispatcher dispatcher, Db2SourceContext dataSet, DataGridController controller, string sql, int offset)
        {
            DateTime start = DateTime.Now;
            try
            {
                using (IDbConnection conn = dataSet.NewConnection(true, 0))
                {
                    using (IDbCommand cmd = dataSet.GetSqlCommand(sql, null, conn))
                    {
                        try
                        {
                            await ExecuteCommandAsync(dispatcher, dataSet, controller, cmd);
                        }
                        catch (Exception t)
                        {
                            await dispatcher.InvokeAsync(() =>
                            {
                                dataSet.OnLog(dataSet.GetExceptionMessage(t), LogStatus.Error, cmd);
                                Db2SrcDataSetController.ShowErrorPosition(t, textBoxCondition, dataSet, offset);
                            });
                        }
                    }
                }
            }
            catch (Exception t)
            {
                await dispatcher.InvokeAsync(() =>
                {
                    dataSet.OnLog(dataSet.GetExceptionMessage(t), LogStatus.Error, null);
                });
            }
            finally
            {
                await dispatcher.InvokeAsync(UpdateTextBlockWarningLimit);
                if (_inAutoFetching)
                {
                    DateTime end = DateTime.Now;
                    TimeSpan time = end - start;
                    // 「起動時に検索」で10秒以上かかったら次回からは「起動時に検索」のチェックを外す
                    if (10 < time.TotalSeconds || _fetchingCancellation.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            checkBoxAutoFetch.IsChecked = false;
                            _setting.Save(this);
                        });
                    }
                }
                EndFetching();
                GC.Collect(0);
            }
        }

        private bool _inAutoFetching = false;

        private void AutoFetch()
        {
            if (!(checkBoxAutoFetch.IsChecked ?? false))
            {
                return;
            }
            _inAutoFetching = true;
            Fetch();
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
                    MessageBox.Show((string)FindResource("messageInvalidLimitRow"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxLimitRow.Focus();
                }
                limit = l;
            }
            int offset;
            string sql = Target.GetSelectSQL(null, textBoxCondition.Text, string.Empty, limit, HiddenLevel.Visible, out offset, 0, 80);
            StartFetching();
            Dispatcher dispatcher = Dispatcher;
            DataGridController controller = DataGridControllerResult;
            Task _ = ExecuteSqlAsync(dispatcher, ctx, controller, sql, offset);
        }
        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            switch (_fetchingFaith)
            {
                case QueryFaith.Idle:
                    Fetch();
                    break;
                case QueryFaith.Startup:
                    break;
                case QueryFaith.Abortable:
                    AbortFetching();
                    break;
                default:
                    break;
            }
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
            menu = (ContextMenu)FindResource("dropViewContextMenu");
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
            MessageBoxResult ret = MessageBox.Show(owner, (string)FindResource("messageDropView"), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
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
