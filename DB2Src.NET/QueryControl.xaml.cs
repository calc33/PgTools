using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        private void AddLog(string text, string sql, ParameterStoreCollection parameters, LogStatus status, bool notice)
        {
            LogListBoxItem item = new LogListBoxItem();
            item.Time = DateTime.Now;
            item.Status = status;
            item.Message = text;
            item.Sql = sql;
            item.Parameters = parameters;
            item.RedoSql += Item_RedoSql;
            listBoxLog.Items.Add(item);
            listBoxLog.SelectedItem = item;
            listBoxLog.ScrollIntoView(item);
            if (notice)
            {
                tabControlResult.SelectedItem = tabItemLog;
            }
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

        private void UpdateDataGridResult(SQLParts sqls)
        {
            Db2SourceContext ctx = CurrentDataSet;
            using (IDbConnection conn = ctx.Connection())
            {
                bool modified;
                Parameters = ParameterStore.GetParameterStores(sqls.ParameterNames, Parameters, out modified);
                if (modified)
                {
                    return;
                }

                foreach (SQLPart sql in sqls.Items)
                {
                    IDbCommand cmd = ctx.GetSqlCommand(sql.SQL, conn);
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
                            else
                            {
                                tabControlResult.SelectedItem = tabItemDataGrid;
                            }
                        }
                    }
                    catch (Exception t)
                    {
                        AddLog(t.Message, sql.SQL, stores, LogStatus.Error, true);
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
            UpdateDataGridResult(parts);
        }
        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataGridControllerResult = new DataGridController();
            //DataGridControllerResult.Context = CurrentDataSet;
            DataGridControllerResult.Grid = dataGridResult;
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, listBoxLogCommandCopy_Executed));
            listBoxLog.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, listBoxLogCommandSelAll_Executed));
        }

        private void buttonCopyAll_Click(object sender, RoutedEventArgs e)
        {
            DataGridCommands.CopyTable.Execute(null, dataGridResult);
        }
    }
}
