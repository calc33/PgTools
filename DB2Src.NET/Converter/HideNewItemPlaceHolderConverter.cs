using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class HideNewItemPlaceHolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null || value.GetType() == CollectionView.NewItemPlaceholder.GetType()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
