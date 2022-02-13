using System;
using System.Collections.Generic;
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
    /// HistoryWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class HistoryWindow : Window
    {
        public static readonly DependencyProperty SpanProperty = DependencyProperty.Register("Span", typeof(int), typeof(HistoryWindow));
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(QueryHistory.Query), typeof(HistoryWindow));
        //public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(Db2SourceContext), typeof(HistoryWindow));

        //public Db2SourceContext DataSet
        //{
        //    get
        //    {
        //        return (Db2SourceContext)GetValue(DataSetProperty);
        //    }
        //    set
        //    {
        //        SetValue(DataSetProperty, value);
        //    }
        //}

        public int Span
        {
            get
            {
                return (int)GetValue(SpanProperty);
            }
            set
            {
                SetValue(SpanProperty, value);
            }
        }

        public QueryHistory.Query Selected
        {
            get
            {
                return (QueryHistory.Query)GetValue(SelectedProperty);
            }
            set
            {
                SetValue(SelectedProperty, value);
            }
        }

        private bool _isFetched = false;


        public HistoryWindow()
        {
            InitializeComponent();
        }

        private static Tuple<DateTime?, DateTime?> GetDateTuple(int value, DateUnit unit)
        {
            DateTime dt = DateTime.Now;
            switch (unit)
            {
                case DateUnit.Minute:
                    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
                    return new Tuple<DateTime?, DateTime?>(dt.AddMinutes(-value), null);
                case DateUnit.Hour:
                    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
                    return new Tuple<DateTime?, DateTime?>(dt.AddHours(-value), null);
                case DateUnit.Day:
                    return new Tuple<DateTime?, DateTime?>(DateTime.Today.AddDays(-value), null);
                default:
                    throw new NotImplementedException();
            }
        }

        private Tuple<DateTime?, DateTime?> GetDateRange()
        {
            DateRangeKindItem sel = (DateRangeKindItem)comboBoxDateKind.SelectedItem;
            if (sel == null)
            {
                return new Tuple<DateTime?, DateTime?>(null, null);
            }
            if (sel.UseSpan)
            {
                ComboBoxItem selU = comboBoxDateUnit.SelectedItem as ComboBoxItem;
                DateUnit u = (selU != null) ? (DateUnit)selU.Tag : DateUnit.Day;
                return GetDateTuple(Span, u);
            }
            if (sel.UseRange)
            {
                DateTime? dt1 = datePickerBegin.SelectedDate;
                DateTime? dt2 = datePickerEnd.SelectedDate;
                if (dt2.HasValue)
                {
                    dt2 = dt2.Value.AddDays(1);
                }
                return new Tuple<DateTime?, DateTime?>(dt1, dt2);
            }
            if (sel.Value.HasValue)
            {
                return GetDateTuple(sel.Value.Value, sel.Unit);
            }
            return new Tuple<DateTime?, DateTime?>(null, null);
        }

        private void UpdateListBoxResult()
        {
            if (!_isFetched)
            {
                return;
            }

            List<QueryHistory.Query> l = new List<QueryHistory.Query>();
            string s = textBoxFilter.Text.Trim();
            if (string.IsNullOrEmpty(s))
            {
                s = null;
            }
            foreach (QueryHistory.Query q in App.CurrentDataSet.History)
            {
                if (s == null || q.SqlText.Contains(s))
                {
                    l.Add(q);
                }
            }
            listBoxResult.ItemsSource = l;
        }

        private void Fetch()
        {
            Tuple<DateTime?, DateTime?> range = GetDateRange();
            App.CurrentDataSet.History.Fill(range.Item1, range.Item2);
            _isFetched = true;
            UpdateListBoxResult();
        }

        private void buttonFetch_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            Span = 2;
            Dispatcher.InvokeAsync(Fetch, DispatcherPriority.ApplicationIdle);
        }

        private void listBoxResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            buttonOK_Click(sender, e);
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            QueryHistory.Query sel = Selected;
            if (sel == null)
            {
                return;
            }
            DialogResult = true;
            Close();
        }
    }

    public enum DateUnit
    {
        Minute,
        Hour,
        Day
    }
    public class DateRangeKindItem
    {
        public string Text { get; set; }
        public bool UseSpan { get; set; }
        public bool UseRange { get; set; }
        public int? Value { get; set; }
        public DateUnit Unit { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    public class TimestampTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
            {
                return value;
            }
            DateTime dt0 = DateTime.Now;
            DateTime dt = (DateTime)value;
            TimeSpan t = dt0 - dt;
            if (t.TotalSeconds < 60.0)
            {
                return string.Format("{0}秒前", (int)t.TotalSeconds);
            }
            if (t.TotalMinutes < 60.0)
            {
                return string.Format("{0}分前", (int)t.TotalMinutes);
            }
            if (t.TotalHours < 24.0)
            {
                return string.Format("{0}時間{1}分前", (int)t.TotalHours, t.Minutes);
            }
            if (t.TotalDays < 10.0)
            {
                return string.Format("{0}日前", (int)t.TotalDays);
            }
            if (dt0.Year == dt.Year)
            {
                return string.Format("m/d", dt);
            }
            return string.Format("yyyy/m/d", dt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
