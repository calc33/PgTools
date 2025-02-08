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
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(DateTime), typeof(LogListBoxItem), new PropertyMetadata(new PropertyChangedCallback(OnTimePropertyChanged)));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(LogStatus), typeof(LogListBoxItem), new PropertyMetadata(new PropertyChangedCallback(OnStatusPropertyChanged)));
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(LogListBoxItem), new PropertyMetadata(new PropertyChangedCallback(OnMessagePropertyChanged)));
        public static readonly DependencyProperty QueryProperty = DependencyProperty.Register("Query", typeof(QueryHistory.Query), typeof(LogListBoxItem), new PropertyMetadata(new PropertyChangedCallback(OnQueryPropertyChanged)));
        public static readonly DependencyProperty ErrorPositionProperty = DependencyProperty.Register("ErrorPosition", typeof(Tuple<int,int>), typeof(LogListBoxItem));
        public static readonly DependencyProperty IsQueryEditableProperty = DependencyProperty.Register("IsQueryEditable", typeof(bool), typeof(LogListBoxItem));
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

        public QueryHistory.Query Query
        {
            get
            {
                return (QueryHistory.Query)GetValue(QueryProperty);
            }
            set
            {
                SetValue(QueryProperty, value);
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

        public bool IsQueryEditable
        {
            get { return (bool)GetValue(IsQueryEditableProperty); }
            set { SetValue(IsQueryEditableProperty, value); }
        }

        private void OnTimePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            textBlockTime.Text = Time.ToString("HH:mm:ss.fff ");
        }

        private static void OnTimePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as LogListBoxItem)?.OnTimePropertyChanged(e);
        }

        private void OnStatusPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
                Brush b;
            if (!LogStatusToBrush.TryGetValue(Status, out b))
            {
                b = LogStatusToBrush[LogStatus.Normal];
            }
            textBlockText.Foreground = b;
        }

        private static void OnStatusPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as LogListBoxItem)?.OnStatusPropertyChanged(e);
        }

        private void OnMessagePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            textBlockText.Text = Message.TrimEnd();
        }

        private static void OnMessagePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as LogListBoxItem)?.OnMessagePropertyChanged(e);
        }

        private void OnQueryPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            buttonRedo.Visibility = (Query == null) ? Visibility.Collapsed : Visibility.Visible;
            ToolTip = Query?.SqlText;
        }

        private static void OnQueryPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as LogListBoxItem)?.OnQueryPropertyChanged(e);
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
            buttonRedo.ContentTemplate = FindResource("ImageRollback14") as DataTemplate;
            buttonRedo.ToolTip = Properties.Resources.ButtonRedo_Tooltip;
            buttonRedo.Click += ButtonRedo_Click;
            buttonRedo.Visibility = Visibility.Collapsed;
            buttonRedo.SetBinding(IsEnabledProperty, new Binding("IsQueryEditable") { Source = this });
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
}
