using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// RecordViewerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RecordViewerWindow : Window
    {
        public static readonly DependencyProperty TableProperty = DependencyProperty.Register("Table", typeof(Table), typeof(RecordViewerWindow));
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register("Column", typeof(ColumnInfo), typeof(RecordViewerWindow));
        public static readonly DependencyProperty RowProperty = DependencyProperty.Register("Row", typeof(Row), typeof(RecordViewerWindow));
        public RecordViewerWindow()
        {
            InitializeComponent();
            new CloseOnDeactiveWindowHelper(this, true);
        }

        public Table Table
        {
            get
            {
                return (Table)GetValue(TableProperty);
            }
            set
            {
                SetValue(TableProperty, value);
            }
        }

        public ColumnInfo Column
        {
            get
            {
                return (ColumnInfo)GetValue(ColumnProperty);
            }
            set
            {
                SetValue(ColumnProperty, value);
            }
        }

        public Row Row
        {
            get
            {
                return (Row)GetValue(RowProperty);
            }
            set
            {
                SetValue(RowProperty, value);
            }
        }

        private void AddReference(List<ColumnValue> list, ForeignKeyConstraint constraint)
        {
            Db2SourceContext dataSet = Table.Context;
            Table refTbl = dataSet.Tables[constraint.ReferenceSchemaName, constraint.ReferenceTableName];
            if (refTbl == null)
            {
                return;
            }
            ColumnInfo[] cols = Row.GetForeignKeyColumns(constraint);
            //List<IDbDataParameter> prms = new List<IDbDataParameter>(cols.Length);
            StringBuilder buf = new StringBuilder();
            string join = string.Empty;
            for (int i = 0, n = cols.Length; i < n; i++)
            {
                ColumnInfo col = cols[i];
                buf.Append(join);
                buf.Append(constraint.RefColumns[i]);
                buf.Append(" = :");
                buf.Append(col.Name);
                buf.Append("::");
                buf.Append(Table.Columns[col.Name].DataType);
                //prms.Add(dataSet.CreateParameterByFieldInfo(col, Row[col.Index], false));
                join = " and ";
            }
            using (IDbConnection conn = dataSet.NewConnection(true))
            {
                using (IDbCommand cmd = dataSet.GetSqlCommand(refTbl.GetSelectSQL(string.Empty, buf.ToString(), string.Empty, null, HiddenLevel.Visible), null, conn))
                {
                    foreach (ColumnInfo col in cols)
                    {
                        dataSet.ApplyParameterByFieldInfo(cmd.Parameters, col, Row[col.Index], false);
                    }
                    IDataReader reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        return;
                    }
                    list.Add(new ColumnValue() { IsHeader = true, ColumnName = refTbl.Name, Value = null });
                    for (int i = 0, n = reader.FieldCount; i < n; i++)
                    {
                        object v = null;
                        try
                        {
                            v = reader.GetValue(i);
                        }
                        catch (OverflowException)
                        {
                            v = "(OVERFLOW)";
                        }
                        list.Add(new ColumnValue() { IsHeader = false, ColumnName = reader.GetName(i), Value = v });
                    }
                }
            }
        }
        private void UpdateGridInternal()
        {
            if (Table == null || Column == null || Row == null)
            {
                dataGridColumnValues.ItemsSource = null;
                return;
            }
            if (Column.Index == -1)
            {
                dataGridColumnValues.ItemsSource = null;
                return;
            }
            object v = Row[Column.Index];
            if (v == null)
            {
                dataGridColumnValues.ItemsSource = null;
                return;
            }
            List<ColumnValue> l = new List<ColumnValue>();

            foreach (Constraint c in Table.Constraints)
            {
                ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                if (fc == null)
                {
                    continue;
                }
                foreach (string col in fc.Columns)
                {
                    if (string.Compare(col, Column.Name, true) == 0)
                    {
                        AddReference(l, fc);
                    }
                }
            }
            dataGridColumnValues.ItemsSource = l;
        }

        private object UpdateGridLock = new object();
        private bool _isUpdateGridPosted = false;

        private void UpdateGrid()
        {
            lock (UpdateGridLock)
            {
                try
                {
                    UpdateGridInternal();
                }
                finally
                {
                    _isUpdateGridPosted = false;
                }
            }
        }
        private void DelayedUpdateGrid()
        {
            if (_isUpdateGridPosted)
            {
                return;
            }
            lock (UpdateGridLock)
            {
                if (_isUpdateGridPosted)
                {
                    return;
                }
                _isUpdateGridPosted = true;
                Dispatcher.InvokeAsync(UpdateGrid);
            }
        }

        private void OnTablePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateGrid();
        }
        private void OnColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateGrid();
        }
        private void OnRowPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateGrid();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == TableProperty)
            {
                OnTablePropertyChanged(e);
            }
            if (e.Property == ColumnProperty)
            {
                OnColumnPropertyChanged(e);
            }
            if (e.Property == RowProperty)
            {
                OnRowPropertyChanged(e);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //dataGridTextColumnValue.ElementStyle = Resources["ValueTextBlockStyle"] as Style;
            //dataGridTextColumnValue.EditingElementStyle = Resources["ValueTextBoxStyle"] as Style;
            dataGridTextColumnColumnName.CellStyle = Resources["DataGridCellColumnNameStyle"] as Style;
            dataGridTextColumnValue.CellStyle = Resources["DataGridCellValueStyle"] as Style;
        }
    }

    public class ColumnValue
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }
        public bool IsHeader { get; set; }
    }

    public class SpannedCellPanel: DataGridCellsPanel
    {
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            ColumnValue ctx = DataContext as ColumnValue;
            if (ctx == null || !ctx.IsHeader)
            {
                foreach (UIElement elem in InternalChildren)
                {
                    elem.Visibility = Visibility.Visible;
                }
                return base.ArrangeOverride(arrangeSize);
            }
            Size s = base.ArrangeOverride(arrangeSize);
            if (InternalChildren.Count == 0)
            {
                return s;
            }
            InternalChildren[0].Arrange(new Rect(0, 0, s.Width, s.Height));
            for (int i = 1, n = InternalChildren.Count; i < n; i++)
            {
                UIElement elem = InternalChildren[i];
                elem.Arrange(new Rect(s.Width, 0, 0, 0));
                elem.Visibility = Visibility.Collapsed;
            }
            return s;
        }
        protected override Size MeasureOverride(Size constraint)
        {
            ColumnValue ctx = DataContext as ColumnValue;
            if (ctx == null || !ctx.IsHeader)
            {
                return base.MeasureOverride(constraint);
            }
            if (InternalChildren.Count == 0)
            {
                return base.MeasureOverride(constraint);
            }
            UIElement elem = InternalChildren[0];
            elem.Measure(constraint);
            return elem.DesiredSize;
        }
    }

    public class TypeToHorizontalAlignementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return HorizontalAlignment.Left;
            }
            //Type t = value.GetType();
            object v = value;
            if (v is ColumnValue)
            {
                v = ((ColumnValue)v).Value;
            }
            if (v is sbyte || v is byte || v is short || v is ushort
                || v is int || v is uint || v is long || v is ulong
                || v is float || v is double || v is decimal)
            {
                return HorizontalAlignment.Right;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeaderBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush _headerColor = new SolidColorBrush(Color.FromRgb(240, 255, 240));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ColumnValue obj = value as ColumnValue;
            if (obj == null)
            {
                return SystemColors.WindowBrush;
            }
            return obj.IsHeader ? _headerColor : SystemColors.WindowBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeaderFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ColumnValue obj = value as ColumnValue;
            if (obj == null)
            {
                return FontWeights.Normal;
            }
            return obj.IsHeader ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
