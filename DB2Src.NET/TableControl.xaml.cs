﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static Db2Source.QueryHistory;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Db2Source
{
    public class RefTable
    {
        public static void AddRefTables(List<RefTable> list, IEnumerable<ForeignKeyConstraint> foreignKeys, JoinDirection direction)
        {
            foreach (ForeignKeyConstraint cons in foreignKeys)
            {
                list.Add(new RefTable(cons, direction));
            }
        }
        public ForeignKeyConstraint Constraint { get; private set; }
        public JoinDirection Direction { get; private set; }
        public RefTable(ForeignKeyConstraint constraint, JoinDirection direction)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException("constraint");
            }
            Constraint = constraint;
            Direction = direction;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RefTable))
            {
                return false;
            }
            RefTable o = (RefTable)obj;
            return object.Equals(Constraint, o.Constraint) && Direction == o.Direction;
        }
        public override int GetHashCode()
        {
            return Constraint.GetHashCode() * 2 + (int)Direction;
        }

        public override string ToString()
        {
            switch (Direction)
            {
                case JoinDirection.ReferFrom:
                    return string.Format("{0} → {1}{2}", StrUtil.ArrayToText(Constraint.RefColumns, ", ", "(", ")"), Constraint.Table.FullName,
                        StrUtil.ArrayToText(Constraint.Columns, ", ", "(", ")"));
                case JoinDirection.ReferTo:
                    return string.Format("{0} → {1}", StrUtil.ArrayToText(Constraint.Columns, ", ", "(", ")"), Constraint.ReferenceConstraint.Table.FullName);
                default:
                    throw new NotImplementedException();
            }
        }
    }
    /// <summary>
    /// TableControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TableControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Table), typeof(TableControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        public static readonly DependencyProperty JoinTablesProperty = DependencyProperty.Register("JoinTables", typeof(JoinTableCollection), typeof(TableControl), new PropertyMetadata(new PropertyChangedCallback(OnJoinTablesPropertyChanged)));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(TableControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(TableControl));
        public static readonly DependencyProperty VisibleLevelProperty = DependencyProperty.Register("VisibleLevel", typeof(HiddenLevel), typeof(TableControl), new PropertyMetadata(new PropertyChangedCallback(OnVisibleLevelPropertyChanged)));
        //public static readonly DependencyProperty SelectedTriggerProperty = DependencyProperty.Register("SelectedTrigger", typeof(Trigger), typeof(TableControl));

        private TableSetting _setting = null;
        private HiddenLevelDisplayItem[] HiddenLevelItems = new HiddenLevelDisplayItem[0];
        private void UpdateHiddenLevelDisplayItems()
        {
            if (Target == null)
            {
                return;
            }
            Column oid = Target.Columns["oid"];
            bool hasOid = (oid != null && oid.HiddenLevel == HiddenLevel.Hidden);
            HiddenLevel lv = hasOid ? HiddenLevel.Hidden : HiddenLevel.Visible;
            HiddenLevelDisplayItem sel = null;
            List<HiddenLevelDisplayItem> l = new List<HiddenLevelDisplayItem>();
            foreach (HiddenLevelDisplayItem item in (HiddenLevelDisplayItem[])FindResource("DefaultHiddenLevelDisplayItems"))
            {
                if (item.Level == lv)
                {
                    sel = item;
                }
                if (hasOid || item.Level != HiddenLevel.Hidden)
                {
                    l.Add(item);
                }
            }
            HiddenLevelItems = l.ToArray();
            comboBoxSystemColumn.ItemsSource = HiddenLevelItems;
            comboBoxSystemColumn.SelectedItem = sel;
        }
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

        private static readonly string[] _settingControlNames = new string[] {
            "checkBoxDrop", "checkBoxSourceMain", "checkBoxSourceKeyCons", "checkBoxSourceRefCons", "checkBoxSourceCons",
            "checkBoxSourceComment", "checkBoxSourceTrigger", "checkBoxSourceIndex",
            "checkBoxSourceReferred", "checkBoxSourceDropReferred", "checkBoxUpsert" };
        public string[] SettingCheckBoxNames { get { return _settingControlNames; } }

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

        //public Trigger SelectedTrigger
        //{
        //    get
        //    {
        //        return (Trigger)GetValue(SelectedTriggerProperty);
        //    }
        //    set
        //    {
        //        SetValue(SelectedTriggerProperty, value);
        //    }
        //}

        private CompleteFieldController _dropDownController;

        public TableControl()
        {
            InitializeComponent();
            JoinTables = new JoinTableCollection();
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (DataGridControllerResult == null)
                {
                    return;
                }
                JoinTables.Clear();
                JoinTable jt = new JoinTable() { Alias = "a", Table = Target, Kind = JoinKind.Root };
                jt.PropertyChanged += JoinTable_PropertyChanged;
                JoinTables.Add(jt);
                //dataGridColumns.ItemsSource = Target.Columns;
                DataGridControllerResult.Table = Target;
                sortFields.Target = Target;
                //dataGridReferTo.ItemsSource = Target.ReferTo;
                //dataGridReferedBy.ItemsSource = Target.ReferFrom;
                _dropDownController.Target = Target;
                UpdateTextBoxSource();
                UpdateTextBoxTemplateSql();
                UpdateHiddenLevelDisplayItems();
                _setting = TableSetting.Require(Target);
                _setting?.Load(this);
                AutoFetch();
            });
        }
        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TableControl)?.OnTargetPropertyChanged(e);
        }

        private void JoinTable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Alias")
            {
                DelayedUpdateTextBoxSelectSql();
            }
        }

        private void OnJoinTablesPropertyChanged(DependencyPropertyChangedEventArgs e)
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

        private static void OnJoinTablesPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TableControl)?.OnJoinTablesPropertyChanged(e);
        }


        private void JoinTables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListBoxJoinTable();
            DelayedUpdateTextBoxSelectSql();
        }

        private void OnVisibleLevelPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
            {
                return;
            }
            TryRefetch();
        }

        private static void OnVisibleLevelPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TableControl)?.OnVisibleLevelPropertyChanged(e);
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

        private static void GetDropSQLByIdentifier(StringBuilder buffer, Db2SourceContext context, string identifier, Dictionary<string, bool> dropped)
        {
            if (dropped.ContainsKey(identifier))
            {
                return;
            }
            dropped[identifier] = true;
            SchemaObject obj = context.Objects[identifier];
            foreach (string id in obj.DependBy)
            {
                GetDropSQLByIdentifier(buffer, context, identifier, dropped);
            }
            object[] args = new object[] { obj, true, string.Empty, ";", (int)0, false, true };
            Type[] argTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                argTypes[i] = args[i].GetType();
            }
            MethodInfo method = context.GetType().GetMethod("GetDropSQL", argTypes);
            if (method == null)
            {
                throw new NotImplementedException();
            }
            string[] sqls = (string[])method.Invoke(context, args);
            foreach (string s in sqls)
            {
                buffer.Append(s);
            }
            buffer.AppendLine();
        }

        private static void GetSQLByIdenrtifier(StringBuilder buffer, Db2SourceContext context, string identifier, Dictionary<string, bool> created)
        {
            if (created.ContainsKey(identifier))
            {
                return;
            }
            created[identifier] = true;
            SchemaObject obj = context.Objects[identifier];
            object[] args = new object[] { obj, string.Empty, ";", (int)0, true };
            Type[] argTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                argTypes[i] = args[i].GetType();
            }
            MethodInfo method = context.GetType().GetMethod("GetSQL", argTypes);
            if (method == null)
            {
                throw new NotImplementedException();
            }
            string[] sqls = (string[])method.Invoke(context, args);
            foreach (string s in sqls)
            {
                buffer.Append(s);
            }
            buffer.AppendLine();

            foreach (string id in obj.DependBy)
            {
                GetSQLByIdenrtifier(buffer, context, identifier, created);
            }
        }

        private bool _isTextBoxSourceUpdating = false;
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
                    int lastLength = buf.Length;
                    if (IsChecked(checkBoxSourceDropReferred))
                    {
                        foreach (ForeignKeyConstraint f in Target.ReferFrom)
                        {
                            foreach (string s in ctx.GetDropSQL(f, true, string.Empty, ";", 0, false, true))
                            {
                                buf.Append(s);
                            }
                        }
                        Dictionary<string, bool> dropped = new Dictionary<string, bool>();
                        foreach (string id in Target.DependBy)
                        {
                            GetDropSQLByIdentifier(buf, ctx, id, dropped);
                        }
                        if (lastLength < buf.Length)
                        {
                            buf.AppendLine();
                        }
                    }
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
                        foreach (string s in ctx.GetSQL(Target, string.Empty, ";", 0, true, true))
                        {
                            buf.Append(s);
                        }
                    }
                    List<Constraint> list = new List<Constraint>(Target.Constraints);
                    list.Sort();
                    lastLength = buf.Length;
                    foreach (Constraint c in list)
                    {
                        switch (c.ConstraintType)
                        {
                            case ConstraintType.Primary:
                                if (!IsChecked(checkBoxSourceMain) && IsChecked(checkBoxSourceKeyCons))
                                {
                                    // 本体ソース内で出力しているので本体を出力しない場合のみ
                                    foreach (string s in ctx.GetSQL(c, string.Empty, ";", 0, true, true))
                                    {
                                        buf.Append(s);
                                    }
                                }
                                break;
                            case ConstraintType.Unique:
                                if (IsChecked(checkBoxSourceKeyCons))
                                {
                                    foreach (string s in ctx.GetSQL(c, string.Empty, ";", 0, true, true))
                                    {
                                        buf.Append(s);
                                    }
                                }
                                break;
                            case ConstraintType.ForeignKey:
                                if (IsChecked(checkBoxSourceRefCons))
                                {
                                    foreach (string s in ctx.GetSQL(c, string.Empty, ";", 0, true, true))
                                    {
                                        buf.Append(s);
                                    }
                                }
                                break;
                            case ConstraintType.Check:
                                if (IsChecked(checkBoxSourceCons))
                                {
                                    foreach (string s in ctx.GetSQL(c, string.Empty, ";", 0, true, true))
                                    {
                                        buf.Append(s);
                                    }
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
                            foreach (string s in ctx.GetSQL(t, string.Empty, ";", 0, true))
                            {
                                buf.Append(s);
                            }
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
                            foreach (string s in ctx.GetSQL(i, string.Empty, ";", 0, true))
                            {
                                buf.Append(s);
                            }
                        }
                        if (lastLength < buf.Length)
                        {
                            buf.AppendLine();
                        }
                    }
                    if (IsChecked(checkBoxSourceReferred))
                    {
                        lastLength = buf.Length;
                        foreach (ForeignKeyConstraint f in Target.ReferFrom)
                        {
                            foreach (string s in ctx.GetSQL(f, string.Empty, ";", 0, true, true))
                            {
                                buf.Append(s);
                            }
                        }
                        Dictionary<string, bool> created = new Dictionary<string, bool>();
                        foreach (string id in Target.DependBy)
                        {
                            GetSQLByIdenrtifier(buf, ctx, id, created);
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

        private void UpdateTextBoxSelectSql()
        {
            _textBoxSelectSqlUpdating = false;
            if (textBoxSelectSql == null)
            {
                return;
            }
            if (Target == null)
            {
                return;
            }
            if (JoinTables.Count == 0)
            {
                textBoxSelectSql.Text = string.Empty;
                return;
            }
            string alias = JoinTables[0].Alias;
            string where = Target.GetKeyConditionSQL(alias, string.Empty, MainWindow.Current.IndentOffset, false);
            string sql = JoinTables.GetSelectSQL(where, string.Empty, null, MainWindow.Current.IndentOffset, 80);
            textBoxSelectSql.Text = sql;
        }

        private bool _textBoxSelectSqlUpdating;
        private void DelayedUpdateTextBoxSelectSql()
        {
            if (!_textBoxSelectSqlUpdating)
            {
                Dispatcher.InvokeAsync(UpdateTextBoxSelectSql);
                _textBoxSelectSqlUpdating = true;
            }
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
            try
            {
                if (checkBoxUpsert.IsChecked ?? false)
                {
                    textBoxInsertSql.Text = (Target != null) ? Target.GetUpsertSql(MainWindow.Current.IndentOffset, 80, string.Empty) : string.Empty;
                }
                else
                {
                    textBoxInsertSql.Text = Target?.GetInsertSql(MainWindow.Current.IndentOffset, 80, string.Empty, false);
                }
            }
            catch (Exception t)
            {
                textBoxInsertSql.Text = t.ToString();
            }
        }
        private void UpdateTextBoxUpdateSql()
        {
            if (textBoxUpdateSql == null)
            {
                return;
            }
            textBoxUpdateSql.Text = (Target != null) ? Target.GetUpdateSql(Target.GetKeyConditionSQL(string.Empty, "where ", MainWindow.Current.IndentOffset, false), MainWindow.Current.IndentOffset, 80, string.Empty) : string.Empty;
        }

        private void UpdateTextBoxDeleteSql()
        {
            if (textBoxDeleteSql == null)
            {
                return;
            }
            textBoxDeleteSql.Text = (Target != null) ? Target.GetDeleteSql(Target.GetKeyConditionSQL(string.Empty, "where ", MainWindow.Current.IndentOffset, false), MainWindow.Current.IndentOffset, 80, string.Empty) : string.Empty;
        }

        private void UpdateTextBoxMergeSql()
        {
            if (textBoxMergeSql == null)
            {
                return;
            }
            // MERGE文はPostgreSQL15以降の機能
            bool enabled = (Target != null && 15 <= Target.Context.Database.VersionNum[0]);
            tabItemMergeSql.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                textBoxMergeSql.Text = enabled ? Target.GetMergeSql(MainWindow.Current.IndentOffset, 80, string.Empty) : string.Empty;
            }
            catch (Exception t)
            {
                textBoxMergeSql.Text = t.ToString();
            }
        }

        private bool _isTextBoxTemplateSqlUpdatng = false;
        private void UpdateTextBoxTemplateSql()
        {
            DelayedUpdateTextBoxSelectSql();
            UpdateTextBoxInsertSql();
            UpdateTextBoxUpdateSql();
            UpdateTextBoxDeleteSql();
            UpdateTextBoxMergeSql();
            _isTextBoxTemplateSqlUpdatng = false;
        }

        private void DelayedUpdateTextBoxTemplateSql()
        {
            if (_isTextBoxTemplateSqlUpdatng)
            {
                return;
            }
            _isTextBoxTemplateSqlUpdatng = true;
            Dispatcher.InvokeAsync(UpdateTextBoxTemplateSql, DispatcherPriority.ApplicationIdle);
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
        private bool _fetched = false;
        private QueryFaith _fetchingFaith = QueryFaith.Idle;
        private readonly object _fetchingFaithLock = new object();
        private DispatcherTimer _fetcingCooldownTimer = null;
        private CancellationTokenSource _fetchingCancellation = null;

        private void UpdateControlsIsEnabled()
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
            textBoxCondition.IsEnabled = (_fetchingFaith == QueryFaith.Idle);
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

                _fetcingCooldownTimer?.Stop();
                _fetcingCooldownTimer = null;
            }
            Dispatcher.InvokeAsync(UpdateControlsIsEnabled);
            _inAutoFetching = false;
        }

		private static string GetDurationText(TimeSpan time)
		{
			return string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
		}

		private async Task ExecuteCommandAsync(Dispatcher dispatcher, Db2SourceContext dataSet, DataGridController controller, Table table, IDbCommand command)
        {
            DateTime start = DateTime.Now;
            DateTime sqlEnd = start;
            try
            {
                using (IDataReader reader = await dataSet.ExecuteReaderAsync(command, _fetchingCancellation.Token))
                {
                    sqlEnd = DateTime.Now;
                    await controller.LoadAsync(dispatcher, reader, table, _fetchingCancellation.Token);
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
				string s1 = GetDurationText(sqlEnd - start);
				string s2 = GetDurationText(end - start);
				await dispatcher.InvokeAsync(() =>
                {
					textBlockGridResult.Text = string.Format((string)FindResource("messageRowsFound"), controller.Rows.Count, s1, s2);
                });
                command.Dispose();
            }
        }

        private async Task ExecuteSqlAsync(Dispatcher dispatcher, Db2SourceContext dataSet, DataGridController controller, Table table, string sql, int offset)
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
                            await ExecuteCommandAsync(dispatcher, dataSet, controller, table, cmd);
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
            _fetched = true;
        }

        public void Fetch(string condition)
        {
            textBoxCondition.Text = condition;
            Fetch();
        }
        public void Fetch()
        {
            Fetch(false);
        }
        public void Fetch(bool force)
        {
            dataGridResult.CommitEdit();
            _fetched = false;
            if (!force && DataGridControllerResult.IsModified)
            {
                Window owner = Window.GetWindow(this);
                MessageBoxResult ret = MessageBox.Show(owner, (string)FindResource("messageConfirmSave"), Properties.Resources.MessageBoxCaption_Confirm, MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
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
                    Window owner = Window.GetWindow(this);
                    MessageBox.Show(owner, (string)FindResource("messageInvalidLimitRow"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxLimitRow.Focus();
                }
                limit = l;
            }
            string orderby = sortFields.GetOrderBySql(string.Empty);
            int offset;
            string sql = Target.GetSelectSQL(null, textBoxCondition.Text, orderby, limit, VisibleLevel, out offset, 0, 80);
            StartFetching();
            Dispatcher dispatcher = Dispatcher;
            DataGridController controller = DataGridControllerResult;
            Table table = Target;
            Task _ = ExecuteSqlAsync(dispatcher, ctx, controller, table, sql, offset);
        }

        private void TryRefetch()
        {
            if (!_fetched)
            {
                return;
            }
            Fetch();
        }

        public void DropTarget(bool cascade)
        {
            if (Target == null)
            {
                return;
            }
            Window owner = Window.GetWindow(this);
            Db2SourceContext ctx = Target.Context;
            string[] sql = ctx.GetDropSQL(Target, true, string.Empty, string.Empty, 0, cascade, cascade);
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
            VisibleLevel = HiddenLevel.Hidden;
            _dropDownController = new CompleteFieldController(Target, textBoxCondition);
            MainWindow.Current.IndentPropertyChanged += MainWindow_IndentPropertyChanged;
            MainWindow.Current.IndentCharPropertyChanged += MainWindow_IndentPropertyChanged;
            MainWindow.Current.IndentOffsetPropertyChanged += MainWindow_IndentPropertyChanged;
        }

        private void MainWindow_IndentPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateTextBoxSource();
            DelayedUpdateTextBoxTemplateSql();
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

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            try
            {
                DataGridControllerResult.Save();
            }
            catch (Exception t)
            {
                Db2SourceContext ctx = Target.Context;
                ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, null);
                return;
            }
        }

        private void ButtonRevert_Click(object sender, RoutedEventArgs e)
        {
            DataGridControllerResult.Revert();
            //Fetch(true);
        }

        private void dataGridResult_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            Row row = new Row(DataGridControllerResult);
            e.NewItem = row;
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

        //private WeakReference<SearchDataGridWindow> _searchWindowDataGridColumns = null;
        //private SearchDataGridWindow RequireSearchWindowDataGridColumns()
        //{
        //    SearchDataGridWindow win;
        //    if (_searchWindowDataGridColumns != null && _searchWindowDataGridColumns.TryGetTarget(out win))
        //    {
        //        return win;
        //    }
        //    win = new SearchDataGridWindow();
        //    win.Target = dataGridColumns;
        //    win.Owner = Window.GetWindow(this);
        //    win.Closed += SearchWindowDataGridColumns_Closed;
        //    _searchWindowDataGridColumns = new WeakReference<SearchDataGridWindow>(win);
        //    return win;
        //}

        //private void SearchWindowDataGridColumns_Closed(object sender, EventArgs e)
        //{
        //    _searchWindowDataGridColumns = null;
        //}

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

        private void buttonSearchWord_Click(object sender, RoutedEventArgs e)
        {
            ApplicationCommands.Find.Execute(null, dataGridResult);
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
            _setting.Save(this);
            if (!DataGridControllerResult.IsModified)
            {
                return;
            }
            MainWindow.TabBecomeVisible(this);
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, string.Format((string)FindResource("messageConfirmSaveAndClose"), Target.DisplayName),
                Properties.Resources.MessageBoxCaption_Confirm, MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
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

        public void Dispose()
        {
            Name = null;
            Target = null;
            dataGridResult.ItemsSource = null;
            dataGridResult.CommandBindings.Clear();
            DataGridControllerResult.Rows.Clear();
            DataGridControllerResult = null;
            tableInfoControl.Dispose();
            tableInfoControl = null;
            dataGridResult = null;
            CommandBindings.Clear();
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        private bool HasReferenceRecord(DataGridCellInfo cell)
        {
            if (Target == null)
            {
                return false;
            }
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
            win.Owner = Window.GetWindow(this);
            win.Table = Target;
            win.Column = col;
            win.Row = row;
            WindowLocator.LocateNearby(cur, win, NearbyLocation.RightSideTop);
            win.Show();
        }

        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            //DataGridCell cell = e.OriginalSource as DataGridCell;
            //if (cell == null)
            //{
            //    return;
            //}
            //if (!(cell.DataContext is Row))
            //{
            //    // NewItemPlaceHolderで編集モードに入ると行が挿入されるため編集モードに入らない
            //    return;
            //}
            //DataGrid gr = App.FindVisualParent<DataGrid>(cell);
            //if (gr != null)
            //{
            //    gr.BeginEdit(e);
            //}
        }

        private void buttonAddJoin_Click(object sender, RoutedEventArgs e)
        {
            ContentPresenter obj = App.FindVisualParent<ContentPresenter>(sender as DependencyObject);
            ContextMenu menu = FindResource("ContextMenuJoinTableCandidates") as ContextMenu;
            JoinTable joinTable = obj.Content as JoinTable;
            menu.DataContext = joinTable;
            menu.ItemsSource = null;
            List<RefTable> l = new List<RefTable>();
            RefTable.AddRefTables(l, joinTable.SelectableForeignKeys, JoinDirection.ReferTo);
            RefTable.AddRefTables(l, (joinTable.Table as Table).ReferFrom, JoinDirection.ReferFrom);
            menu.ItemsSource = l;
            menu.PlacementTarget = sender as UIElement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void SelectGridCell(DataGrid grid, object row, DataGridColumn column)
        {
            grid.CommitEdit();
            DataGridCellInfo cell = new DataGridCellInfo(row, column);
            grid.CurrentCell = cell;
            grid.SelectedCells.Clear();
            grid.SelectedCells.Add(cell);
        }

        private void DelayedSelectGridCell(DataGrid grid, object row, DataGridColumn column)
        {
            Dispatcher.InvokeAsync(() => { SelectGridCell(grid, row, column); });
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
                    DelayedSelectGridCell(gr, rows[r], cell.Column);
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
                    DelayedSelectGridCell(gr, rows[r], cell.Column);
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
                    DelayedSelectGridCell(gr, rows[r], gr.Columns[c]);
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
                    DelayedSelectGridCell(gr, rows[r], gr.Columns[c]);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    if (0 < tb.GetLineIndexFromCharacterIndex(tb.SelectionStart))
                    {
                        return;
                    }
                    gr.CommitEdit();
                    break;
                case Key.PageDown:
                    if (tb.GetLineIndexFromCharacterIndex(tb.SelectionStart + tb.SelectionLength) < tb.LineCount - 1)
                    {
                        return;
                    }
                    gr.CommitEdit();
                    break;
                case Key.Home:
                    if (0 < tb.SelectionStart || 0 < tb.SelectionLength)
                    {
                        return;
                    }
                    gr.CommitEdit();
                    break;
                case Key.End:
                    if (tb.SelectionStart < tb.Text.Length)
                    {
                        return;
                    }
                    gr.CommitEdit();
                    break;
                case Key.Enter:
                case Key.Tab:
                    gr.CommitEdit();
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

        private void dataGridResult_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel && !e.Cancel)
            {
                Row row = e.Row.Item as Row;
                if (row == null)
                {
                    return;
                }
                if (e.Row.IsNewItem)
                {
                    DataGridControllerResult.Rows.Remove(row);
                }
            }
        }

        private void MenuItemAddJoin_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (sender as MenuItem);
            if (item == null)
            {
                return;
            }
            RefTable refTbl = item.DataContext as RefTable;
            if (refTbl == null)
            {
                return;
            }
            ContextMenu menu = App.FindVisualParent<ContextMenu>(item);
            if (menu == null)
            {
                return;
            }
            JoinTable join = menu.DataContext as JoinTable;
            JoinTable jt = new JoinTable(join, refTbl.Constraint, refTbl.Direction);
            jt.PropertyChanged += JoinTable_PropertyChanged;
            JoinTables.Add(jt);
        }

        private void TextBoxAlias_LostFocus(object sender, RoutedEventArgs e)
        {
            //DelayedUpdateTextBoxSelectSql();
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

        private void SetNullToCurrentCell()
        {
            DataGridCellInfo cell = dataGridResult.CurrentCell;
            if (!cell.IsValid)
            {
                return;
            }
            Row row = cell.Item as Row;
            if (row == null)
            {
                return;
            }
            ColumnInfo col = cell.Column.Header as ColumnInfo;
            if (col == null)
            {
                return;
            }
            dataGridResult.CommitEdit();
            dataGridResult.CancelEdit();
            Dispatcher.InvokeAsync(() => { row[col.Index] = DBNull.Value; });
        }

        private void buttonSetNull_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(SetNullToCurrentCell);
        }

        private void ButtonJoinTableColumns_Click(object sender, RoutedEventArgs e)
        {
            //ListBoxItem item = App.FindLogicalParent<ListBoxItem>(sender as DependencyObject);
            ListBoxItem item = App.FindVisualParent<ListBoxItem>(sender as DependencyObject);
            JoinTable tbl = item?.DataContext as JoinTable;
            if (tbl == null)
            {
                return;
            }
            ColumnCheckListWindow win = new ColumnCheckListWindow();
            win.Owner = Window.GetWindow(this);
            win.Target = tbl;
            WindowLocator.LocateNearby(sender as FrameworkElement, win, NearbyLocation.DownLeft);
            win.Closed += ColumnCheckListWindow_Closed;
            win.Show();
        }

        private void ColumnCheckListWindow_Closed(object sender, EventArgs e)
        {
            DelayedUpdateTextBoxSelectSql();
        }

        ~TableControl()
        {
            App.Log("~TableControl");
        }

        private void checkBoxUpsert_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxInsertSql();
        }

        private void checkBoxUpsert_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxInsertSql();
        }

        private void TriggerControl_TargetDropped(object sender, EventArgs e)
        {
            TriggerControl ctl = sender as TriggerControl;
            Trigger trigger = ctl?.Target;
            if (trigger == null)
            {
                return;
            }
            listBoxTrigger.ItemsSource = null;
            Target.Triggers.Remove(trigger);
            listBoxTrigger.ItemsSource = Target.Triggers;
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
}
