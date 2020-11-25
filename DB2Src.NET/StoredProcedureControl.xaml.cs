using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
    /// StoredProcedureControl.xaml の相互作用ロジック
    /// </summary>
    public partial class StoredProcedureControl: UserControl, ISchemaObjectControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(StoredFunction), typeof(StoredProcedureControl));
        public static readonly DependencyProperty IsTargetModifiedProperty = DependencyProperty.Register("IsTargetModified", typeof(bool), typeof(StoredProcedureControl));
        public static readonly DependencyProperty DataGridControllerResultProperty = DependencyProperty.Register("DataGridControllerResult", typeof(DataGridController), typeof(StoredProcedureControl));
        public static readonly DependencyProperty DataGridResultMaxHeightProperty = DependencyProperty.Register("DataGridResultMaxHeight", typeof(double), typeof(StoredProcedureControl));

        public StoredFunction Target
        {
            get
            {
                return (StoredFunction)GetValue(TargetProperty);
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
        private void AddLog(string text, string sql, LogStatus status, bool notice)
        {
            LogListBoxItem item = new LogListBoxItem();
            item.Time = DateTime.Now;
            item.Status = status;
            item.Message = text;
            item.Sql = sql;
            item.RedoSql += Item_RedoSql;
            listBoxLog.Items.Add(item);
            listBoxLog.SelectedItem = item;
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
            //textBoxSql.Text = item.Sql;
            //Fetch();
        }

        public StoredProcedureControl()
        {
            InitializeComponent();
        }

        private void UpdateIsTargetModified()
        {
            IsTargetModified = Target.IsModified();
        }
        private void TargetChanged(DependencyPropertyChangedEventArgs e)
        {
            //dataGridColumns.ItemsSource = Target.Columns;
            Target.PropertyChanged += Target_PropertyChanged;
            Target.ParameterChantged += Target_ColumnPropertyChanged;
            Target.CommentChanged += Target_CommentChanged;
            UpdateDataGridParameters();
            //dataGridColumns.ItemsSource = new List<Column>(Target.Columns);
            //dataGridColumns.ItemsSource = Target.Parameters;
            UpdateTabItemExecuteVisiblity();
            UpdateTextBoxSource();
            UpdateIsTargetModified();
            //Dispatcher.Invoke(Execute, DispatcherPriority.Normal);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private void UpdateDataGridParameters()
        {
            if (!Target.Context.AllowOutputParameter)
            {
                dataGridParameterValue.Header = "値";
                dataGridParameterNewValue.Visibility = Visibility.Collapsed;
            }
            List<ParamEditor> list = new List<ParamEditor>();
            IDbCommand cmd = Target.DbCommand;
            foreach (Parameter p in Target.Parameters)
            {
                if (p.DbParameter == null)
                {
                    continue;
                }
                ParamEditor ed = new ParamEditor(p);
                ed.RevertValue();
                list.Add(ed);
            }
            dataGridParameters.ItemsSource = list;
        }
        private void UpdateDataGridResult(IDbCommand command)
        {
            DateTime start = DateTime.Now;
            try
            {
                IDbTransaction txn = command.Connection.BeginTransaction();
                try
                {
                    command.Transaction = txn;
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        IEnumerable l = dataGridParameters.ItemsSource;
                        dataGridParameters.ItemsSource = null;
                        dataGridParameters.ItemsSource = l;
                        DataGridControllerResult.Load(reader);
                        if (0 <= reader.RecordsAffected)
                        {
                            AddLog(string.Format("{0}行反映しました。", reader.RecordsAffected), null, LogStatus.Normal, true);
                        }
                        else
                        {
                            tabControlResult.SelectedItem = tabItemDataGrid;
                        }
                    }
                    txn.Commit();
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
                finally
                {
                    command.Transaction = null;
                    txn.Dispose();
                }
            }
            catch (Exception t)
            {
                Db2SourceContext ctx = Target.Context;
                string msg = ctx.GetExceptionMessage(t);
                AddLog(msg, command.CommandText, LogStatus.Error, true);
                //MessageBox.Show(msg, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                DateTime end = DateTime.Now;
                TimeSpan time = end - start;
                string s = string.Format("{0}:{1:00}:{2:00}.{3:000}", (int)time.TotalHours, time.Minutes, time.Seconds, time.Milliseconds);
                AddLog(string.Format("実行しました (所要時間 {0})", s), command.CommandText, LogStatus.Aux, false);
                textBlockGridResult.Text = string.Format("{0}件見つかりました。  所要時間 {1}", DataGridControllerResult.Rows.Count, s);
            }
        }

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }
        private void UpdateTabItemExecuteVisiblity()
        {
            if (Target.DataType == "trigger")
            {
                tabItemExecute.Visibility = Visibility.Collapsed;
            }
            else
            {
                tabItemExecute.Visibility = Visibility.Visible;
            }
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
                    buf.AppendLine();
                }
                textBoxSource.Text = buf.ToString();
            }
            catch (Exception t)
            {
                textBoxSource.Text = t.ToString();
            }
        }

        public void Execute()
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
            try
            {
                foreach (ParamEditor p in dataGridParameters.ItemsSource)
                {
                    p.SetValue();
                }
                using (IDbConnection conn = ctx.NewConnection(true))
                {
                    IDbCommand cmd = Target.DbCommand;
                    try
                    {
                        cmd.Connection = conn;
                        UpdateDataGridResult(cmd);
                    }
                    catch (Exception t)
                    {
                        ctx.OnLog(ctx.GetExceptionMessage(t), LogStatus.Error, Target.FullName);
                    }
                    finally
                    {
                        cmd.Connection = null;
                    }
                }
            }
            catch (Exception t)
            {
                MessageBox.Show(ctx.GetExceptionMessage(t), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            Execute();
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
        private void Target_ColumnPropertyChanged(object sender, CollectionOperationEventArgs<Parameter> e)
        {
            UpdateIsTargetModified();
        }
        private void Target_CommentChanged(object sender, CommentChangedEventArgs e)
        {
            UpdateIsTargetModified();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            DataGridControllerResult = new DataGridController();
            DataGridControllerResult.Grid = dataGridResult;
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {

        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void OnTabClosed(object sender) { }

        private void menuItemClearLog_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
        }
    }
    public class ParamEditor: DependencyObject
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(ParamEditor));
        public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register("IsError", typeof(bool), typeof(ParamEditor));
        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", typeof(string), typeof(ParamEditor));
        public static readonly DependencyProperty IsNullProperty = DependencyProperty.Register("IsNull", typeof(bool), typeof(ParamEditor));
        public static readonly DependencyProperty NewValueProperty = DependencyProperty.Register("NewValue", typeof(string), typeof(ParamEditor));
        private Parameter _parameter;
        private IDbDataParameter _dbParameter;
        public Parameter Parameter
        {
            get
            {
                return _parameter;
            }
            set
            {
                _parameter = value;
                if (_parameter == null)
                {
                    return;
                }
                StringFormat = _parameter.StringFormat;
                if (_parameter.DbParameter != null)
                {
                    DbParameter = _parameter.DbParameter;
                }
            }
        }
        public Type ValueType { get; set; }
        public IDbDataParameter DbParameter
        {
            get
            {
                return _dbParameter;
            }
            set
            {
                if (_dbParameter == value)
                {
                    return;
                }
                _dbParameter = value;
            }
        }
        private string GetStrValue()
        {
            if (DbParameter == null)
            {
                return null;
            }
            if (DbParameter.Value == null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(StringFormat))
            {
                string fmt = "{0:" + StringFormat + "}";
                return string.Format(fmt, DbParameter.Value);
            }
            else
            {
                return DbParameter.Value.ToString();
            }
        }
        public void RevertValue()
        {
            if (DbParameter == null)
            {
                return;
            }
            IsNull = ((DbParameter.Value == null) || (DbParameter.Value is DBNull));
            Value = GetStrValue();
        }
        public void SetValue()
        {
            if (DbParameter == null)
            {
                return;
            }
            if (IsNull)
            {
                DbParameter.Value = DBNull.Value;
            }
            if (ValueType == typeof(string) || ValueType.IsSubclassOf(typeof(string)))
            {
                DbParameter.Value = string.IsNullOrEmpty(Value) ? (object)DBNull.Value : Value;
                return;
            }
            if (string.IsNullOrEmpty(Value))
            {
                DbParameter.Value = DBNull.Value;
                return;
            }
            MethodInfo mi = null;
            if (!string.IsNullOrEmpty(StringFormat))
            {
                mi = ValueType.GetMethod("ParseExact", new Type[] { typeof(string), typeof(string) });
                if (mi != null)
                {
                    DbParameter.Value = mi.Invoke(null, new object[] { Value, StringFormat });
                    return;
                }
            }
            mi = ValueType.GetMethod("Parse", new Type[] { typeof(string) });
            if (mi == null)
            {
                throw new NotSupportedException();
            }
            DbParameter.Value = mi.Invoke(null, new object[] { Value });
        }
        public void RevertNewValue()
        {
            NewValue = GetStrValue();
        }
        public string ParameterName
        {
            get
            {
                return DbParameter?.ParameterName;
            }
        }

        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            private set { SetValue(StringFormatProperty, value); }
        }
        public bool IsNull
        {
            get { return (bool)GetValue(IsNullProperty); }
            set { SetValue(IsNullProperty, value); }
        }
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string NewValue
        {
            get { return (string)GetValue(NewValueProperty); }
            set { SetValue(NewValueProperty, value); }
        }

        private void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Value) && IsNull)
            {
                IsNull = false;
            }
        }

        private void IsNullPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (IsNull && !string.IsNullOrEmpty(Value))
            {
                Value = null;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ValueProperty)
            {
                ValuePropertyChanged(e);
            }
            if (e.Property == IsNullProperty)
            {
                IsNullPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }
        //public ParamEditor() { }
        public ParamEditor(Parameter param)
        {
            Parameter = param;
            ValueType = param.ValueType;
        }
    }
}
