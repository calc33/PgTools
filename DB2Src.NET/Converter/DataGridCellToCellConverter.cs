using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Db2Source
{
    public class DataGridCellToCellConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridCell cell = value as DataGridCell;
            if (cell == null)
            {
                return null;
            }
            ColumnInfo col = cell.Column.Header as ColumnInfo;
            if (col == null || col.Index == -1)
            {
                return null;
            }
            GridClipboard.Row row = cell.DataContext as GridClipboard.Row;
            if (row == null)
            {
                return ColumnInfo.Stub;
            }
            return row.Cells[cell.Column.DisplayIndex];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
