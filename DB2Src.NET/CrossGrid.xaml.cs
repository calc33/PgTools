using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    /// CrossGrid.xaml の相互作用ロジック
    /// </summary>
    public partial class CrossGrid : UserControl
    {
        public static readonly DependencyProperty RowAxisesProperty = DependencyProperty.Register("RowAxises", typeof(AxisCollection), typeof(CrossGrid), new PropertyMetadata(new PropertyChangedCallback(OnRowAxisesPropertyChanged)));
        public static readonly DependencyProperty ColumnAxisesProperty = DependencyProperty.Register("ColumnAxises", typeof(AxisCollection), typeof(CrossGrid), new PropertyMetadata(new PropertyChangedCallback(OnColumnAxisesPropertyChanged)));
        public static readonly DependencyProperty FilterAxisesProperty = DependencyProperty.Register("FilterAxises", typeof(AxisCollection), typeof(CrossGrid), new PropertyMetadata(new PropertyChangedCallback(OnFilterAxisesPropertyChanged)));
        //public static readonly DependencyProperty AnimationAxisesProperty = DependencyProperty.Register("AnimationAxises", typeof(AxisCollection), typeof(CrossGrid));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(CrossTable), typeof(CrossGrid), new PropertyMetadata(new PropertyChangedCallback(OnItemsSourcePropertyChanged)));
        public static readonly DependencyProperty SummaryOrientationProperty = DependencyProperty.Register("SummaryOrientation", typeof(Orientation), typeof(CrossGrid));

        private CrossTable _table;

        private List<AxisEntry> _rowAxisEntries = new List<AxisEntry>();
        private List<AxisEntry> _columnAxisEntries = new List<AxisEntry>();
        private List<AxisEntry> _filterAxisEntries = new List<AxisEntry>();

        public AxisCollection RowAxises
        {
            get { return (AxisCollection)GetValue(RowAxisesProperty); }
            set { SetValue(RowAxisesProperty, value); }
        }

        public AxisCollection ColumnAxises
        {
            get { return (AxisCollection)GetValue(ColumnAxisesProperty); }
            set { SetValue(ColumnAxisesProperty, value); }
        }

        public AxisCollection FilterAxises
        {
            get { return (AxisCollection)GetValue(FilterAxisesProperty); }
            set { SetValue(FilterAxisesProperty, value); }
        }

        public CrossTable ItemsSource
        {
            get { return (CrossTable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public Orientation SummaryOrientation
        {
            get { return (Orientation)GetValue(SummaryOrientationProperty); }
            set { SetValue(SummaryOrientationProperty, value); }
        }

        public CrossGrid()
        {
            InitializeComponent();
            _table = new CrossTable();
            BindingOperations.SetBinding(_table, CrossTable.ItemsSourceProperty, new Binding("ItemsSource") { Source = this });
        }

        private AxisEntry[] GetVisibleEntries(List<AxisEntry> entries)
        {
            List<AxisEntry> l = new List<AxisEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                AxisEntry entry = entries[i];
                l.Add(entry);
                if (!entry.IsFolded)
                {
                    l.AddRange(GetVisibleEntries(entry.Children));
                }
            }
            return l.ToArray();
        }
        private void UpdateAxisEntries()
        {
            AxisCollection[] axises = new AxisCollection[] { RowAxises, ColumnAxises, FilterAxises};
            List<AxisEntry>[] entries = ItemsSource.GetAxisEntries(axises);
            _rowAxisEntries = entries[0];
            _columnAxisEntries = entries[1];
            _filterAxisEntries = entries[2];
        }

        private static void AdjustRowDefinitions(RowDefinitionCollection rowDefinitions, int size, bool named)
        {
            for (int i = rowDefinitions.Count; i < size; i++)
            {
                RowDefinition def = new RowDefinition() { Height = GridLength.Auto };
                if (named)
                {
                    def.SharedSizeGroup = "R" + i.ToString();
                }
                rowDefinitions.Add(def);
            }
        }

        private static void AdjustColumnDefinitions(ColumnDefinitionCollection columnDefinitions, int size, bool named)
        {
            for (int i = columnDefinitions.Count; i < size; i++)
            {
                ColumnDefinition def = new ColumnDefinition() { Width = GridLength.Auto };
                if (named)
                {
                    def.SharedSizeGroup = "C" + i.ToString();
                }
                columnDefinitions.Add(def);
            }
        }

        private void UpdateGridRowHeader(AxisEntry[] entries, int nHeader, int nSumHeader, int nSpan)
        {
            gridRowHeader.Children.Clear();
            Label[] prevLabel = new Label[nHeader];
            for (int i = 0; i < entries.Length; i++)
            {
                Label[] labels = new Label[nHeader];
                AxisEntry entry = entries[i];
                Label label = null;
                for (int j = 0; j < nHeader; j++)
                {
                    switch (entry.Status[j])
                    {
                        case AxisEntryStatus.Visible:
                            if (label != null)
                            {
                                label.SetValue(Grid.ColumnSpanProperty, j - (int)label.GetValue(Grid.ColumnProperty));
                            }
                            label = new Label()
                            {
                                Content = entry.Contents[j],
                                ContentStringFormat = entry.Values[j].Owner.StringFormat,
                                BorderThickness = new Thickness(1.0, 1.0, 0.0, 0.0)
                            };
                            label.SetValue(Grid.RowProperty, i);
                            label.SetValue(Grid.ColumnProperty, j);
                            label.SetValue(Grid.RowSpanProperty, nSpan);
                            gridRowHeader.Children.Add(label);
                            break;
                        case AxisEntryStatus.JoinPriorEntry:
                            if (label != null)
                            {
                                label.SetValue(Grid.ColumnSpanProperty, j - (int)label.GetValue(Grid.ColumnProperty));
                            }
                            if (prevLabel[j].Content == null)
                            {
                                label = prevLabel[j];
                                label.SetValue(Grid.RowSpanProperty, i * nSpan - (int)label.GetValue(Grid.RowProperty));
                            }
                            break;
                        case AxisEntryStatus.JoinPriorLevel:
                            break;
                        case AxisEntryStatus.Unkonwn:
                            throw new NotImplementedException();
                        case AxisEntryStatus.Hidden:
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }
                    labels[j] = label;
                }
                if (label != null)
                {
                    label.SetValue(Grid.ColumnSpanProperty, nHeader - (int)label.GetValue(Grid.ColumnProperty));
                    label.BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0);
                }
                if (nSumHeader != 0)
                {
                    for (int j = 0; j < nSpan; j++)
                    {
                        Label lbl = new Label() { Content = _table.SummaryDefinitions[j].Axis.Title };
                        lbl.SetValue(Grid.RowProperty, i * nSpan + j);
                        lbl.SetValue(Grid.ColumnProperty, nHeader);
                        gridRowHeader.Children.Add(lbl);
                    }
                }
                prevLabel = labels;
            }
        }

        private void UpdateGridColumnHeader(AxisEntry[] entries, int nHeader, int nSumHeader, int nSpan)
        {
            gridColumnHeader.Children.Clear();
            Label[] prevLabel = new Label[nHeader];
            for (int i = 0; i < entries.Length; i++)
            {
                Label[] labels = new Label[nHeader];
                AxisEntry entry = entries[i];
                Label label = null;
                for (int j = 0; j < nHeader; j++)
                {
                    switch (entry.Status[j])
                    {
                        case AxisEntryStatus.Visible:
                            if (label != null)
                            {
                                label.SetValue(Grid.RowSpanProperty, j - (int)label.GetValue(Grid.RowProperty));
                            }
                            label = new Label()
                            {
                                Content = entry.Contents[j],
                                ContentStringFormat = entry.Values[j].Owner.StringFormat,
                                BorderThickness = new Thickness(1.0, 1.0, 0.0, 0.0)
                            };
                            label.SetValue(Grid.ColumnProperty, i);
                            label.SetValue(Grid.RowProperty, j);
                            label.SetValue(Grid.ColumnSpanProperty, nSpan);
                            gridColumnHeader.Children.Add(label);
                            break;
                        case AxisEntryStatus.JoinPriorEntry:
                            if (label != null)
                            {
                                label.SetValue(Grid.RowSpanProperty, j - (int)label.GetValue(Grid.RowProperty));
                            }
                            if (prevLabel[j].Content == null)
                            {
                                label = prevLabel[j];
                                label.SetValue(Grid.ColumnSpanProperty, i * nSpan - (int)label.GetValue(Grid.ColumnProperty));
                            }
                            break;
                        case AxisEntryStatus.JoinPriorLevel:
                            break;
                        case AxisEntryStatus.Unkonwn:
                            throw new NotImplementedException();
                        case AxisEntryStatus.Hidden:
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }
                    labels[j] = label;
                }
                if (label != null)
                {
                    label.SetValue(Grid.RowSpanProperty, nHeader - (int)label.GetValue(Grid.RowProperty));
                    label.BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0);
                }
                if (nSumHeader != 0)
                {
                    for (int j = 0; j < nSpan; j++)
                    {
                        Label lbl = new Label() { Content = _table.SummaryDefinitions[j].Axis.Title };
                        lbl.SetValue(Grid.ColumnProperty, i * nSpan + j);
                        lbl.SetValue(Grid.RowProperty, nHeader);
                        gridColumnHeader.Children.Add(lbl);
                    }
                }
                prevLabel = labels;
            }
        }
        private void UpdateGridBody(AxisEntry[] rows, AxisEntry[] columns, int nRowSpan, int nColumnSpan)
        {
            gridBody.Children.Clear();
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    var cell = _table.Find(rows[r], columns[c]);
                    int p = 0;
                    for (int cs = 0; cs < nColumnSpan; cs++)
                    {
                        for (int rs = 0; rs < nRowSpan; rs++)
                        {
                            var sum = cell.Summaries[p];
                            Label lbl = new Label() { Content = sum.Result, ContentStringFormat = sum.Axis.StringFormat };
                            lbl.SetValue(Grid.ColumnProperty, c * nColumnSpan + cs);
                            lbl.SetValue(Grid.RowProperty, r * nRowSpan + rs);
                            gridBody.Children.Add(lbl);
                            p++;
                        }
                    }
                }
            }
        }

        private void ArrangeControls()
        {
            AxisEntry[] rows = GetVisibleEntries(_rowAxisEntries);
            AxisEntry[] columns = GetVisibleEntries(_columnAxisEntries);
            int nRowHeader = ColumnAxises.Count;
            int nColumnHeader = RowAxises.Count;
            int nRowSumHeader = 0;
            int nColumnSumHeader = 0;
            int nRowSpan = 1;
            int nColumnSpan = 1;
            switch (SummaryOrientation)
            {
                case Orientation.Horizontal:
                    nRowSumHeader = 1;
                    nColumnSpan = Math.Max(1, _table.SummaryDefinitions.Count);
                    break;
                case Orientation.Vertical:
                    nColumnSumHeader = 1;
                    nRowSpan = Math.Max(1, _table.SummaryDefinitions.Count);
                    break;
                default:
                    throw new NotImplementedException(string.Format("SummaryOrientation={0}に対応する処理がありません", SummaryOrientation));
            }
            int nRow = rows.Length * nRowSpan;
            int nColumn = columns.Length * nColumnSpan;
            AdjustRowDefinitions(gridRowHeader.RowDefinitions, nRow, true);
            AdjustColumnDefinitions(gridRowHeader.ColumnDefinitions, nRowHeader + nRowSumHeader, false);
            AdjustRowDefinitions(gridColumnHeader.RowDefinitions, nColumnHeader + nColumnSumHeader, false);
            AdjustColumnDefinitions(gridColumnHeader.ColumnDefinitions, nColumn, true);
            AdjustRowDefinitions(gridBody.RowDefinitions, nRow, true);
            AdjustColumnDefinitions(gridBody.ColumnDefinitions, nColumn, true);
            UpdateGridRowHeader(rows, nColumnHeader, nColumnSumHeader, nRowSpan);
            UpdateGridColumnHeader(columns, nRowHeader, nRowSumHeader, nColumnSpan);
            UpdateGridBody(rows, columns, nRowSpan, nColumnSpan);
        }

        private bool _isUpdateAxisSourcePosted = false;
        private void UpdateAxisSource()
        {
            try
            {
                if (ItemsSource == null)
                {
                    return;
                }
                ItemsSource.Axises.Clear();
                foreach (Axis axis in RowAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
                foreach (Axis axis in ColumnAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
                foreach (Axis axis in FilterAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
                UpdateAxisEntries();
                Dispatcher.InvokeAsync(ArrangeControls);
            }
            finally
            {
                _isUpdateAxisSourcePosted = false;
            }
        }

        private void DelayedUpdateAxisSource()
        {
            if (_isUpdateAxisSourcePosted)
            {
                return;
            }
            _isUpdateAxisSourcePosted = true;
            Dispatcher.InvokeAsync(UpdateAxisSource, DispatcherPriority.Normal);
        }

        private void OnItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateAxisSource();
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CrossGrid)?.OnItemsSourcePropertyChanged(e);
        }

        private void OnRowAxisesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateAxisSource();
        }

        private static void OnRowAxisesPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CrossGrid)?.OnRowAxisesPropertyChanged(e);
        }

        private void OnColumnAxisesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateAxisSource();
        }

        private static void OnColumnAxisesPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CrossGrid)?.OnColumnAxisesPropertyChanged(e);
        }

        private void OnFilterAxisesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateAxisSource();
        }

        private static void OnFilterAxisesPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CrossGrid)?.OnFilterAxisesPropertyChanged(e);
        }

        private void scrollViewerBody_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            scrollViewerRowHeader.ScrollToVerticalOffset(e.VerticalOffset);
            scrollViewerColumnHeader.ScrollToHorizontalOffset(e.HorizontalOffset);
        }
    }
}
