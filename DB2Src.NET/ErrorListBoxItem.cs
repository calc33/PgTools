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
    public class ErrorListBoxItem: ListBoxItem
    {
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(ErrorListBoxItem));
        public static readonly DependencyProperty ErrorPositionProperty = DependencyProperty.Register("ErrorPosition", typeof(Tuple<int, int>), typeof(ErrorListBoxItem));
        public static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
        private static readonly Dictionary<LogStatus, Brush> LogStatusToBrush = new Dictionary<LogStatus, Brush>()
        {
            { LogStatus.Normal, SystemColors.ControlTextBrush },
            { LogStatus.Error, ErrorBrush },
            { LogStatus.Aux, SystemColors.GrayTextBrush },
        };
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

        /// <summary>
        /// エラーの発生位置(文字数)+選択するキーワードの文字列長 のタプル
        /// エラー箇所の選択表示が不要な場合はnull
        /// </summary>
        public Tuple<int, int> ErrorPosition
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
        private void TextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            textBlockText.Text = Message.TrimEnd();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == MessageProperty)
            {
                TextPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private TextBlock textBlockText;

        public event EventHandler<EventArgs> RedoSql;

        protected void OnRedoSql(EventArgs e)
        {
            RedoSql?.Invoke(this, e);
        }

        public ErrorListBoxItem() : base()
        {
            Grid grid = new Grid();
            Content = grid;
            textBlockText = new TextBlock();
            textBlockText.Name = "textBoxText";
            textBlockText.VerticalAlignment = VerticalAlignment.Top;
            textBlockText.TextWrapping = TextWrapping.Wrap;
            textBlockText.Foreground = Brushes.Red;
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
