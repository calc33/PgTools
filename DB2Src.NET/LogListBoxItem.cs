using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class LogListBoxItem: ListBoxItem
    {
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(DateTime), typeof(LogListBoxItem));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(LogStatus), typeof(LogListBoxItem));
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(LogListBoxItem));
        public static readonly DependencyProperty SqlProperty = DependencyProperty.Register("Sql", typeof(string), typeof(LogListBoxItem));
        public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register("Parameters", typeof(ParameterStoreCollection), typeof(LogListBoxItem));
        public static readonly DependencyProperty ErrorPositionProperty = DependencyProperty.Register("ErrorPosition", typeof(Tuple<int,int>), typeof(LogListBoxItem));
        public static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
        private static readonly Dictionary<LogStatus, Brush> LogStatusToBrush = new Dictionary<LogStatus, Brush>()
        {
            { LogStatus.Normal, SystemColors.ControlTextBrush },
            { LogStatus.Error, ErrorBrush },
            { LogStatus.Aux, SystemColors.GrayTextBrush },
        };
        public DateTime Time
        {
            get
            {
                return (DateTime)GetValue(TimeProperty);
            }
            set
            {
                SetValue(TimeProperty, value);
            }
        }
        public LogStatus Status
        {
            get
            {
                return (LogStatus)GetValue(StatusProperty);
            }
            set
            {
                SetValue(StatusProperty, value);
            }
        }
        public string Message
        {
            get
            {
                return (string)GetValue(MessageProperty);
            }
            set
            {
                SetValue(MessageProperty, value);
            }
        }

        public string Sql
        {
            get
            {
                return (string)GetValue(SqlProperty);
            }
            set
            {
                SetValue(SqlProperty, value);
            }
        }
        public ParameterStoreCollection Parameters
        {
            get
            {
                return (ParameterStoreCollection)GetValue(ParametersProperty);
            }
            set
            {
                SetValue(ParametersProperty, value);
            }
        }
        /// <summary>
        /// エラーの発生位置(文字数)+選択するキーワードの文字列長 のタプル
        /// エラー箇所の選択表示が不要な場合はnull
        /// </summary>
        public Tuple<int,int> ErrorPosition
        {
            get
            {
                return (Tuple<int, int>)GetValue(ErrorPositionProperty);
            }
            set
            {
                SetValue(ErrorPositionProperty, value);
            }
        }
        private void TimePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            textBlockTime.Text = Time.ToString("HH:mm:ss.fff ");
        }
        private void StatusPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Brush b;
            if (!LogStatusToBrush.TryGetValue(Status, out b))
            {
                b = LogStatusToBrush[LogStatus.Normal];
            }
            textBlockText.Foreground = b;
        }
        private void TextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            textBlockText.Text = Message.TrimEnd();
        }
        private void SqlPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            buttonRedo.Visibility = string.IsNullOrEmpty(Sql) ? Visibility.Collapsed : Visibility.Visible;
            textBlockText.ToolTip = Sql;
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TimeProperty)
            {
                TimePropertyChanged(e);
            }
            if (e.Property == MessageProperty)
            {
                TextPropertyChanged(e);
            }
            if (e.Property == StatusProperty)
            {
                StatusPropertyChanged(e);
            }
            if (e.Property == SqlProperty)
            {
                SqlPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private TextBlock textBlockTime;
        private Button buttonRedo;
        private TextBlock textBlockText;

        public event EventHandler<EventArgs> RedoSql;

        protected void OnRedoSql(EventArgs e)
        {
            RedoSql?.Invoke(this, e);
        }

        public LogListBoxItem() : base()
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto, SharedSizeGroup = "logListBoxItem1" });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto, SharedSizeGroup = "logListBoxItem2" });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
            Content = grid;
            textBlockTime = new TextBlock();
            textBlockTime.Name = "textBoxTime";
            textBlockTime.VerticalAlignment = VerticalAlignment.Top;
            textBlockTime.Foreground = SystemColors.GrayTextBrush;
            grid.Children.Add(textBlockTime);

            buttonRedo = new Button();
            buttonRedo.Name = "buttonRedo";
            buttonRedo.Content = FindResource("ImageRollback14");
            buttonRedo.ToolTip = "再実行";
            buttonRedo.Click += ButtonRedo_Click;
            buttonRedo.Visibility = Visibility.Collapsed;
            Grid.SetColumn(buttonRedo, 1);
            grid.Children.Add(buttonRedo);

            textBlockText = new TextBlock();
            textBlockText.Name = "textBoxText";
            textBlockText.VerticalAlignment = VerticalAlignment.Top;
            textBlockText.TextWrapping = TextWrapping.Wrap;
            Grid.SetColumn(textBlockText, 2);
            grid.Children.Add(textBlockText);
        }

        private void ButtonRedo_Click(object sender, RoutedEventArgs e)
        {
            OnRedoSql(EventArgs.Empty);
        }

        public override string ToString()
        {
            return textBlockText.Text;
        }
    }

    //public class LogStatusToBrushConverter: IValueConverter
    //{
    //    private static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        switch ((LogStatus)value)
    //        {
    //            case LogStatus.Normal:
    //                return SystemColors.ControlTextBrush;
    //            case LogStatus.Error:
    //                return ErrorBrush;
    //            default:
    //                return SystemColors.ControlTextBrush;
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
