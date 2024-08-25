using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
using static Db2Source.QueryHistory;

namespace Db2Source
{
    /// <summary>
    /// QueryControl.xaml の相互作用ロジック
    /// </summary>
    public partial class QueryControl: UserControl, IRegistryStore
    {
        public static readonly DependencyProperty CurrentDataSetProperty = DependencyProperty.Register("CurrentDataSet", typeof(Db2SourceContext), typeof(QueryControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(QueryControl));
        public static readonly DependencyProperty IsQueryEditableProperty = DependencyProperty.Register("IsQueryEditable", typeof(bool), typeof(QueryControl), new PropertyMetadata(true));

        private ParameterStoreCollection _parameters = new ParameterStoreCollection();
        private void UpdateDataGridParameters()
        {
            dataGridParameters.ItemsSource = null;
            dataGridParameters.ItemsSource = _parameters;
            dataGridParameters.Visibility = _parameters.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            splitterParameters.Visibility = dataGridParameters.Visibility;
            ColumnDefinition c1 = gridSql.ColumnDefinitions[1];
            if (dataGridParameters.Visibility == Visibility.Visible)
            {
                if (c1.Tag is GridLength length)
                {
                    c1.Width = length;
                    c1.Tag = null;
                }
            }
            else
            {
                if (c1.Width.IsAbsolute)
                {
                    c1.Tag = c1.Width;
                    c1.Width = new GridLength(0, GridUnitType.Auto);
                }
            }
        }

        public ParameterStoreCollection Parameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters = value;
                UpdateDataGridParameters();
            }
        }
        public Db2SourceContext CurrentDataSet
        {
            get { return (Db2SourceContext)GetValue(CurrentDataSetProperty); }
            set { SetValue(CurrentDataSetProperty, value); }
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

        public bool IsQueryEditable
        {
            get { return (bool)GetValue(IsQueryEditableProperty); }
            set { SetValue(IsQueryEditableProperty, value); }
        }

        private RegistryBinding _registryBinding = null;
        private void RequireRegistryBinding()
        {
            if (_registryBinding != null)
            {
                return;
            }
			Window window = Window.GetWindow(this);
			_registryBinding = new RegistryBinding(window);
            _registryBinding.Register(window, gridVertical);
            _registryBinding.Register(window, gridSql);
        }
        public RegistryBinding RegistryBinding
        {
            get
            {
                RequireRegistryBinding();
                return _registryBinding;
            }
        }

        public void LoadFromRegistry()
        {
            RegistryBinding.Load(App.RegistryFinder);
        }

        public void SaveToRegistry()
        {
            RegistryBinding.Save(App.RegistryFinder);
        }

        public string GetTabItemHeader(int index)
        {
            string s = (string)FindResource("tabItemHeader");
            if (index != 0)
            {
                s += " " + index.ToString();
            }
            return s;
        }

        public QueryControl()
        {
            InitializeComponent();
            dataGridParameters.Visibility = Visibility.Collapsed;
            splitterParameters.Visibility = Visibility.Collapsed;
            dataGridColumnDbType.ItemsSource = DbTypeInfo.DbTypeInfos;
        }
        private LogListBoxItem NewLogListBoxItem(string text, QueryHistory.Query query, LogStatus status, bool notice, Tuple<int, int> errorPos)
        {
            LogListBoxItem item = new LogListBoxItem
            {
                Time = DateTime.Now,
                Status = status,
                Message = text,
                Query = query,
                ErrorPosition = errorPos
            };
            item.SetBinding(LogListBoxItem.IsQueryEditableProperty, new Binding("IsQueryEditable") { Source = this });
            return item;
        }
        private ErrorListBoxItem NewErrorListBoxItem(string text, Tuple<int, int> errorPos)
        {
            ErrorListBoxItem item = new ErrorListBoxItem
            {
                Message = text,
                ErrorPosition = errorPos
            };
            return item;
        }

        private void AddLog(string text, QueryHistory.Query query, LogStatus status, bool notice, Tuple<int, int> errorPos = null)
        {
            LogListBoxItem item = NewLogListBoxItem(text, query, status, notice, errorPos);
            item.RedoSql += Item_RedoSql;
            listBoxLog.Items.Add(item);
            listBoxLog.SelectedItem = item;
            listBoxLog.ScrollIntoView(item);
            if (notice)
            {
                tabControlResult.SelectedItem = tabItemLog;
            }
            if (status == LogStatus.Error && errorPos != null)
            {
                ErrorListBoxItem err = NewErrorListBoxItem(text, errorPos);
                err.MouseDoubleClick += ListBoxErrors_MouseDoubleClick;
                listBoxErrors.Items.Add(err);
                listBoxErrors.SelectedItem = err;
                listBoxErrors.ScrollIntoView(err);
                listBoxErrors.Visibility = Visibility.Visible;
            }
        }

        private void ListBoxErrors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ErrorListBoxItem item))
            {
                return;
            }
            if (item.ErrorPosition == null)
            {
                return;
            }
            textBoxSql.Select(item.ErrorPosition.Item1, item.ErrorPosition.Item2);
            int l = textBoxSql.GetLineIndexFromCharacterIndex(item.ErrorPosition.Item1);
            textBoxSql.ScrollToLine(l);
            textBoxSql.Focus();

        }

        private void Item_RedoSql(object sender, EventArgs e)
        {
            LogListBoxItem item = sender as LogListBoxItem;
            string sql = item.Query?.SqlText;
            if (string.IsNullOrEmpty(sql))
            {
                return;
            }
            textBoxSql.Text = sql;
            Parameters = ParameterStoreCollection.GetParameterStores(item.Query, Parameters, out _);
            //Fetch();
        }

        private void Command_Log(object sender, LogEventArgs e)
        {
            AddLog(e.Text, new QueryHistory.Query(e.Command), e.Status, false);
        }

        private QueryFaith _fetchingFaith = QueryFaith.Idle;
        private readonly object _fetchingFaithLock = new object();
        private DispatcherTimer _fetcingCooldownTimer = null;
        private CancellationTokenSource _fetchingCancellation = null;

        private void UpdateControlsIsEnabled()
        {
            buttonFetch.IsEnabled = (_fetchingFaith != QueryFaith.Startup);
            if (_fetchingFaith != QueryFaith.Abortable)
            {
                buttonFetch.ContentTemplate = (DataTemplate)FindResource("ImageExec20");
            }
            else
            {
                buttonFetch.ContentTemplate = (DataTemplate)FindResource("ImageAbort20");
            }
            IsQueryEditable = (_fetchingFaith == QueryFaith.Idle);
        }

        private void FetchingCooldownTimer_Timer(object sender, EventArgs e)
        {
            if (_fetcingCooldownTimer != null)
            {
                _fetcingCooldownTimer.Stop();
                _fetcingCooldownTimer = null;
            }
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
            UpdateControlsIsEnabled();
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
            Dispatcher.InvokeAsync(UpdateControlsIsEnabled);
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
            }
            Dispatcher.InvokeAsync(UpdateControlsIsEnabled);
        }

        private async Task ExecuteSqlPartsAsync(Dispatcher dispatcher, Db2SourceContext ctx, DataGridController controller, SQLParts sqls)
        {
            try
            {
                await dispatcher.InvokeAsync(() =>
                {
                    controller.Grid.ItemsSource = null;
                });
                using (IDbConnection conn = ctx.NewConnection(true, 0))
                {
                    bool aborted = false;
                    await dispatcher.InvokeAsync(() =>
                    {
                        Parameters = ParameterStoreCollection.GetParameterStores(sqls.ParameterNames, Parameters, out bool modified);
                        if (modified)
                        {
                            aborted = true;
                            return;
                        }

                        foreach (ParameterStore p in Parameters)
                        {
                            if (p.IsError)
                            {
                                dataGridParameters.Focus();
                                dataGridParameters.SelectedItem = p;
                                Window owner = Window.GetWindow(this);
                                MessageBox.Show(owner, string.Format((string)FindResource("messageInvalidParameter"), p.ParameterName), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                                aborted = true;
                                return;
                            }
                        }
                    });
                    if (aborted)
                    {
                        return;
                    }

                    foreach (SQLPart sql in sqls.Items)
                    {
                        if (!sql.IsExecutable)
                        {
                            continue;
                        }
                        using (IDbCommand cmd = ctx.GetSqlCommand(sql.SQL, Command_Log, conn))
                        {
                            ParameterStoreCollection stores = null;
                            await Dispatcher.InvokeAsync(() =>
                            {
                                stores = ParameterStoreCollection.GetParameterStores(cmd, Parameters, out _);
                            });
                            DateTime start = DateTime.Now;
                            Query history = new Query(cmd);
                            try
                            {
                                using (IDataReader reader = await ctx.ExecuteReaderAsync(cmd, _fetchingCancellation.Token))
                                {
                                    await controller.LoadAsync(dispatcher, reader, _fetchingCancellation.Token);
                                    if (0 <= reader.RecordsAffected)
                                    {
                                        await dispatcher.InvokeAsync(() =>
                                        {
                                            AddLog(string.Format((string)FindResource("messageRowsAffected"), reader.RecordsAffected), history, LogStatus.Normal, true);
                                        });
                                    }
                                    else if (0 < reader.FieldCount)
                                    {
                                        tabControlResult.SelectedItem = tabItemDataGrid;
                                    }
                                }
                                ctx.History.AddHistory(history);
                            }
                            catch (OperationAbortedException)
                            {
                                await dispatcher.InvokeAsync(() =>
                                {
                                    AddLog((string)FindResource("messageQueryAborted"), history, LogStatus.Error, true);
                                });
                            }
                            catch (OperationCanceledException)
                            {
                                await dispatcher.InvokeAsync(() =>
                                {
                                    AddLog((string)FindResource("messageQueryAborted"), history, LogStatus.Error, true);
                                });
                            }
                            catch (Exception t)
                            {
                                await dispatcher.InvokeAsync(() =>
                                {
                                    Tuple<int, int> errPos = CurrentDataSet.GetErrorPosition(t, sql.SQL, 0);
                                    AddLog(CurrentDataSet.GetExceptionMessage(t), history, LogStatus.Error, true, errPos);
                                    App.LogException(t);
                                    Db2SrcDataSetController.ShowErrorPosition(t, textBoxSql, CurrentDataSet, sql.Offset);
                                });
                                return;
                            }
                            finally
                            {
                                DateTime end = DateTime.Now;
                                TimeSpan time = end - start;
                                string s = string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
                                await dispatcher.InvokeAsync(() =>
                                {
                                    ParameterStoreCollection.GetParameterStores(cmd, stores, out _);
                                    UpdateDataGridParameters();
                                    AddLog(string.Format((string)FindResource("messageExecuted"), s), history, LogStatus.Aux, false);
                                    textBlockGridResult.Text = string.Format((string)FindResource("messageRowsFound"), controller.Rows.Count, s);
                                }, DispatcherPriority.Background);
                            }
                        }
                    }
                }
                await dispatcher.InvokeAsync(() =>
                {
                    Parameters.Save();
                    controller.UpdateGrid();
                });
            }
            catch(Exception t)
            {
                await dispatcher.InvokeAsync(() =>
                {
                    AddLog(CurrentDataSet.GetExceptionMessage(t), null, LogStatus.Error, true);
                    App.LogException(t);
                });
                return;
            }
            finally
            {
                EndFetching();
            }
        }

        private void UpdateDataGridResult(SQLParts sqls)
        {
            StartFetching();
            Dispatcher dispatcher = Dispatcher;
            Db2SourceContext dataSet = CurrentDataSet;
            DataGridController controller = DataGridControllerResult;
            Task _ = ExecuteSqlPartsAsync(dispatcher, dataSet, controller, sqls);
        }

        private void Parameters_ParameterTextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IEnumerable temp = dataGridParameters.ItemsSource;
            dataGridParameters.ItemsSource = null;
            dataGridParameters.ItemsSource = temp;
            //dataGridParameters.UpdateLayout();
        }

        private void listBoxLogCommandCopy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listBoxLog.SelectedItems.Count == 0)
            {
                return;
            }
            StringBuilder buf = new StringBuilder();
            foreach (ListBoxItem item in listBoxLog.Items)
            {
                if (item.IsSelected)
                {
                    buf.AppendLine(item.ToString());
                }
            }
            Clipboard.SetText(buf.ToString());
            e.Handled = true;
        }
        private void listBoxLogCommandSelAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            listBoxLog.SelectAll();
            e.Handled = true;
        }

        private void textBoxSqlCommandNormalizeSql_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxSql.Text = CurrentDataSet.NormalizeSQL(textBoxSql.Text);
            e.Handled = true;
        }

        private void Fetch()
        {
            Db2SourceContext ctx = CurrentDataSet;
            if (ctx == null)
            {
                AddLog((string)FindResource("messageNotConnected"), null, LogStatus.Error, true);
                return;
            }
            string sql = textBoxSql.Text.TrimEnd();
            if (string.IsNullOrEmpty(sql))
            {
                AddLog((string)FindResource("messageNoSql"), null, LogStatus.Error, true);
                return;
            }
            SQLParts parts = ctx.SplitSQL(sql);
            if (parts.Count == 0)
            {
                AddLog((string)FindResource("messageNoSql"), null, LogStatus.Error, true);
                return;
            }
            listBoxErrors.Items.Clear();
            listBoxErrors.Visibility = Visibility.Collapsed;
            UpdateDataGridResult(parts);
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

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            ParameterStoreCollection.AllParameters.ParameterTextChanged += Parameters_ParameterTextChanged;
            DataGridControllerResult = new DataGridController
            {
                //Context = CurrentDataSet,
                Grid = dataGridResult
            };
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, listBoxLogCommandCopy_Executed));
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, listBoxLogCommandSelAll_Executed));
            textBoxSql.CommandBindings.Add(new CommandBinding(QueryCommands.NormalizeSQL, textBoxSqlCommandNormalizeSql_Executed));
            LoadFromRegistry();
        }

        private void buttonCopyAll_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ContextMenu menu = FindResource("ContextMenuCopyTable") as ContextMenu;
            menu.PlacementTarget = btn;
            menu.IsOpen = true;
            foreach (MenuItem mi in menu.Items)
            {
                mi.CommandTarget = dataGridResult;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ParameterStoreCollection.AllParameters.ParameterTextChanged -= Parameters_ParameterTextChanged;
        }

        private void menuItemClearLog_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
        }

        private void buttonSearchWord_Click(object sender, RoutedEventArgs e)
        {
            ApplicationCommands.Find.Execute(null, dataGridResult);
        }

        private void buttonHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow window = new HistoryWindow
            {
                Owner = Window.GetWindow(this)
            };
            bool? ret = window.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return;
            }
            QueryHistory.Query sel = window.Selected;
            if (sel == null)
            {
                return;
            }
            textBoxSql.Text = sel.SqlText;
            Parameters = ParameterStoreCollection.GetParameterStores(sel, Parameters, out _);
        }

        private void DataGridCellParamValueStyleButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = (ContextMenu)FindResource("ContextMenuParameter");
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = App.FindVisualParent<DataGridCell>(e.Source as DependencyObject);
            menu.IsOpen = true;
        }

        private void MenuItemParameterSetNull(object sender, RoutedEventArgs e)
        {
            (dataGridParameters.CurrentItem as ParameterStore).Value = DBNull.Value;
        }
    }
}
