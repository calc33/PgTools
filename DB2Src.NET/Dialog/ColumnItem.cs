using System;
using System.Windows;

namespace Db2Source
{
    public class ColumnItem : DependencyObject
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(ColumnItem));
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register("Column", typeof(Column), typeof(ColumnItem));

        public bool IsChecked
        {
            get
            {
                return (bool)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
            }
        }

        public Column Column
        {
            get
            {
                return (Column)GetValue(ColumnProperty);
            }
            set
            {
                SetValue(ColumnProperty, value);
            }
        }

        public override string ToString()
        {
            return Column?.Name;
        }
    }
}
