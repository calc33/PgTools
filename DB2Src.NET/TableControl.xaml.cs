using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// TableControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TableControl: UserControl, ISchemaObjectControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Table), typeof(TableControl));
        public static readonly DependencyProperty JoinTablesProperty = DependencyProperty.Register("JoinTables", typeof(JoinTableCollection), typeof(TableControl));
        public static readonly DependencyProperty IsTargetModifiedProperty = DependencyProperty.Register("IsTargetModified", typeof(bool), typeof(TableControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(TableControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(TableControl));
        public static readonly DependencyProperty VisibleLevelProperty = DependencyProperty.Register("VisibleLevel", typeof(HiddenLevel), typeof(TableControl));
        public static readonly DependencyProperty SelectedTriggerProperty = DependencyProperty.Register("SelectedTrigger", typeof(Trigger), typeof(TableControl));

        public Table Target
        {
            get
            {
                return (Table)GetValue(TargetProperty);
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

        public JoinTableCollection JoinTables
        {
            get
            {
                return (JoinTableCollection)GetValue(JoinTablesProperty);
            }
            set
            {
                SetValue(JoinTablesProperty, value);
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

        public bool IsTargetModified
        {
            get
            {
                return (bool)GetValue(IsTargetModifiedProperty);
            }
            set
            {
                SetValue(IsTargetModifiedProperty, value);
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

        public HiddenLevel VisibleLevel
        {
            get
            {
                return (HiddenLevel)GetValue(VisibleLevelProperty);
            }
            set
            {
                SetValue(VisibleLevelProperty, value);
            }
        }

        public Trigger SelectedTrigger
        {
            get
            {
                return (Trigger)GetValue(SelectedTriggerProperty);
            }
            set
            {
                SetValue(SelectedTriggerProperty, value);
            }
        }

        public TableControl()
        {
            InitializeComponent();
            JoinTables = new JoinTableCollection();
        }

        private void UpdateIsTargetModified()
        {
            IsTargetModified = Target.IsModified();
        }
        private void UpdateDataGridColumns()
        {
            bool? flg = checkBoxShowHidden.IsChecked;
            if (flg.HasValue && flg.Value)
            {
                dataGridColumns.ItemsSource = Target.Columns.AllColumns;
            }
            else
            {
                dataGridColumns.ItemsSource = Target.Columns;
            }
        }
        
        private void TargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            JoinTables.Clear();
            JoinTable jt = new JoinTable() { Alias = "a", Table = Target, Kind = JoinKind.Root };
            jt.PropertyChanged += JoinTable_PropertyChanged;
            JoinTables.Add(jt);
            //dataGridColumns.ItemsSource = Target.Columns;
            Target.PropertyChanged += Target_PropertyChanged;
            Target.ColumnPropertyChanged += Target_ColumnPropertyChanged;
            Target.CommentChanged += Target_CommentChanged;
            //dataGridColumns.ItemsSource = new List<Column>(Target.Columns);
            UpdateDataGridColumns();
            DataGridControllerResult.Table = Target;
            sortFields.Target = Target;
            //dataGridReferTo.ItemsSource = Target.ReferTo;
            //dataGridReferedBy.ItemsSource = Target.ReferFrom;
            UpdateTextBoxSource();
            UpdateIsTargetModified();
            UpdateTextBoxTemplateSql();
            Dispatcher.Invoke(Fetch, DispatcherPriority.ApplicationIdle);
        }

        private void JoinTable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Alias")
            {
                UpdateTextBoxSelectSql();
            }
        }

        private void JoinTablesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            JoinTableCollection l = (e.OldValue as JoinTableCollection);
            if (l != null)
            {
                l.CollectionChanged += JoinTables_CollectionChanged;

            }
            l = (e.NewValue as JoinTableCollection);
            if (l != null)
            {
                l.CollectionChanged += JoinTables_CollectionChanged;
            }
        }

        private void SelectedTriggerPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Trigger t = SelectedTrigger;
            checkBoxInsertTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Insert) != 0);
            checkBoxDeleteTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Delete) != 0);
            checkBoxTruncateTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Truncate) != 0);
            checkBoxUpdateTrigger.IsChecked = (t != null) && ((t.Event & TriggerEvent.Update) != 0);
            if (t != null && t.Procedure != null)
            {
                textBoxTriggerBodySQL.Text = t.Context.GetSQL(t.Procedure, string.Empty, string.Empty, 0, false);
            }
            else
            {
                textBoxTriggerBodySQL.Text = string.Empty;
            }
        }

        private void JoinTables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListBoxJoinTable();
            UpdateTextBoxSelectSql();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
            }
            if (e.Property == JoinTablesProperty)
            {
                JoinTablesPropertyChanged(e);
            }
            if (e.Property == SelectedTriggerProperty)
            {
                SelectedTriggerPropertyChanged(e);
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
                IDataReader reader = null;
                try
                {
                    Style style = FindResource("DataGridCellRefColumnStyle") as Style;
                    reader = command.ExecuteReader();
                    DataGridControllerResult.Load(reader, Target);
                    foreach (DataGridColumn c in dataGridResult.Columns)
                    {
                        ColumnInfo info = c.Header as ColumnInfo;
                        if (info == null)
                        {
                            continue;
                        }
                        if (info.ForeignKeys == null || info.ForeignKeys.Length == 0)
                        {
                            continue;
                        }
                        c.CellStyle = style;
                    }
                }
                catch (Exception t)
                {
                    Db2SourceContext ctx = Target.Context;
                    ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, command.CommandText);
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
                if (IsChecked(checkBoxSourceDropReferredCons))
                {
                    foreach (ForeignKeyConstraint f in Target.ReferFrom)
                    {
                        buf.Append(ctx.GetDropSQL(f, string.Empty, ";", 0, false, true));
                    }
                    if (0 < buf.Length)
                    {
                        buf.AppendLine();
                    }
                }
                if (IsChecked(checkBoxSourceMain))
                {
                    buf.AppendLine(ctx.GetSQL(Target, string.Empty, ";", 0, true, IsChecked(checkBoxSourceKeyCons)));
                }
                List<Constraint> list = new List<Constraint>(Target.Constraints);
                list.Sort();
                int lastLength = buf.Length;
                foreach (Constraint c in list)
                {
                    switch (c.ConstraintType)
                    {
                        case ConstraintType.Primary:
                            if (!IsChecked(checkBoxSourceMain) && IsChecked(checkBoxSourceKeyCons))
                            {
                                // 本体ソース内で出力しているので本体を出力しない場合のみ
                                buf.Append(ctx.GetSQL(c, string.Empty, ";", 0, true, true));
                            }
                            break;
                        case ConstraintType.Unique:
                            if (IsChecked(checkBoxSourceKeyCons))
                            {
                                buf.Append(ctx.GetSQL(c, string.Empty, ";", 0, true, true));
                            }
                            break;
                        case ConstraintType.ForeignKey:
                            if (IsChecked(checkBoxSourceRefCons))
                            {
                                buf.Append(ctx.GetSQL(c, string.Empty, ";", 0, true, true));
                            }
                            break;
                        case ConstraintType.Check:
                            if (IsChecked(checkBoxSourceCons))
                            {
                                buf.Append(ctx.GetSQL(c, string.Empty, ";", 0, true, true));
                            }
                            break;
                    }
                }
                if (lastLength < buf.Length)
                {
                    buf.AppendLine();
                }
                if (IsChecked(checkBoxSourceComment))
                {
                    lastLength = buf.Length;
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
                    if (lastLength < buf.Length)
                    {
                        buf.AppendLine();
                    }
                }
                if (IsChecked(checkBoxSourceTrigger))
                {
                    lastLength = buf.Length;
                    foreach (Trigger t in Target.Triggers)
                    {
                        buf.Append(ctx.GetSQL(t, string.Empty, ";", 0, true));
                        buf.AppendLine();
                    }
                    if (lastLength < buf.Length)
                    {
                        buf.AppendLine();
                    }
                }
                if (IsChecked(checkBoxSourceIndex))
                {
                    lastLength = buf.Length;
                    foreach (Index i in Target.Indexes)
                    {
                        buf.Append(ctx.GetSQL(i, string.Empty, ";", 0, true));
                    }
                    if (lastLength < buf.Length)
                    {
                        buf.AppendLine();
                    }
                }
                if (IsChecked(checkBoxSourceReferredCons))
                {
                    lastLength = buf.Length;
                    foreach (ForeignKeyConstraint f in Target.ReferFrom)
                    {
                        buf.Append(ctx.GetSQL(f, string.Empty, ";", 0, true, true));
                    }
                    if (lastLength < buf.Length)
                    {
                        buf.AppendLine();
                    }
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
            if (JoinTables.Count == 0)
            {
                textBoxSelectSql.Text = string.Empty;
                return;
            }
            string alias = JoinTables[0].Alias;
            string where = Target.GetKeyConditionSQL(alias, string.Empty, 0);
            textBoxSelectSql.Text = JoinTables.GetSelectSQL(where, string.Empty, null, VisibleLevel);
        }

        private void UpdateListBoxJoinTable()
        {
            foreach (JoinTable j in ListBoxJoinTables.Items)
            {
                j.UpdateSelectableForeignKeys(JoinTables);
            }
        }

        private void UpdateTextBoxInsertSql()
        {
            if (textBoxInsertSql == null)
            {
                return;
            }
            textBoxInsertSql.Text = Target.GetInsertSql(0, 80, string.Empty);
        }
        private void UpdateTextBoxUpdateSql()
        {
            if (textBoxUpdateSql == null)
            {
                return;
            }
            textBoxUpdateSql.Text = Target.GetUpdateSql(Target.GetKeyConditionSQL(string.Empty, "where ", 0), 0, 80, string.Empty);
        }

        private void UpdateTextBoxDeleteSql()
        {
            if (textBoxDeleteSql == null)
            {
                return;
            }
            textBoxDeleteSql.Text = Target.GetDeleteSql(Target.GetKeyConditionSQL(string.Empty, "where ", 0), 0, 80, string.Empty);
        }

        private void UpdateTextBoxTemplateSql()
        {
            UpdateTextBoxSelectSql();
            UpdateTextBoxInsertSql();
            UpdateTextBoxUpdateSql();
            UpdateTextBoxDeleteSql();
        }

        public void Fetch(string condition)
        {
            textBoxCondition.Text = condition;
            Fetch();
        }
        public void Fetch()
        {
            if (DataGridControllerResult.IsModified)
            {
                MessageBoxResult ret = MessageBox.Show("変更が保存されていません。保存しますか?", "確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
                switch (ret)
                {
                    case MessageBoxResult.Yes:
                        DataGridControllerResult.Save();
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
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
            string orderby = sortFields.GetOrderBySql(string.Empty);
            int offset;
            string sql = Target.GetSelectSQL(null, textBoxCondition.Text, orderby, limit, VisibleLevel, out offset);
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

        private void checkBoxSource_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateIsTargetModified();
        }
        private void Target_ColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateIsTargetModified();
        }
        private void Target_CommentChanged(object sender, CommentChangedEventArgs e)
        {
            UpdateIsTargetModified();
        }

        private static readonly Regex NumericStrRegex = new Regex("[0-9]+");
        private void textBoxLimitRow_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = NumericStrRegex.IsMatch(e.Text);
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
                if (newC.IsModified())
                {
                    Column oldC = Target.Columns[i, true];
                    sqls.AddRange(ctx.GetAlterColumnSQL(newC, oldC));
                }
                if ((newC.Comment != null) && newC.Comment.IsModified())
                {
                    sqls.Add(ctx.GetSQL(newC.Comment, string.Empty, string.Empty, 0, false));
                }
            }
            try
            {
                if (sqls.Count != 0)
                {
                    ctx.ExecSqls(sqls);
                }
            }
            finally
            {
                ctx.Revert(Target);
            }
        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            ctx.Revert(Target);
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataGridControllerResult = new DataGridController();
            //DataGridControllerResult.Context = null;
            DataGridControllerResult.Grid = dataGridResult;
            //Dispatcher.Invoke(Fetch, DispatcherPriority.ApplicationIdle);
            CommandBinding b;
            b = new CommandBinding(ApplicationCommands.Find, FindCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            dataGridResult.CommandBindings.Add(b);
            b = new CommandBinding(QueryCommands.NormalizeSQL, textBoxConditionCommandNormalizeSql_Executed);
            textBoxCondition.CommandBindings.Add(b);
            VisibleLevel = HiddenLevel.Hidden;
        }

        private void textBoxConditionCommandNormalizeSql_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxCondition.Text = Target.Context.NormalizeSQL(textBoxCondition.Text, CaseRule.Lowercase, CaseRule.Lowercase);
            e.Handled = true;
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataGridControllerResult.Save();
            }
            catch (Exception t)
            {
                Db2SourceContext ctx = Target.Context;
                ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, Target.Context.LastSql);
                return;
            }
        }

        private void ButtonRevert_Click(object sender, RoutedEventArgs e)
        {
            DataGridControllerResult.Revert();
            Fetch();
        }

        private void dataGridResult_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            Row row = new Row(DataGridControllerResult);
            //DataGridControllerResult.Rows.Add(row);
            e.NewItem = row;
        }

        private void dataGridResult_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            //Row row = e.NewItem as Row;
            //row.SetOwner(DataGridControllerResult);
            //DataGridControllerResult.Rows.TemporaryRows.Add(row);
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            DataGridControllerResult.NewRow();
        }

        private void dataGridTrigger_LayoutUpdated(object sender, EventArgs e)
        {
            if (Target == null || Target.Triggers.Count == 0)
            {
                return;
            }
            if (dataGridTrigger.SelectedItem != null)
            {
                return;
            }
            dataGridTrigger.SelectedItem = Target.Triggers[0];
        }

        private ColumnFilterWindow _columnFilterWindow = null;
        private object _columnFilterWindowLock = new object();
        private ColumnFilterWindow GetColumnFilterWindow()
        {
            if (_columnFilterWindow != null)
            {
                return _columnFilterWindow;
            }
            lock (_columnFilterWindowLock)
            {
                if (_columnFilterWindow != null)
                {
                    return _columnFilterWindow;
                }
                _columnFilterWindow = new ColumnFilterWindow();

                _columnFilterWindow.Owner = Window.GetWindow(this);
                return _columnFilterWindow;
            }
        }

        //private void UpdateButtonAddJoinContextMenu()
        //{
        //    Dictionary<JoinTable, List<ForeignKeyConstraint>> dict = new Dictionary<JoinTable, List<ForeignKeyConstraint>>();
        //    foreach (JoinTable join in JoinTables)
        //    {
        //        Table tbl = join.Table as Table;
        //        if (tbl == null)
        //        {
        //            continue;
        //        }
        //        List<ForeignKeyConstraint> l = new List<ForeignKeyConstraint>(tbl.ReferTo);
        //        dict.Add(join, l);
        //    }
        //    foreach (JoinTable join in JoinTables)
        //    {
        //        if (join.Referrer == null)
        //        {
        //            continue;
        //        }
        //        List<ForeignKeyConstraint> l;
        //        if (dict.TryGetValue(join.Referrer, out l))
        //        {
        //            l.Remove(join.JoinBy);
        //        }
        //    }
        //    buttonAddJoinContextMenu.Items.Clear();
        //}

        private void buttonFilterColumns_Click(object sender, RoutedEventArgs e)
        {
            ColumnFilterWindow w = GetColumnFilterWindow();
            if (w.IsVisible)
            {
                return;
            }
            w.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight / 2;
            w.Grid = dataGridResult;
            w.StartIndex = 1;
            WindowUtil.MoveFormNearby(w, buttonFilterColumns, false, false);
            w.Closed += ColumnFilterWindow_Closed;
            w.Show();
        }

        private void ColumnFilterWindow_Closed(object sender, EventArgs e)
        {
            _columnFilterWindow = null;
        }

        private void checkBoxShowHidden_Click(object sender, RoutedEventArgs e)
        {
            UpdateDataGridColumns();
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

        //private WeakReference<SearchDataGridTextWindow> _searchDataGridTextWindow = null;
        private WeakReference<SearchDataGridWindow> _searchWindowDataGridColumns = null;
        private SearchDataGridWindow RequireSearchWindowDataGridColumns()
        {
            SearchDataGridWindow win;
            if (_searchWindowDataGridColumns != null && _searchWindowDataGridColumns.TryGetTarget(out win))
            {
                return win;
            }
            win = new SearchDataGridWindow();
            win.Target = dataGridColumns;
            win.Owner = Window.GetWindow(this);
            win.Closed += SearchWindowDataGridColumns_Closed;
            _searchWindowDataGridColumns = new WeakReference<SearchDataGridWindow>(win);
            return win;
        }

        private void SearchWindowDataGridColumns_Closed(object sender, EventArgs e)
        {
            _searchWindowDataGridColumns = null;
        }

        private void FindCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (tabControlMain.SelectedItem == tabItemData)
            {
                DataGridControllerResult.ShowSearchWinodow();
            }
            else if (tabControlMain.SelectedItem == tabItemInfo)
            {
                SearchDataGridWindow win = RequireSearchWindowDataGridColumns();
                win.Show();
            }
        }

        private void FindNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (tabControlMain.SelectedItem == tabItemData)
            {
                DataGridControllerResult.SearchGridTextForward();
            }
            else if (tabControlMain.SelectedItem == tabItemInfo)
            {

            }
        }

        private void FindPreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridControllerResult.SearchGridTextBackward();
        }

        private void buttonSearchWord_Click(object sender, RoutedEventArgs e)
        {
            ApplicationCommands.Find.Execute(null, dataGridResult);
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
            if (!DataGridControllerResult.IsModified)
            {
                return;
            }
            MainWindow.TabBecomeVisible(this);
            MessageBoxResult ret = MessageBox.Show(string.Format("\"{0}\"の変更が保存されていません。保存しますか?", Target.DisplayName),
                "確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
            switch (ret)
            {
                case MessageBoxResult.Yes:
                    DataGridControllerResult.Save();
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    cancel = true;
                    break;
            }
        }

        public void OnTabClosed(object sender) { }

        private bool HasReferenceRecord(DataGridCellInfo cell)
        {
            ColumnInfo col = cell.Column.Header as ColumnInfo;
            Row row = cell.Item as Row;
            if (col == null || row == null)
            {
                return false;
            }
            if (row[col.Index] == null)
            {
                return false;
            }
            foreach (Constraint c in Target.Constraints)
            {
                ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                if (fc == null)
                {
                    continue;
                }
                foreach (string s in fc.Columns)
                {
                    if (string.Compare(s, col.Name, true) == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void DataGridCellRefColumnStyleButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataGridCell cur = btn.TemplatedParent as DataGridCell;
            if (cur == null)
            {
                return;
            }
            ColumnInfo col = cur.Column.Header as ColumnInfo;
            Row row = cur.DataContext as Row;

            FrameworkElement cell = cur.Content as FrameworkElement;

            if (col == null || row == null || cell == null)
            {
                return;
            }
            RecordViewerWindow win = new RecordViewerWindow();
            win.Table = Target;
            win.Column = col;
            win.Row = row;
            App.ShowNearby(win, cur, NearbyLocation.RightSideTop);
        }

        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = e.OriginalSource as DataGridCell;
            if (cell == null)
            {
                return;
            }
            if (!(cell.DataContext is Row))
            {
                // NewItemPlaceHolderで編集モードに入ると行が挿入されるため編集モードに入らない
                return;
            }
            DataGrid gr = App.FindVisualParent<DataGrid>(cell);
            if (gr != null)
            {
                gr.BeginEdit(e);
            }
        }

        private void buttonAddJoin_Click(object sender, RoutedEventArgs e)
        {
            ContentPresenter obj = App.FindVisualParent<ContentPresenter>(sender as DependencyObject);
            ContextMenu menu = Resources["ContextMenuJoinTableCandidates"] as ContextMenu;
            JoinTable joinTable = obj.Content as JoinTable;
            menu.DataContext = joinTable;
            menu.ItemsSource = null;
            menu.ItemsSource = (joinTable.Table as Table).ReferTo;
            menu.PlacementTarget = sender as UIElement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void dataGridResult_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = e.OriginalSource as TextBox;
            if (tb == null)
            {
                return;
            }
            DataGrid gr = sender as DataGrid;
            ItemCollection rows = gr.Items;
            DataGridCellInfo cell = dataGridResult.CurrentCell;
            int r0 = rows.IndexOf(cell.Item);
            int r = r0;
            int c0 = gr.Columns.IndexOf(cell.Column);
            int c = c0;
            switch (e.Key)
            {
                case Key.Up:
                    if (0 < tb.GetLineIndexFromCharacterIndex(tb.SelectionStart))
                    {
                        return;
                    }
                    if (r0 == 0)
                    {
                        return;
                    }
                    r--;
                    cell = new DataGridCellInfo(rows[r], cell.Column);
                    gr.CurrentCell = cell;
                    gr.SelectedCells.Clear();
                    gr.SelectedCells.Add(cell);
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (tb.GetLineIndexFromCharacterIndex(tb.SelectionStart + tb.SelectionLength) < tb.LineCount - 1)
                    {
                        return;
                    }
                    if (rows.Count - 1 <= r0)
                    {
                        return;
                    }
                    r++;
                    cell = new DataGridCellInfo(rows[r], cell.Column);
                    gr.CurrentCell = cell;
                    gr.SelectedCells.Clear();
                    gr.SelectedCells.Add(cell);
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (0 < tb.SelectionStart || 0 < tb.SelectionLength)
                    {
                        return;
                    }
                    for (c--; 0 <= c && gr.Columns[c].Visibility != Visibility.Visible; c--) ;
                    if (c < 0)
                    {
                        return;
                    }
                    cell = new DataGridCellInfo(rows[r], gr.Columns[c]);
                    gr.CurrentCell = cell;
                    gr.SelectedCells.Clear();
                    gr.SelectedCells.Add(cell);
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (tb.SelectionStart < tb.Text.Length)
                    {
                        return;
                    }
                    for (c++; c < gr.Columns.Count && gr.Columns[c].Visibility != Visibility.Visible; c++) ;
                    if (gr.Columns.Count <= c)
                    {
                        return;
                    }
                    cell = new DataGridCellInfo(rows[r], gr.Columns[c]);
                    gr.CurrentCell = cell;
                    gr.SelectedCells.Clear();
                    gr.SelectedCells.Add(cell);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    if (0 < tb.GetLineIndexFromCharacterIndex(tb.SelectionStart))
                    {
                        return;
                    }
                    gr.CancelEdit();
                    break;
                case Key.PageDown:
                    if (tb.GetLineIndexFromCharacterIndex(tb.SelectionStart + tb.SelectionLength) < tb.LineCount - 1)
                    {
                        return;
                    }
                    gr.CancelEdit();
                    break;
                case Key.Home:
                    if (0 < tb.SelectionStart || 0 < tb.SelectionLength)
                    {
                        return;
                    }
                    gr.CancelEdit();
                    break;
                case Key.End:
                    if (tb.SelectionStart < tb.Text.Length)
                    {
                        return;
                    }
                    gr.CancelEdit();
                    break;
            }
        }

        private void dataGridResult_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Row row = e.Row.Item as Row;
            if (row != null && row.IsDeleted && (e.Column is DataGridTextColumn))
            {
                e.Cancel = true;
            }
        }

        private void buttonSearchSchema_Click(object sender, RoutedEventArgs e)
        {
            SearchDataGridWindow win = new SearchDataGridWindow();
            FrameworkElement elem = sender as FrameworkElement ?? dataGridColumns;
            WindowLocator.LocateNearby(elem, win, NearbyLocation.UpLeft);
            win.Owner = Window.GetWindow(this);
            win.Target = dataGridColumns;
            win.Show();
        }

        private void MenuItemAddJoin_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            if (item == null)
            {
                return;
            }
            ForeignKeyConstraint cons = item.DataContext as ForeignKeyConstraint;
            if (cons == null)
            {
                return;
            }
            ContextMenu menu = App.FindVisualParent<ContextMenu>(item);
            if (menu == null)
            {
                return;
            }
            JoinTable join = menu.DataContext as JoinTable;
            JoinTables.Add(new JoinTable(join, cons));
        }

        private void TextBoxAlias_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSelectSql();
        }

        private void TextBoxAlias_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tb = sender as TextBox;
                BindingExpression b = tb.GetBindingExpression(TextBox.TextProperty);
                b.UpdateSource();
            }
        }

        private void buttonCloseJoin_Click(object sender, RoutedEventArgs e)
        {
            ContentPresenter obj = App.FindVisualParent<ContentPresenter>(sender as DependencyObject);
            if (obj == null)
            {
                return;
            }
            JoinTable join = obj.DataContext as JoinTable;
            JoinTables.Remove(join);
        }
    }

    public class NotNullTextConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null && (bool)value) ? "○" : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TimingItem
    {
        public TriggerTiming? Value { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
    public class PgsqlColumnArrayToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            string[] strs = value as string[];
            if (strs.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('(');
            buf.Append(NpgsqlDataSet.GetEscapedPgsqlIdentifier(strs[0]));
            for (int i = 1; i < strs.Length; i++)
            {
                buf.Append(", ");
                buf.Append(NpgsqlDataSet.GetEscapedPgsqlIdentifier(strs[i]));
            }
            buf.Append(')');
            return buf.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            string csv = value.ToString();
            if (string.IsNullOrEmpty(csv))
            {
                return csv;
            }
            if (csv[0] == '(')
            {
                csv = csv.Substring(1);
            }
            if (csv[csv.Length - 1] == ')')
            {
                csv = csv.Substring(0, csv.Length - 1);
            }
            string[] strs = csv.Split(',');
            for (int i = 0; i < strs.Length; i++)
            {
                strs[i] = Db2SourceContext.DequoteIdentifier(strs[i].Trim());
            }
            return strs;
        }
    }

    public class ForeignKeyRuleConverter : IValueConverter
    {
        private static Dictionary<ForeignKeyRule, string> _ruleToText = new Dictionary<ForeignKeyRule, string>()
        {
            { ForeignKeyRule.NoAction, "NO ACTION" },
            { ForeignKeyRule.Restrict, "RESTICT" },
            { ForeignKeyRule.Cascade, "CASCADE" },
            { ForeignKeyRule.SetNull, "SET NULL" },
            { ForeignKeyRule.SetDefault, "SET DEFAULT" },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ForeignKeyRule))
            {
                return null;
            }
            ForeignKeyRule rule = (ForeignKeyRule)value;
            return _ruleToText[rule];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RefButtonVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null || value is DBNull || (value is string && (string)value == string.Empty)) ? Visibility.Collapsed : Visibility.Visible;
            //return (cell.DataContext == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class JoinKindToVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            JoinKind k = (JoinKind)value;
            return (k == JoinKind.Root) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class JoinTableCandidatesCountToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection l = value as ICollection;
            if (l == null)
            {
                return false;
            }
            return 0 < l.Count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
