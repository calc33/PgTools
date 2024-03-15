using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using System.Runtime.Remoting.Channels;

namespace Db2Source
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string TitleBase;
        public static readonly Type[] ConnectionInfoTypes = new Type[] { typeof(NpgsqlConnectionInfo) };
        public static readonly DependencyProperty CurrentDataSetProperty = DependencyProperty.Register("CurrentDataSet", typeof(Db2SourceContext), typeof(MainWindow), new PropertyMetadata(new PropertyChangedCallback(OnCurrentDataSetPropertyChanged)));
        public static readonly DependencyProperty ConnectionStatusProperty = DependencyProperty.Register("ConnectionStatus", typeof(SchemaConnectionStatus), typeof(MainWindow), new PropertyMetadata(new PropertyChangedCallback(OnConnectionStatusPropertyChanged)));
        public static readonly DependencyProperty MultipleSelectionModeProperty = DependencyProperty.Register("MultipleSelectionMode", typeof(bool), typeof(MainWindow), new PropertyMetadata(new PropertyChangedCallback(OnMultipleSelectionModePropertyChanged)));
        public static readonly DependencyProperty IndentProperty = DependencyProperty.Register("Indent", typeof(int), typeof(MainWindow), new PropertyMetadata(2, IndentPropertyChangedCallback));
        public static readonly DependencyProperty IndentCharProperty = DependencyProperty.Register("IndentChar", typeof(string), typeof(MainWindow), new PropertyMetadata(" ", IndentCharPropertyChangedCallback));
        public static readonly DependencyProperty IndentOffsetProperty = DependencyProperty.Register("IndentOffset", typeof(int), typeof(MainWindow), new PropertyMetadata(0, IndentOffsetPropertyChangedCallback));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty, SearchTextPropertyChangedCallback));
        public static readonly DependencyProperty MatchByIgnoreCaseProperty = DependencyProperty.Register("MatchByIgnoreCase", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, MatchByIgnoreCasePropertyChangedCallback));
        public static readonly DependencyProperty MatchByWordwrapProperty = DependencyProperty.Register("MatchByWordwrap", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, MatchByWordwrapPropertyChangedCallback));
        public static readonly DependencyProperty MatchByWholeProperty = DependencyProperty.Register("MatchByWhole", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, MatchByWholePropertyChangedCallback));
        public static readonly DependencyProperty MatchByRegexProperty = DependencyProperty.Register("MatchByRegex", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, MatchByRegexPropertyChangedCallback));
        public static readonly DependencyProperty CommandTimeoutProperty = DependencyProperty.Register("CommandTimeout", typeof(int), typeof(MainWindow), new PropertyMetadata(30, CommandTimeoutPropertyChangedCallback));
        private void UpdateIndentText()
        {
            Db2SourceContext.IndentText = IndentText;

        }

        public event DependencyPropertyChangedEventHandler IndentPropertyChanged;
        protected void OnIndentPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateIndentText();
            IndentPropertyChanged?.Invoke(sender, e);
        }
        private static void IndentPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnIndentPropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler IndentCharPropertyChanged;
        protected void OnIndentCharPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateIndentText();
            IndentCharPropertyChanged?.Invoke(sender, e);
        }
        private static void IndentCharPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnIndentCharPropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler IndentOffsetPropertyChanged;
        protected void OnIndentOffsetPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IndentOffsetPropertyChanged?.Invoke(sender, e);
        }
        private static void IndentOffsetPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnIndentOffsetPropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler SearchTextPropertyChanged;
        protected void OnSearchTextPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SearchTextPropertyChanged?.Invoke(sender, e);
        }
        private static void SearchTextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnSearchTextPropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler MatchByIgnoreCasePropertyChanged;
        protected void OnMatchByIgnoreCasePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MatchByIgnoreCasePropertyChanged?.Invoke(sender, e);
        }
        private static void MatchByIgnoreCasePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnMatchByIgnoreCasePropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler MatchByWordwrapPropertyChanged;
        protected void OnMatchByWordwrapPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MatchByWordwrapPropertyChanged?.Invoke(sender, e);
        }
        private static void MatchByWordwrapPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnMatchByWordwrapPropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler MatchByWholePropertyChanged;
        protected void OnMatchByWholePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MatchByWholePropertyChanged?.Invoke(sender, e);
        }
        private static void MatchByWholePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnMatchByWholePropertyChanged(d, e);
        }

        public event DependencyPropertyChangedEventHandler MatchByRegexPropertyChanged;
        protected void OnMatchByRegexPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MatchByRegexPropertyChanged?.Invoke(sender, e);
        }
        private static void MatchByRegexPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnMatchByRegexPropertyChanged(d, e);
        }
        public event DependencyPropertyChangedEventHandler CommandTimeoutPropertyChanged;
        protected void OnCommandTimeoutPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CommandTimeoutPropertyChanged?.Invoke(sender, e);
        }
        private static void CommandTimeoutPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MainWindow)?.OnCommandTimeoutPropertyChanged(d, e);
        }

        public static MainWindow Current { get; private set; } = null;
        public Db2SourceContext CurrentDataSet
        {
            get { return (Db2SourceContext)GetValue(CurrentDataSetProperty); }
            set { SetValue(CurrentDataSetProperty, value); }
        }

        public SchemaConnectionStatus ConnectionStatus
        {
            get { return (SchemaConnectionStatus)GetValue(ConnectionStatusProperty); }
            set { SetValue(ConnectionStatusProperty, value); }
        }

        public bool MultipleSelectionMode
        {
            get { return (bool)GetValue(MultipleSelectionModeProperty); }
            set { SetValue(MultipleSelectionModeProperty, value); }
        }

        public int Indent
        {
            get { return (int)GetValue(IndentProperty); }
            set { SetValue(IndentProperty, value); }
        }

        public string IndentChar
        {
            get { return (string)GetValue(IndentCharProperty); }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("IndentChar");
                }
                if (value.Length != 1)
                {
                    throw new ArgumentException("IndentChar");
                }
                SetValue(IndentCharProperty, value);
            }
        }

        public int IndentOffset
        {
            get { return (int)GetValue(IndentOffsetProperty); }
            set { SetValue(IndentOffsetProperty, value); }
        }

        public string IndentText
        {
            get
            {
                return string.IsNullOrEmpty(IndentChar) ? string.Empty : new string(IndentChar[0], Indent);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("IndentText");
                }
                if (value.Length == 0)
                {
                    Indent = 0;
                    return;
                }
                char c = value[0];
                foreach (char ch in value)
                {
                    if (ch != c)
                    {
                        throw new ArgumentException((string)Resources["InvalidIndentText"]);
                    }
                }
                Indent = value.Length;
                IndentChar = c.ToString();
            }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public bool MatchByIgnoreCase
        {
            get { return (bool)GetValue(MatchByIgnoreCaseProperty); }
            set { SetValue(MatchByIgnoreCaseProperty, value); }
        }

        public bool MatchByWordwrap
        {
            get { return (bool)GetValue(MatchByWordwrapProperty); }
            set { SetValue(MatchByWordwrapProperty, value); }
        }

        public bool MatchByWhole
        {
            get { return (bool)GetValue(MatchByWholeProperty); }
            set { SetValue(MatchByWholeProperty, value); }
        }

        public bool MatchByRegex
        {
            get { return (bool)GetValue(MatchByRegexProperty); }
            set { SetValue(MatchByRegexProperty, value); }
        }

        public int CommandTimeout
        {
            get { return (int)GetValue(CommandTimeoutProperty); }
            set { SetValue(CommandTimeoutProperty, value); }
        }

        private void SetConnectionStatus(SchemaConnectionStatus value)
        {
            Dispatcher.InvokeAsync(() => { ConnectionStatus = value; });
        }

        private int _queryControlIndex = 1;

        private TreeViewFilterKeyEventController _treeViewFilterController;
        public MainWindow()
        {
            InitializeComponent();
            if (Current == null)
            {
                Current = this;
            }
        }

        private void MenuItemOpenDb_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataSet != null)
            {
                string path = Assembly.GetExecutingAssembly().Location;
                Process.Start(path);
                return;
            }
            ConnectionInfo info = NewConnectionInfoFromRegistry();
            NewConnectionWindow win = new NewConnectionWindow() { Owner = this, Target = info };
            bool? ret = win.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return;
            }
            info = win.Target;
            Connect(info);
        }
        private void UpdateTabControlsTarget()
        {
            for (int i = tabControlMain.Items.Count - 1; 0 <= i; i--)
            {
                TabItem item = tabControlMain.Items[i] as TabItem;
                UIElement c = item.Content as UIElement;
                Type t = c.GetType();
                PropertyInfo p = t.GetProperty("Target");
                if (p == null)
                {
                    continue;
                }
                SchemaObject o = p?.GetValue(c) as SchemaObject;
                if (o != null)
                {
                    Schema sch = CurrentDataSet.Schemas[o.SchemaName];
                    o = sch?.GetCollection(o.GetCollectionIndex())[o.Identifier] as SchemaObject;
                }
                if (o != null)
                {
                    p.SetValue(c, o);
                }
                else
                {
                    tabControlMain.Items.RemoveAt(i);
                }
            }
        }

        private DataGrid FindDataGridRecursive(DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            if (obj is DataGrid)
            {
                DataGrid gr = (DataGrid)obj;
                return gr.IsVisible ? gr : null;
            }
            if (obj is TabControl)
            {
                return FindDataGridRecursive(((TabControl)obj).SelectedItem as TabItem);
            }
            if (obj is ContentControl)
            {
                return FindDataGridRecursive(((ContentControl)obj).Content as DependencyObject);
            }
            if (obj is Panel)
            {
                foreach (object elem in LogicalTreeHelper.GetChildren(obj))
                {
                    if (!(elem is DependencyObject))
                    {
                        return null;
                    }
                    DataGrid ret = FindDataGridRecursive((DependencyObject)elem);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
                return null;
            }
            return null;
        }

        private DataGrid GetActiveDataGrid()
        {
            FrameworkElement elem = tabControlMain.SelectedItem as FrameworkElement;
            if (elem == null)
            {
                return null;
            }
            return FindDataGridRecursive(tabControlMain);
        }

        public static void TabBecomeVisible(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }
            if (element is Window)
            {
                return;
            }
            TabBecomeVisible(element.Parent as FrameworkElement);
            if (!(element is TabItem))
            {
                return;
            }
            TabItem tab = element as TabItem;
            TabControl ctrl = tab.Parent as TabControl;
            if (ctrl != null && ctrl.SelectedItem != tab)
            {
                ctrl.SelectedItem = tab;
            }
        }

        private void UpdateSchema()
        {
            ConnectionStatus = SchemaConnectionStatus.Done;
            if (CurrentDataSet == null)
            {
                Title = TitleBase;
                treeViewItemTop.Header = (string)Resources["TreeViewItemTopHeader"];
                treeViewItemTop.Items.Clear();
                return;
            }
            Title = TitleBase + " - " + CurrentDataSet?.ConnectionInfo?.GetDefaultName();
            UpdateTreeViewDB();
            UpdateTabControlsTarget();
        }

        public static Color ToColor(RGB rgb)
        {
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        private Brush GetBackgroundColor()
        {
            if (CurrentDataSet == null || CurrentDataSet.ConnectionInfo == null)
            {
                return SystemColors.ControlBrush;
            }
            return new SolidColorBrush(ToColor(CurrentDataSet.ConnectionInfo.BackgroundColor));
        }

        private void LoadLocationFromConnectionInfo(ConnectionInfo info)
        {
            if (info == null)
            {
                return;
            }
            if (info.WindowLeft.HasValue)
            {
                Left = info.WindowLeft.Value;
            }
            if (info.WindowTop.HasValue)
            {
                Top = info.WindowTop.Value;
            }
            if (info.WindowWidth.HasValue)
            {
                Width = info.WindowWidth.Value;
            }
            if (info.WindowHeight.HasValue)
            {
                Height = info.WindowHeight.Value;
            }
            if (info.IsWindowMaximized.HasValue)
            {
                WindowState = info.IsWindowMaximized.Value ? WindowState.Maximized : WindowState.Normal;
            }
        }

        private void SaveLocationToConnectionInfo()
        {
            ConnectionInfo info = CurrentDataSet?.ConnectionInfo;
            if (info == null)
            {
                return;
            }
            info.WindowLeft = Left;
            info.WindowTop = Top;
            info.WindowWidth = Width;
            info.WindowHeight = Height;
            info.IsWindowMaximized = (WindowState == WindowState.Maximized);
            App.Connections.Merge(info);
            App.Connections.Save(info, null);
        }


        private Db2SourceContext _dataSetTemp;
        private void SetSchema()
        {
            CurrentDataSet = null;
            CurrentDataSet = _dataSetTemp;
            Resources["WindowBackground"] = GetBackgroundColor();
            RegistryBinding binding = App.NewRegistryBinding(_dataSetTemp.ConnectionInfo);
            binding.Save(App.RegistryFinder);
            //CurrentDataSet.MergeConnectionInfo(App.Connections);
        }

        private void CurrentDataSet_SchemaLoaded(object sender, EventArgs e)
        {
            _dataSetTemp = sender as Db2SourceContext;
            Dispatcher.InvokeAsync(SetSchema);
        }

        //#pragma warning disable 1998
        public async Task LoadSchemaAsync(Db2SourceContext dataSet, IDbConnection connection)
        {
            try
            {
                await dataSet.LoadSchemaAsync(connection);
                GC.Collect();
            }
            catch (Exception t)
            {
                App.HandleThreadException(t);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }
            return;
        }
        public async Task LoadSchemaAsync(Db2SourceContext dataSet)
        {
            try
            {
                await dataSet.LoadSchemaAsync();
                GC.Collect();
            }
            catch (Exception t)
            {
                App.HandleThreadException(t);
            }
            return;
        }

        public void LoadSchema(Db2SourceContext dataSet, IDbConnection connection)
        {
            LoadLocationFromConnectionInfo(dataSet.ConnectionInfo);
            SetConnectionStatus(SchemaConnectionStatus.Loading);
            try
            {
                Task t = LoadSchemaAsync(dataSet, connection);
            }
            finally
            {
                SetConnectionStatus(SchemaConnectionStatus.Done);
            }
        }
        public async Task ConnectAndLoadSchemaAsync(Db2SourceContext dataSet, ConnectionInfo info)
        {
            try
            {
                SetConnectionStatus(SchemaConnectionStatus.Connecting);
                IDbConnection conn;
                try
                {
                    conn = await info.NewConnectionAsync(true);
                }
                catch
                {
                    await Dispatcher.InvokeAsync(() => { SaveConnectionInfo(info, false); }, DispatcherPriority.ApplicationIdle);
                    throw;
                }
                await Dispatcher.InvokeAsync(() => { SaveConnectionInfo(info, true); }, DispatcherPriority.ApplicationIdle);
                SetConnectionStatus(SchemaConnectionStatus.Loading);
                await LoadSchemaAsync(dataSet, conn);
            }
            finally
            {
                SetConnectionStatus(SchemaConnectionStatus.Done);
            }
        }
        public void LoadSchema(Db2SourceContext dataSet)
        {
            SetConnectionStatus(SchemaConnectionStatus.Loading);
            _ = LoadSchemaAsync(dataSet);
        }

        //#pragma warning restore 1998

        public void OpenViewer(SchemaObject target)
        {
            ISchemaObjectWpfControl curCtl = (tabControlMain.SelectedItem as TabItem)?.Content as ISchemaObjectWpfControl;

            TabItem item = MovableTabItem.RequireTabItem(target, FindResource("TabItemStyleClosable") as Style, tabControlMain, CurrentDataSet);
            if (item == null)
            {
                return;
            }
            if (item.Parent == null)
            {
                tabControlMain.Items.Add(item);
            }
            tabControlMain.SelectedItem = item;
            ISchemaObjectWpfControl newCtl = item.Content as ISchemaObjectWpfControl;
            if (newCtl != null && curCtl != null)
            {
                newCtl.SelectedTabKey = curCtl.SelectedTabKey;
            }
        }

        /// <summary>
        /// treeViewDBで選択している項目のビューアを開く
        /// </summary>
        public void OpenViewerFromTreeViewDBSelection()
        {
            TreeViewItem item = treeViewDB.SelectedItem as TreeViewItem;
            if (item == null)
            {
                return;
            }
            TreeNode node = item.Tag as TreeNode;
            if (node == null)
            {
                return;
            }
            SchemaObject obj = node.Target as SchemaObject;
            if (obj == null)
            {
                return;
            }
            OpenViewer(obj);
        }

        protected void OnCurrentDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (CurrentDataSet == null)
            {
                return;
            }
            UpdateSchema();
            CurrentDataSet.SchemaObjectReplaced += CurrentDataSet_SchemaObjectReplaced;
            CurrentDataSet.Log += CurrentDataSet_Log;
            //CurrentDataSet.SchemaLoaded += CurrentDataSet_SchemaLoaded;
        }

        private static void OnCurrentDataSetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as MainWindow)?.OnCurrentDataSetPropertyChanged(e);
        }

        protected void OnConnectionStatusPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            gridLoading.Visibility = (ConnectionStatus != SchemaConnectionStatus.Done) ? Visibility.Visible : Visibility.Collapsed;
            textBlockConnecting.Visibility = (ConnectionStatus == SchemaConnectionStatus.Connecting) ? Visibility.Visible : Visibility.Collapsed;
            textBlockLoading.Visibility = (ConnectionStatus == SchemaConnectionStatus.Loading) ? Visibility.Visible : Visibility.Collapsed;
        }
        private static void OnConnectionStatusPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as MainWindow)?.OnConnectionStatusPropertyChanged(e);
        }

        protected void OnMultipleSelectionModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (menuItemMultiSelMode.IsChecked != MultipleSelectionMode)
            {
                menuItemMultiSelMode.IsChecked = MultipleSelectionMode;
            }
            Dispatcher.InvokeAsync(UpdateTreeViewDB, DispatcherPriority.Background);
        }

        private static void OnMultipleSelectionModePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as MainWindow)?.OnMultipleSelectionModePropertyChanged(e);
        }

        private void CurrentDataSet_Log(object sender, LogEventArgs e)
        {
            LogListBoxItem item = new LogListBoxItem()
            {
                Time = DateTime.Now,
                Status = e.Status,
                Message = e.Text,
                ToolTip = e.Command?.CommandText
            };
            listBoxLog.Items.Add(item);
            listBoxLog.SelectedItem = item;
            listBoxLog.ScrollIntoView(item);
            menuItemLogWindow.IsChecked = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            try
            {
                base.OnPropertyChanged(e);
            }
            catch (Exception t)
            {
                App.Log(string.Format("OnPropertyChanged(Property: \"{0}\", NewValue: {1}, OldValue: {2}): {3}",
                    e.Property.Name, e.NewValue != null ? e.NewValue.ToString() : "null", e.OldValue != null ? e.OldValue.ToString() : "null", t.ToString()));
                throw;
            }
        }

        private void ReplaceSchemaObjectRecursive(TreeViewItem treeViewItem, SchemaObject newObj, SchemaObject oldObj)
        {
            for (int i = treeViewItem.Items.Count - 1; 0 <= i; i--)
            {
                TreeViewItem item = treeViewItem.Items[i] as TreeViewItem;
                ReplaceSchemaObjectRecursive(item, newObj, oldObj);
            }
            TreeNode node = treeViewItem.Tag as TreeNode;
            if (node == null)
            {
                return;
            }
            SchemaObject obj = node.Target as SchemaObject;
            if (obj == null)
            {
                return;
            }
            if (obj == oldObj)
            {
                if (newObj == null)
                {
                    TreeViewItem p = treeViewItem.Parent as TreeViewItem;
                    if (p != null)
                    {
                        p.Items.Remove(treeViewItem);
                    }
                }
                else
                {
                    treeViewItem.Tag = newObj;
                }
            }
        }
        private void CurrentDataSet_SchemaObjectReplaced(object sender, SchemaObjectReplacedEventArgs e)
        {
            foreach (TreeViewItem item in treeViewDB.Items)
            {
                ReplaceSchemaObjectRecursive(item, e.New, e.Old);
            }
            for (int i = tabControlMain.Items.Count - 1; 0 <= i; i--)
            {
                TabItem item = tabControlMain.Items[i] as TabItem;
                if (item == null)
                {
                    continue;
                }
                ISchemaObjectWpfControl obj = (item.Content as ISchemaObjectWpfControl);
                if (obj != null && obj.Target == e.Old)
                {
                    if (e.New == null)
                    {
                        tabControlMain.Items.RemoveAt(i);
                    }
                    else
                    {
                        obj.Target = e.New;
                    }
                }
            }
        }

        private void TabItemCloseButton_Click(object sender, RoutedEventArgs e)
        {
            TabItem item = (sender as Control).TemplatedParent as TabItem;
            if (item == null)
            {
                return;
            }
            TabControl tab = item.Parent as TabControl;
            if (tab == null)
            {
                return;
            }
            tab.Items.Remove(item);
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            //// フィルタ文字列が空文字列になった時はEnterを押さないとツリーが更新されない
            //if (!string.IsNullOrEmpty(_treeViewFilterText) && string.IsNullOrEmpty(textBoxFilter.Text))
            //{
            //    return;
            //}
            DelayedFilterTreeView();
        }

        private void textBoxFilter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TreeViewItem sel = treeViewDB.SelectedItem as TreeViewItem;
            KeyEventArgs e2;
            switch (e.Key)
            {
                case Key.Up:
                    TreeViewUtil.SelectPreviousSiblingTreeViewItem(treeViewDB);
                    e.Handled = true;
                    break;
                case Key.Down:
                    TreeViewUtil.SelectNextSiblingTreeViewItem(treeViewDB);
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (textBoxFilter.SelectionStart != 0 || textBoxFilter.SelectionLength != 0)
                    {
                        break;
                    }
                    if (sel != null && sel.IsExpanded)
                    {
                        sel.IsExpanded = false;
                    }
                    else
                    {
                        TreeViewUtil.SelectParentTreeViewItem(treeViewDB);
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (textBoxFilter.SelectionStart < textBoxFilter.Text.Length)
                    {
                        break;
                    }
                    if (sel != null && sel.HasItems)
                    {
                        sel.IsExpanded = true;
                    }
                    e.Handled = true;
                    break;
                case Key.PageUp:
                case Key.PageDown:
                    treeViewDB.RaiseEvent(e);
                    e2 = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
                    {
                        RoutedEvent = KeyDownEvent
                    };
                    treeViewDB.RaiseEvent(e2);
                    e.Handled = e2.Handled;
                    textBoxFilter.Focus();
                    break;
            }
        }

        private void textBoxFilter_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            TreeViewItem sel = treeViewDB.SelectedItem as TreeViewItem;
            KeyEventArgs e2;
            switch (e.Key)
            {
                case Key.PageUp:
                case Key.PageDown:
                    treeViewDB.RaiseEvent(e);
                    e2 = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
                    {
                        RoutedEvent = KeyUpEvent
                    };
                    treeViewDB.RaiseEvent(e2);
                    e.Handled = e2.Handled;
                    break;
            }
        }

        private void textBoxFilter_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Dispatcher.InvokeAsync(() => { FilterTreeView(false); });
                    OpenViewerFromTreeViewDBSelection();
                    e.Handled = true;
                    break;
            }
        }

        private void textBoxFilter_LostFocus(object sender, RoutedEventArgs e)
        {
            FilterTreeView(false);
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            treeViewItemTop.Items.Clear();

            MovableTabItem.RegisterSchemaObjectControl(typeof(Table), typeof(TableControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(View), typeof(ViewControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(Sequence), typeof(SequenceControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(StoredFunction), typeof(StoredProcedureControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(ComplexType), typeof(ComplexTypeControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(PgsqlBasicType), typeof(PgsqlTypeControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(PgsqlEnumType), typeof(PgsqlTypeControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(PgsqlRangeType), typeof(PgsqlTypeControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(PgsqlDatabase), typeof(DatabaseControl));
            MovableTabItem.RegisterSchemaObjectControl(typeof(SessionList), typeof(PgsqlSessionListControl));
            TitleBase = Title;
            LoadFromRegistry();
            ParameterStoreCollection.AllParameters.Load();
        }

        private void menuItemRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            LoadSchema(CurrentDataSet);
        }

        private void buttonFilterKind_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = buttonFilterKind.ContextMenu;
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = buttonFilterKind;
            menu.IsOpen = true;
            DelayedFilterTreeView();
        }

        private void menuItemAddQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryControl c = new QueryControl();
            Binding b = new Binding("CurrentDataSet") { ElementName = "window" };
            c.SetBinding(QueryControl.CurrentDataSetProperty, b);
            TabItem item = MovableTabItem.NewTabItem(tabControlMain, c.GetTabItemHeader(_queryControlIndex), c, FindResource("TabItemStyleClosable") as Style);
            tabControlMain.SelectedItem = item;
            _queryControlIndex++;
        }

        private void buttonQuit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = MessageBox.Show(this, Properties.Resources.MessageBoxText_Quit, Properties.Resources.MessageBoxCaption_Quit, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            Application.Current.Shutdown();
        }

        private void menuItemExportSchema_Click(object sender, RoutedEventArgs e)
        {
            ExportSchema win = new ExportSchema() { Owner = this, DataSet = CurrentDataSet };
            win.ShowDialog();
        }

        private RegistryBinding _registryBinding;
        private void RequireRegistryBindings()
        {
            if (_registryBinding != null)
            {
                return;
            }
            _registryBinding = new RegistryBinding();
            _registryBinding.Register(this);
            _registryBinding.Register(this, gridBase);
            _registryBinding.Register(string.Empty, "Indent", this, "Indent", new Int32Operator());
            _registryBinding.Register(string.Empty, "IndentChar", this, "IndentChar", new StringOperator());
            _registryBinding.Register(string.Empty, "IndentOffset", this, "IndentOffset", new Int32Operator());
            _registryBinding.Register(string.Empty, "Maximized", this, "WindowState", new WindowStateOperator());
            _registryBinding.Register(string.Empty, "CommandTimeout", this, "CommandTimeout", new Int32Operator());
        }

        public RegistryBinding RegistryBinding
        {
            get
            {
                RequireRegistryBindings();
                return _registryBinding;
            }
        }

        private void LoadFromRegistry()
        {
            RegistryBinding.Load(App.RegistryFinder);
            foreach (MovableTabItem tabItem in tabControlMain.Items)
            {
                IRegistryStore store = tabItem.Content as IRegistryStore;
                store?.LoadFromRegistry();
            }
        }
        private void SaveToRegistry()
        {
            foreach (MovableTabItem tabItem in tabControlMain.Items)
            {
                IRegistryStore store = tabItem.Content as IRegistryStore;
                store?.SaveToRegistry();
            }
            RegistryBinding.Save(App.RegistryFinder);
        }

        private ConnectionInfo NewConnectionInfoFromRegistry()
        {
            NpgsqlConnectionInfo info = new NpgsqlConnectionInfo()
            {
                ServerName = App.RegistryFinder.GetString("Connection", "ServerName", App.Hostname),
                ServerPort = App.RegistryFinder.GetInt32("Connection", "ServerPort", App.Port),
                DatabaseName = App.RegistryFinder.GetString("Connection", "DatabaseName", App.Database),
                UserName = App.RegistryFinder.GetString("Connection", "UserName", App.Username),
                SearchPath = App.RegistryFinder.GetString("Connection", "SearchPath", App.SearchPath),
            };
            info.FillStoredPassword(false);
            info = App.Connections.Find(info) as NpgsqlConnectionInfo;
            return info;
        }

        private void SaveConnectionInfo(ConnectionInfo info, bool connected)
        {
            info = App.Connections.Merge(info);
            App.Connections.Save(info, connected);
            info.UpdateLastConnected(App.Connections.RequireDatabase());
        }

        private void Connect(ConnectionInfo info)
        {
            info = App.Connections.Merge(info);
            Db2SourceContext ds = info.NewDataSet();
            ds.SchemaLoaded += CurrentDataSet_SchemaLoaded;
            ConnectionStatus = SchemaConnectionStatus.Connecting;
            try
            {
                Task t = ConnectAndLoadSchemaAsync(ds, info);
            }
            catch
            {
                ConnectionStatus = SchemaConnectionStatus.Done;
                throw;
            }
        }

        /// <summary>
        /// MenuItemのTagに格納されているファイル名をフルパスに変換する
        /// </summary>
        /// <param name="menuItem"></param>
        private void InitExecutableMenuItem(MenuItem menuItem, RoutedEventHandler clickEvent)
        {
            string exe = menuItem.Tag.ToString();
            string path = App.GetExecutableFromPath(exe);
            List<PgsqlInstallation> l = new List<PgsqlInstallation>();
            foreach (PgsqlInstallation ins in PgsqlInstallation.Installations)
            {
                string s = System.IO.Path.Combine(ins.BinDirectory, exe);
                if (System.IO.File.Exists(s))
                {
                    l.Add(ins);
                }
            }
            switch (l.Count)
            {
                case 0:
                    menuItem.Tag = path;
                    menuItem.IsEnabled = !string.IsNullOrEmpty(path);
                    break;
                case 1:
                    menuItem.Tag = System.IO.Path.Combine(l[0].BinDirectory, exe);
                    menuItem.IsEnabled = true;
                    break;
                default:
                    foreach (PgsqlInstallation ins in l)
                    {
                        string s = System.IO.Path.Combine(ins.BinDirectory, exe);
                        MenuItem mi = new MenuItem() { Header = ins.Name, Tag = s, ToolTip = ins.BinDirectory };
                        mi.Click += clickEvent;
                        if (string.Compare(s, path, true) == 0)
                        {
                            mi.FontWeight = FontWeights.Bold;
                            menuItem.Items.Insert(0, mi);
                        }
                        else
                        {
                            menuItem.Items.Add(mi);
                        }
                    }
                    menuItem.IsEnabled = true;
                    menuItem.Click -= clickEvent;
                    break;
            }
        }

        private void InitMenu()
        {
            InitExecutableMenuItem(menuItemPsql, menuItemPsql_Click);
            //InitExecutableMenuItem(menuItemPgdump, menuItemPgdump_Click);
        }
        private void InitCommandBindings()
        {
            CommandBinding cb;
            cb = new CommandBinding(DataGridCommands.CopyTable, CopyTableCommand_Executed, CopyTableCommand_CanExecute);
            CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableContent, CopyTableContentCommand_Executed, CopyTableCommand_CanExecute);
            CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableAsInsert, CopyTableAsInsertCommand_Executed, CopyTableCommand_CanExecute);
            CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableAsUpdate, CopyTableAsUpdateCommand_Executed, CopyTableCommand_CanExecute);
            CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableAsCopy, CopyTableAsCopyCommand_Executed, CopyTableCommand_CanExecute);
            CommandBindings.Add(cb);
        }

        private ConnectionInfo _startupConnection;
        public ConnectionInfo StartupConnection
        {
            get
            {
                return _startupConnection;
            }
            set
            {
                _startupConnection = value;
                if (_startupConnection != null)
                {
                    LoadLocationFromConnectionInfo(_startupConnection);
                }
            }
        }

        private void StartConnection()
        {
            ConnectionStatus = SchemaConnectionStatus.Done;
            if (StartupConnection != null)
            {
                Connect(StartupConnection);
            }
        }
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            _treeViewFilterController = new TreeViewFilterKeyEventController(treeViewDB, textBoxFilter);
            InitMenu();
            InitCommandBindings();
            UpdateTextBoxFilter();
            Dispatcher.InvokeAsync(StartConnection, DispatcherPriority.ApplicationIdle);
            WindowLocator.AdjustMaxSizeToScreen(this);
        }

        private void window_LocationChanged(object sender, EventArgs e)
        {
            WindowLocator.AdjustMaxSizeToScreen(this);
        }

        private void CopyTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            e.CanExecute = (gr != null) && DataGridCommands.CopyTable.CanExecute(null, gr);
            e.Handled = true;
        }
        private void CopyTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTable.Execute(null, gr);
            e.Handled = true;
        }


        private void CopyTableContentCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableContent.Execute(null, gr);
            e.Handled = true;
        }

        private void CopyTableAsInsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableAsInsert.Execute(null, gr);
            e.Handled = true;
        }

        private void CopyTableAsUpdateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableAsUpdate.Execute(null, gr);
            e.Handled = true;
        }

        private void CopyTableAsCopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableAsCopy.Execute(null, gr);
            e.Handled = true;
        }

        private void menuItemPsql_Click(object sender, RoutedEventArgs e)
        {
            string exe = (sender as MenuItem).Tag.ToString();
            if (string.IsNullOrEmpty(exe))
            {
                return;
            }
            NpgsqlConnectionInfo info = CurrentDataSet.ConnectionInfo as NpgsqlConnectionInfo;
            string arg = string.Format("/K \"{0}\" -h {1} -p {2} -d {3} -U {4}", exe, info.ServerName, info.ServerPort, info.DatabaseName, info.UserName);
            Process.Start("cmd.exe", arg);
        }

        private void menuItemPgdump_Click(object sender, RoutedEventArgs e)
        {
            PgDumpOptionWindow win = new PgDumpOptionWindow() { Owner = this, DataSet = CurrentDataSet };
            win.Show();
        }

        private void buttonAddTab_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ContextMenu menu = FindResource("contextMenuTab") as ContextMenu;
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = !menu.IsOpen;
        }

        private void menuItemAddDatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenViewer(CurrentDataSet.Database);
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            foreach (TabItem c in tabControlMain.Items)
            {
                ISchemaObjectWpfControl obj = c.Content as ISchemaObjectWpfControl;
                if (obj == null)
                {
                    continue;
                }
                bool f = e.Cancel;
                obj.OnTabClosing(sender, ref f);
                e.Cancel = f;
                if (e.Cancel)
                {
                    return;
                }
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            SaveToRegistry();
            SaveLocationToConnectionInfo();
        }

        private void menuItemOption_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow() { Owner = this };
            window.Show();
        }

        private void menuItemClearLog_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
            menuItemLogWindow.IsChecked = false;
        }

        private void menuItemEditConnections_Click(object sender, RoutedEventArgs e)
        {
            EditConnectionListWindow win = new EditConnectionListWindow() { Owner = this };
            win.ShowDialog();
        }

        private struct TabIndexRange
        {
            public ScrollViewer Viewer;
            public TabPanel Panel;
            /// <summary>
            /// 一部だけでも入っているTabIndexの最小値
            /// </summary>
            public int PartialMin;
            /// <summary>
            /// 完全に入っているTabIndexの最小値
            /// </summary>
            public int FullMin;
            /// <summary>
            /// 完全に入っているTabIndexの最大値
            /// </summary>
            public int FullMax;
            /// <summary>
            /// 一部だけでも入っているTabIndexの最大値
            /// </summary>
            public int PartialMax;
            public TabIndexRange(TabControl tabControl)
            {
                ControlTemplate tmpl = tabControl.Template as ControlTemplate;
                Viewer = tmpl.FindName("tabPanelScrollViewer", tabControl) as ScrollViewer;
                Panel = tmpl.FindName("headerPanel", tabControl) as TabPanel;
                PartialMin = int.MaxValue;
                FullMin = int.MaxValue;
                FullMax = -1;
                PartialMax = -1;
                int n = tabControl.Items.Count;
                for (int i = 0; i < n; i++)
                {
                    TabItem item = tabControl.Items[i] as TabItem;
                    Point[] p = new Point[] { new Point(0, 0), new Point(item.ActualWidth + 1, item.ActualHeight + 1) };
                    p[0] = item.TranslatePoint(p[0], Viewer);
                    p[1] = item.TranslatePoint(p[1], Viewer);
                    if (Viewer.ActualWidth <= p[1].X)
                    {
                        Normalize();
                        return;
                    }
                    if (p[0].X < Viewer.ActualWidth && 0 < p[1].X)
                    {
                        PartialMin = Math.Min(PartialMin, i);
                        PartialMax = i;
                    }
                    if (0 <= p[0].X && p[1].X < Viewer.ActualWidth)
                    {
                        FullMin = Math.Min(FullMin, i);
                        FullMax = i;
                    }
                }
                Normalize();
            }
            public void Normalize()
            {
                PartialMin = Math.Min(PartialMin, PartialMax);
                FullMin = Math.Min(FullMin, FullMax);
            }
        }

        private static void ScrollTabItemAsLeft(TabControl tabControl, ScrollViewer viewer, TabPanel panel, int index)
        {
            if (index < 0 || tabControl.Items.Count <= index)
            {
                return;
            }
            TabItem item = tabControl.Items[index] as TabItem;
            Point p = item.TranslatePoint(new Point(), panel);
            viewer.ScrollToHorizontalOffset(p.X);
        }
        private static void ScrollTabItemAsRight(TabControl tabControl, ScrollViewer viewer, TabPanel panel, int index)
        {
            if (index < 0 || tabControl.Items.Count <= index)
            {
                return;
            }
            TabItem item = tabControl.Items[index] as TabItem;
            Point p1 = item.TranslatePoint(new Point(item.ActualWidth + 1, item.ActualHeight + 1), panel);
            double x0 = p1.X - viewer.ActualWidth;
            Point p0 = item.TranslatePoint(new Point(), panel);
            for (int i = index - 1; 0 <= i; i--)
            {
                item = tabControl.Items[i] as TabItem;
                Point p = item.TranslatePoint(new Point(), panel);
                if (p.X < x0)
                {
                    break;
                }
                p0 = p;
                if (p.X == x0)
                {
                    break;
                }
            }
            viewer.ScrollToHorizontalOffset(p0.X);
        }
        private static void ScrollTabItemToVisible(TabControl tabControl)
        {
            TabIndexRange range = new TabIndexRange(tabControl);
            if (range.PartialMin == -1 || range.PartialMax == -1)
            {
                return;
            }
            int index = tabControl.SelectedIndex;
            if (range.FullMin <= index && index <= range.FullMax)
            {
                return;
            }
            if (index <= range.PartialMin)
            {
                ScrollTabItemAsLeft(tabControl, range.Viewer, range.Panel, index);
                return;
            }
            if (range.PartialMax <= index)
            {
                ScrollTabItemAsRight(tabControl, range.Viewer, range.Panel, index);
            }
            return;
        }
        private void ScrollTabItemToVisible()
        {
            ScrollTabItemToVisible(tabControlMain);
        }

        private void buttonScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            TabControl tab = btn.TemplatedParent as TabControl;
            TabIndexRange range = new TabIndexRange(tab);
            ScrollTabItemAsRight(tab, range.Viewer, range.Panel, range.FullMin);
            TabIndexRange range2 = new TabIndexRange(tab);
            if (0 < range.FullMin && range.FullMin == range2.FullMin)
            {
                ScrollTabItemAsRight(tab, range.Viewer, range.Panel, range.FullMin - 1);
            }
        }

        private void buttonScrollRight_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            TabControl tab = btn.TemplatedParent as TabControl;
            TabIndexRange range = new TabIndexRange(tab);
            ScrollTabItemAsLeft(tab, range.Viewer, range.Panel, range.FullMax);
            TabIndexRange range2 = new TabIndexRange(tab);
            if (range.FullMax < tab.Items.Count - 1 && range.FullMax == range2.FullMax)
            {
                ScrollTabItemAsLeft(tab, range.Viewer, range.Panel, range.FullMax + 1);
            }
        }

        private bool _isTabControlMainSelectionChanged = false;
        private void InvalidateTabControlMainSelection()
        {
            _isTabControlMainSelectionChanged = true;
        }
        private void UpdateTabControlMainSelection()
        {
            if (!_isTabControlMainSelectionChanged)
            {
                return;
            }
            _isTabControlMainSelectionChanged = false;
            ScrollTabItemToVisible();
        }

        private void tabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvalidateTabControlMainSelection();
        }

        private void tabControlMain_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateTabControlMainSelection();
        }

        private void buttonScrollSearchTabItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SelectTabItemWindow win = new SelectTabItemWindow() { Owner = this, TabControl = tabControlMain };
            win.Closed += SelectTabItemWindow_Closed;
            WindowLocator.LocateNearby(btn, win, NearbyLocation.DownRight);
            win.Show();
        }

        private void SelectTabItemWindow_Closed(object sender, EventArgs e)
        {
            SelectTabItemWindow win = sender as SelectTabItemWindow;
            TabItem sel = win.SelectedItem;
            if (sel == null)
            {
                return;
            }
            tabControlMain.SelectedItem = sel;
        }

        #region タブのドラッグ&ドロップによる移動
        private MovableTabItem _movingTabItem = null;
        internal MovableTabItem MovingTabItem
        {
            get { return _movingTabItem; }
            set
            {
                if (_movingTabItem == value)
                {
                    return;
                }
                if (_movingTabItem != null)
                {
                    _movingTabItem.IsMoving = false;
                }
                _movingTabItem = value;
                if (_movingTabItem != null)
                {
                    _movingTabItem.IsMoving = true;
                }
            }
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (MovingTabItem != null)
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Released)
                {
                    MovingTabItem = null;
                }
                return;
            }
            MovableTabItem item = e.Source as MovableTabItem;
            if (item == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                ScrollViewer sv = App.FindVisualParent<ScrollViewer>(item);
                if (sv != null && !sv.IsMouseCaptured)
                {
                    Mouse.Capture(sv);
                }
                MovingTabItem = item;
            }
        }

        private void MoveTabItem(TabItem goal)
        {
            if (MovingTabItem == null)
            {
                return;
            }
            if (MovingTabItem == goal)
            {
                return;
            }
            TabControl tab = MovingTabItem.Parent as TabControl;
            int goalPos = tab.Items.IndexOf(goal);
            int i = tab.Items.IndexOf(MovingTabItem);
            bool sel = MovingTabItem.IsSelected;
            tab.Items.RemoveAt(i);
            tab.Items.Insert(goalPos, MovingTabItem);
            if (sel)
            {
                tab.SelectedItem = MovingTabItem;
            }
        }
        private void tabPanelScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (MovingTabItem == null)
            {
                return;
            }

            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed && !viewer.IsMouseCaptured)
            {
                viewer.CaptureMouse();
            }
            FrameworkElement parent = VisualTreeHelper.GetParent(viewer) as FrameworkElement;
            Point p = e.MouseDevice.GetPosition(parent);
            double dx = 0;
            if (p.X < 0)
            {
                dx = p.X;
            }
            else if (viewer.ActualWidth < p.X)
            {
                dx = p.X - viewer.ActualWidth;
            }
            TabControl tab = MovingTabItem.Parent as TabControl;
            MovableTabItem goal;
            if (dx == 0)
            {
                HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
                if (ret == null)
                {
                    return;
                }
                goal = App.FindVisualParent<MovableTabItem>(ret.VisualHit);
                double x = e.MouseDevice.GetPosition(goal).X;
                if (x < 0 || MovingTabItem.ActualWidth < x)
                {
                    // 移動した結果マウスの位置が選択しているタブの範囲外になって振動を起こさないように
                    goal = null;
                }
            }
            else if (dx < 0)
            {
                TabIndexRange range = new TabIndexRange(tab);
                goal = tab.Items[Math.Max(0, range.PartialMin - 1)] as MovableTabItem;
            }
            else
            {
                TabIndexRange range = new TabIndexRange(tab);
                goal = tab.Items[Math.Min(range.PartialMax + 1, tab.Items.Count - 1)] as MovableTabItem;
            }
            if (goal == null || goal.Parent != MovingTabItem.Parent)
            {
                return;
            }
            MoveTabItem(goal);
        }

        private void tabPanelScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer != null && viewer.IsMouseCaptured)
            {
                viewer.ReleaseMouseCapture();
            }
            MovingTabItem = null;
        }

        private void tabPanelScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MovingTabItem = null;
            ScrollViewer viewer = sender as ScrollViewer;
            HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
            if (ret == null)
            {
                return;
            }
            Control c = App.FindVisualParent<Control>(ret.VisualHit);
            MovingTabItem = c as MovableTabItem;
        }
        #endregion

        private void menuItemSessionList_Click(object sender, RoutedEventArgs e)
        {
            OpenViewer(CurrentDataSet.SessionList);
        }

        private void menuItemNewDatabase_Click(object sender, RoutedEventArgs e)
        {
            NewPgsqlDatabaseWindow win = new NewPgsqlDatabaseWindow() { DataSet = CurrentDataSet as NpgsqlDataSet, Owner = this };
            bool? ret = win.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                LoadSchema(CurrentDataSet);
            }
        }

        private void menuItemDatabaseInfo_Click(object sender, RoutedEventArgs e)
        {
            PgsqlDatabase db = (CurrentDataSet as NpgsqlDataSet).Database;
            if (db == null)
            {
                return;
            }
            OpenViewer(db);
        }

        private WeakReference<TabItem> _recordCountTabItem;

        private TabItem RequireRecordCountTabItem(bool becomeSelected)
        {
            TabItem item;
            if (_recordCountTabItem != null && _recordCountTabItem.TryGetTarget(out item))
            {
                if (item.Parent == null)
                {
                    tabControlMain.Items.Add(item);
                }
                if (becomeSelected)
                {
                    tabControlMain.SelectedItem = item;
                }
                return item;
            }

            RecordCountControl c = new RecordCountControl();
            Binding b = new Binding("CurrentDataSet") { Source = this };
            c.SetBinding(RecordCountControl.DataSetProperty, b);
            item = MovableTabItem.NewTabItem(tabControlMain, c.GetTabItemHeader(), c, FindResource("TabItemStyleClosable") as Style);
            _recordCountTabItem = new WeakReference<TabItem>(item);
            if (becomeSelected)
            {
                tabControlMain.SelectedItem = item;
            }
            return item;
        }
        private void menuItemCount_Click(object sender, RoutedEventArgs e)
        {
            RequireRecordCountTabItem(true);
        }

        private static readonly bool[,] TextBoxFilterEnabledMap = new bool[,]
        {
            { false, true },
            { true, true },
        };
        private static readonly string[,] TextBoxFilterTooltipResourceKeyMap = new string[,]
        {
            { "TextBoxFilterTooltipNone", "TextBoxFilterTooltipByColumn" },
            { "TextBoxFilterTooltipByObject", "TextBoxFilterTooltipBoth" },
        };
        private void UpdateTextBoxFilter()
        {
            if (menuItemFilterByObjectName== null || menuItemFilterByColumnName == null)
            {
                return;
            }
            int idxByObj = menuItemFilterByObjectName.IsChecked ? 1 : 0;
            int idxByCol = menuItemFilterByColumnName.IsChecked ? 1 : 0;
            string resName = TextBoxFilterTooltipResourceKeyMap[idxByObj, idxByCol];
            textBoxFilter.ToolTip = (string)Resources[resName];
            textBoxFilter.IsEnabled = TextBoxFilterEnabledMap[idxByObj, idxByCol];
            DelayedFilterTreeView();
        }

        private void GetCheckedIn<T>(List<T> list, TreeViewItem target) where T: SchemaObject
        {
            foreach (TreeViewItem item in target.Items)
            {
                bool? chk = GetIsCheckable(item) ? GetIsChecked(item) : false;
                if (chk.HasValue && !chk.Value)
                {
                    continue;
                }
                TreeNode node = item.Tag as TreeNode;
                NamedObject obj = node?.Target;
                if (chk.HasValue && chk.Value && (obj is T))
                {
                    list.Add((T)obj);
                }
                GetCheckedIn(list, item);
            }
        }

        private T[] GetCheckedOnTreeViewDb<T>() where T: SchemaObject
        {
            List<T> l = new List<T>();
            GetCheckedIn(l, treeViewItemTop);
            return l.ToArray();
        }

        private void menuItemFilterByObjectName_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxFilter();
        }

        private void menuItemFilterByObjectName_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxFilter();
        }

        private void menuItemFilterByColumnName_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxFilter();
        }

        private void menuItemFilterByColumnName_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxFilter();
        }

        private void TreeViewDbContextMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = treeViewDB.ContextMenu;
            menu.PlacementTarget = sender as Button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void menuItemMultiSelMode_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                bool b = (sender as MenuItem).IsChecked;
                if (MultipleSelectionMode == b)
                {
                    return;
                }
                MultipleSelectionMode = b;
            }, DispatcherPriority.Background);
        }

        private void menuItemRecordCount_Click(object sender, RoutedEventArgs e)
        {
            TabItem item = RequireRecordCountTabItem(true);
            if (item == null)
            {
                return;
            }
            RecordCountControl ctrl = item.Content as RecordCountControl;
            if (ctrl == null)
            {
                return;
            }
            ctrl.AddRange(GetCheckedOnTreeViewDb<Table>());
        }
    }
}
