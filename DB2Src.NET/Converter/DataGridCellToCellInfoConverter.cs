using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Db2Source
{
    public class DataGridCellToCellInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridCell cell = value as DataGridCell;
            if (cell == null)
            {
                return null;
            }
            CellInfo info = DataGridController.GetCellInfo(cell);
            if (info == null)
            {
                info = new CellInfo()
                {
                    Cell = cell
                };
                DataGridController.SetCellInfo(cell, info);
            }
            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
