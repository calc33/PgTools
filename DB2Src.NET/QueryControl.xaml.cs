using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

namespace Db2Source
{
    /// <summary>
    /// QueryControl.xaml の相互作用ロジック
    /// </summary>
    public partial class QueryControl: UserControl
    {
        public static readonly DependencyProperty CurrentDataSetProperty = DependencyProperty.Register("CurrentDataSet", typeof(Db2SourceContext), typeof(QueryControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(QueryControl));

        private ParameterStoreCollection _parameters = new ParameterStoreCollection();
        private string _historyPath = System.IO.Path.Combine(Db2SourceContext.AppDataDir, "History",
            string.Format("{0:yyMMddHHmmss}-{1}.sql", DateTime.Now, System.Diagnostics.Process.GetCurrentProcess().Id));
        private void UpdateDataGridParameters()
        {
            dataGridParameters.ItemsSource = null;
            dataGridParameters.ItemsSource = _parameters;
            dataGridParameters.Visibility = _parameters.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            splitterParameters.Visibility = dataGridParameters.Visibility;
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

        public QueryControl()
        {
            InitializeComponent();
            dataGridParameters.Visibility = Visibility.Collapsed;
            splitterParameters.Visibility = Visibility.Collapsed;
            dataGridColumnDbType.ItemsSource = DbTypeInfo.DbTypeInfos;
        }
        private LogListBoxItem NewLogListBoxItem(string text, string sql, ParameterStoreCollection parameters, LogStatus status, bool notice, Tuple<int, int> errorPos)
        {
            LogListBoxItem item = new LogListBoxItem();
            item.Time = DateTime.Now;
            item.Status = status;
            item.Message = text;
            item.Sql = sql;
            item.Parameters = parameters;
            item.ErrorPosition = errorPos;
            return item;
        }
        private ErrorListBoxItem NewErrorListBoxItem(string text, Tuple<int, int> errorPos)
        {
            ErrorListBoxItem item = new ErrorListBoxItem();
            item.Message = text;
            item.ErrorPosition = errorPos;
            return item;
        }
        private void AddLog(string text, string sql, ParameterStoreCollection parameters, LogStatus status, bool notice, Tuple<int,int> errorPos = null)
        {
            LogListBoxItem item = NewLogListBoxItem(text, sql, parameters, status, notice, errorPos);
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
            ErrorListBoxItem item = sender as ErrorListBoxItem;
            if (item == null)
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
            if (string.IsNullOrEmpty(item.Sql))
            {
                return;
            }
            textBoxSql.Text = item.Sql;
            Parameters = item.Parameters;
            //Fetch();
        }

        private void Command_Log(object sender, LogEventArgs e)
        {
            AddLog(e.Text, e.Sql, null, e.Status, false);
        }
        private void AddToHistory(SQLPart sql, ParameterStoreCollection parameters)
        {
            QueryStore q = new QueryStore(sql.SQL, parameters);
            string dir = System.IO.Path.GetDirectoryName(_historyPath);
            Directory.CreateDirectory(dir);
            using (StreamWriter wr = new StreamWriter(_historyPath, true, Encoding.UTF8))
            {
                q.WriteToStream(wr);
            }
        }
        private void UpdateDataGridResult(SQLParts sqls)
        {
            Db2SourceContext ctx = CurrentDataSet;
            using (IDbConnection conn = ctx.NewConnection())
            {
                bool modified;
                Parameters = ParameterStore.GetParameterStores(sqls.ParameterNames, Parameters, out modified);
                if (modified)
                {
                    return;
                }

                foreach (ParameterStore p in Parameters)
                {
                    if (p.IsError)
                    {
                        dataGridParameters.Focus();
                        dataGridParameters.SelectedItem = p;
                        MessageBox.Show(string.Format("パラメータ{0}の値が不正です", p.ParameterName), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                foreach (SQLPart sql in sqls.Items)
                {
                    IDbCommand cmd = ctx.GetSqlCommand(sql.SQL, Command_Log, conn);
                    ParameterStoreCollection stores = ParameterStore.GetParameterStores(cmd, Parameters, out modified);
                    DateTime start = DateTime.Now;
                    try
                    {
                        using (IDataReader reader = cmd.ExecuteReader())
                        {
                            DataGridControllerResult.Load(reader);
                            if (0 <= reader.RecordsAffected)
                            {
                                AddLog(string.Format("{0}行反映しました。", reader.RecordsAffected), null, null, LogStatus.Normal, true);
                            }
                            else if (0 < reader.FieldCount)
                            {
                                tabControlResult.SelectedItem = tabItemDataGrid;
                            }
                        }
                        AddToHistory(sql, stores);
                    }
                    catch (Exception t)
                    {
                        Tuple<int, int> errPos = CurrentDataSet.GetErrorPosition(t, sql.SQL, 0);
                        AddLog(CurrentDataSet.GetExceptionMessage(t), sql.SQL, stores, LogStatus.Error, true, errPos);
                        Db2SrcDataSetController.ShowErrorPosition(t, textBoxSql, CurrentDataSet, sql.Offset);
                        return;
                    }
                    finally
                    {
                        ParameterStore.GetParameterStores(cmd, stores, out modified);
                        UpdateDataGridParameters();
                        DateTime end = DateTime.Now;
                        TimeSpan time = end - start;
                        string s = string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
                        AddLog(string.Format("実行しました (所要時間 {0})", s), cmd.CommandText, stores, LogStatus.Aux, false);
                        textBlockGridResult.Text = string.Format("{0}件見つかりました。  所要時間 {1}", DataGridControllerResult.Rows.Count, s);
                    }
                }
            }
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
            textBoxSql.Text = CurrentDataSet.NormalizeSQL(textBoxSql.Text, CaseRule.Lowercase, CaseRule.Lowercase);
            e.Handled = true;
        }

        private void Fetch()
        {
            Db2SourceContext ctx = CurrentDataSet;
            if (ctx == null)
            {
                AddLog("データベースに接続していません。", null, null, LogStatus.Error, true);
                return;
            }
            string sql = textBoxSql.Text.TrimEnd();
            if (string.IsNullOrEmpty(sql))
            {
                AddLog("SQLがありません。", null, null, LogStatus.Error, true);
                return;
            }
            SQLParts parts = ctx.SplitSQL(sql);
            if (parts.Count == 0)
            {
                AddLog("SQLがありません。", null, null, LogStatus.Error, true);
                return;
            }
            listBoxErrors.Items.Clear();
            listBoxErrors.Visibility = Visibility.Collapsed;
            UpdateDataGridResult(parts);
        }
        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            ParameterStore.AllParameters.ParameterTextChanged += Parameters_ParameterTextChanged;
            DataGridControllerResult = new DataGridController();
            //DataGridControllerResult.Context = CurrentDataSet;
            DataGridControllerResult.Grid = dataGridResult;
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, listBoxLogCommandCopy_Executed));
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, listBoxLogCommandSelAll_Executed));
            textBoxSql.CommandBindings.Add(new CommandBinding(QueryCommands.NormalizeSQL, textBoxSqlCommandNormalizeSql_Executed));
        }

        private void buttonCopyAll_Click(object sender, RoutedEventArgs e)
        {
            DataGridCommands.CopyTable.Execute(null, dataGridResult);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ParameterStore.AllParameters.ParameterTextChanged -= Parameters_ParameterTextChanged;
        }

        private void menuItemClearLog_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
        }
    }
    public class IsErrorBrushConverter: IValueConverter
    {
        private static Brush RedBrush = new SolidColorBrush(Colors.Red);
        //private static Brush NormalBrush = SystemColors.WindowTextBrush;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (((value is bool) && (bool)value)) ? RedBrush : SystemColors.WindowTextBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
