using System;
using System.Collections;
using System.Collections.Generic;
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
        public static readonly DependencyProperty IsTargetModifiedProperty = DependencyProperty.Register("IsTargetModified", typeof(bool), typeof(TableControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(TableControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(TableControl));

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

        public TableControl()
        {
            InitializeComponent();
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
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
                    reader = command.ExecuteReader();
                    DataGridControllerResult.Load(reader, Target);
                }
                catch (Exception t)
                {
                    Target.Context.OnLog(t.Message, LogStatus.Error, command.CommandText);
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
                    buf.AppendLine(ctx.GetSQL(Target, string.Empty, ";", 0, true, IsChecked(checkBoxSourceKeyCons)));
                }
                string consBase = string.Format("alter table {0} add ", Target.EscapedIdentifier(null));
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
                                buf.Append(ctx.GetSQL(c, consBase, ";", 0, true));
                            }
                            break;
                        case ConstraintType.Unique:
                            if (IsChecked(checkBoxSourceKeyCons))
                            {
                                buf.Append(ctx.GetSQL(c, consBase, ";", 0, true));
                            }
                            break;
                        case ConstraintType.ForeignKey:
                            if (IsChecked(checkBoxSourceRefCons))
                            {
                                buf.Append(ctx.GetSQL(c, consBase, ";", 0, true));
                            }
                            break;
                        case ConstraintType.Check:
                            if (IsChecked(checkBoxSourceCons))
                            {
                                buf.Append(ctx.GetSQL(c, consBase, ";", 0, true));
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
            textBoxSelectSql.Text = Target.GetSelectSQL(Target.GetKeyConditionSQL(string.Empty, string.Empty, 0), string.Empty, null, false);
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
            string sql = Target.GetSelectSQL(textBoxCondition.Text, orderby, limit, false, out offset);
            try
            {
                using (IDbConnection conn = ctx.NewConnection())
                {
                    using (IDbCommand cmd = ctx.GetSqlCommand(sql, null, conn))
                    {
                        UpdateDataGridResult(cmd);
                    }
                }
            }
            catch (Exception t)
            {
                ctx.OnLog(t.Message, LogStatus.Error, sql);
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
                Target.Context.OnLog(t.Message, LogStatus.Error, Target.Context.LastSql);
                return;
            }
        }

        private void ButtonRevert_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void dataGridResult_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            DataGridController.Row row = new DataGridController.Row(DataGridControllerResult);
            //DataGridControllerResult.Rows.Add(row);
            e.NewItem = row;
        }

        private void dataGridResult_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            DataGridController.Row row = e.NewItem as DataGridController.Row;
            //row.SetOwner(DataGridControllerResult);
            DataGridControllerResult.Rows.TemporaryRows.Add(row);
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            DataGridControllerResult.NewRow();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            List<DataGridController.Row> selected = new List<DataGridController.Row>();
            foreach (DataGridController.Row row in DataGridControllerResult.Rows)
            {
                if (row.IsChecked)
                {
                    selected.Add(row);
                }
            }
            MessageBoxResult ret;
            if (selected.Count == 0)
            {
                ret = MessageBox.Show("チェックを入れた行がありません。選択中の行を削除しますか?", "削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (ret != MessageBoxResult.Yes)
                {
                    return;
                }
                foreach (DataGridController.Row row in dataGridResult.SelectedItems)
                {
                    selected.Add(row);
                }
            }
            ret = MessageBox.Show(string.Format("{0}行を削除しますか?", selected.Count), "削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            foreach (DataGridController.Row row in selected)
            {
                DataGridControllerResult.Rows.Remove(row);
            }
            try
            {
                DataGridControllerResult.Save();
            }
            catch (Exception t)
            {
                Target.Context.OnLog(t.Message, LogStatus.Error, Target.Context.LastSql);
                return;
            }
            dataGridResult.ItemsSource = null;
            dataGridResult.ItemsSource = DataGridControllerResult.Rows;
        }

        private void dataGridTrigger_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            Trigger t = dataGridTrigger.SelectedItem as Trigger;
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

        private void dataGridTrigger_SourceUpdated(object sender, DataTransferEventArgs e)
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
            DataGridCommands.CopyTable.Execute(null, dataGridResult);
        }

        //private WeakReference<SearchDataGridTextWindow> _searchDataGridTextWindow = null;
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
            if (!DataGridControllerResult.IsModified)
            {
                return;
            }
            MessageBoxResult ret = MessageBox.Show("変更が保存されていません。保存しますか?", "確認", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
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
}
