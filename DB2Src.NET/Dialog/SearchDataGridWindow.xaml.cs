using System;
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
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace Db2Source
{
    /// <summary>
    /// SearchDataGridWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchDataGridWindow: Window
    {
        internal class Evaluator : FrameworkElement
        {
            public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Evaluator));

            public string Text
            {
                get
                {
                    return (string)GetValue(TextProperty);
                }
                set
                {
                    SetValue(TextProperty, value);
                }
            }

            public static string Eval(BindingBase binding, object target)
            {
                Evaluator obj = new Evaluator();
                obj.DataContext = target;
                BindingOperations.SetBinding(obj, TextProperty, binding);
                return obj.Text;
            }
        }
        public class CellInfo
        {
            public struct CellPoint
            {
                public int ItemIndex;
                public int DisplayColumnIndex;
                public void Apply(CellInfo info)
                {
                    info.ItemIndex = ItemIndex;
                    info.DisplayColumnIndex = DisplayColumnIndex;
                }
                internal CellPoint(CellInfo info)
                {
                    ItemIndex = info.ItemIndex;
                    DisplayColumnIndex = info.DisplayColumnIndex;
                }
                internal CellPoint(int itemIndex, int displayColumnIndex)
                {
                    ItemIndex = itemIndex;
                    DisplayColumnIndex = displayColumnIndex;
                }
                internal CellPoint(CellPoint point)
                {
                    ItemIndex = point.ItemIndex;
                    DisplayColumnIndex = point.DisplayColumnIndex;
                }
                public override bool Equals(object obj)
                {
                    if (obj is CellPoint)
                    {
                        return ItemIndex == ((CellPoint)obj).ItemIndex && DisplayColumnIndex == ((CellPoint)obj).DisplayColumnIndex;
                    }
                    if (obj is CellInfo)
                    {
                        return ItemIndex == ((CellInfo)obj).ItemIndex && DisplayColumnIndex == ((CellInfo)obj).DisplayColumnIndex;
                    }
                    return base.Equals(obj);
                }
                public override int GetHashCode()
                {
                    return ItemIndex * 17 + DisplayColumnIndex;
                }
                public override string ToString()
                {
                    return string.Format("({0}, {1})", ItemIndex, DisplayColumnIndex);
                }
            }
            public CellPoint _terminal;
            public SearchDataGridWindow Owner { get; private set; }
            private List<DataGridColumn> DisplayColumns
            {
                get
                {
                    return Owner._displayColumns;
                }
            }
            public DataGrid Grid
            {
                get
                {
                    return Owner.Target;
                }
            }
            public object Item
            {
                get
                {
                    return Grid.Items[ItemIndex];
                }
                private set
                {
                    ItemIndex = Grid.Items.IndexOf(value);
                }
            }
            private int _itemIndex;
            public int ItemIndex
            {
                get { return _itemIndex; }
                set { _itemIndex = value; }
            }
            public DataGridColumn Column
            {
                get
                {
                    return DisplayColumnIndex != -1 ? DisplayColumns[DisplayColumnIndex] : null;
                }
                set
                {
                    DisplayColumnIndex = DisplayColumns.IndexOf(value);
                }
            }
            private int _displayColumnIndex;
            public int DisplayColumnIndex
            {
                get { return _displayColumnIndex; }
                set { _displayColumnIndex = value; }
            }
            public string GetText()
            {
                DataGridTextColumn column = Column as DataGridTextColumn;
                if (column == null)
                {
                    return null;
                }
                object item = Item;
                if (item == null || item == CollectionView.NewItemPlaceholder)
                {
                    return null;
                }
                DataGridCell cell = new DataGridCell();
                cell.DataContext = item;
                BindingBase b = column.Binding;
                return Evaluator.Eval(column.Binding, item);
            }

            public bool IsValid()
            {
                return 0 <= ItemIndex && ItemIndex < Grid.Items.Count && Grid.Items[ItemIndex] != CollectionView.NewItemPlaceholder
                    && 0 <= DisplayColumnIndex && DisplayColumnIndex < DisplayColumns.Count;
            }

            public bool IsValid(CellPoint point)
            {
                return 0 <= point.ItemIndex && point.ItemIndex < Grid.Items.Count && Grid.Items[point.ItemIndex] != CollectionView.NewItemPlaceholder
                    && 0 <= point.DisplayColumnIndex && point.DisplayColumnIndex < DisplayColumns.Count;
            }

            private CellPoint GetNextPoint(CellPoint point)
            {
                if (!IsValid(point))
                {
                    return new CellPoint(0, 0);
                }
                CellPoint p = new CellPoint(point);
                p.DisplayColumnIndex++;
                if (0 <= p.DisplayColumnIndex && p.DisplayColumnIndex < DisplayColumns.Count)
                {
                    return p;
                }
                p.DisplayColumnIndex = 0;
                p.ItemIndex++;
                if (0 <= p.ItemIndex && p.ItemIndex < Grid.Items.Count && Grid.Items[p.ItemIndex] != CollectionView.NewItemPlaceholder)
                {
                    return p;
                }
                p.ItemIndex = 0;
                return p;
            }
            private CellPoint GetNextPoint()
            {
                return GetNextPoint(new CellPoint(this));
            }
            private CellPoint GetPreviousPoint(CellPoint point)
            {
                if (!IsValid(point))
                {
                    int row;
                    for (row = Grid.Items.Count - 1;  0 < row && Grid.Items[row] == CollectionView.NewItemPlaceholder; row--) ;
                    return new CellPoint(row, DisplayColumns.Count - 1);
                }
                CellPoint p = point;
                p.DisplayColumnIndex--;
                if (0 <= p.DisplayColumnIndex)
                {
                    return p;
                }
                p.DisplayColumnIndex = DisplayColumns.Count - 1;
                p.ItemIndex--;
                if (0 <= p.ItemIndex && p.ItemIndex < Grid.Items.Count && Grid.Items[p.ItemIndex] != CollectionView.NewItemPlaceholder)
                {
                    return p;
                }
                for (p.ItemIndex = Grid.Items.Count - 1; 0 < p.ItemIndex && Grid.Items[p.ItemIndex] == CollectionView.NewItemPlaceholder; p.ItemIndex--) ;
                return p;
            }
            private CellPoint GetPreviousPoint()
            {
                return GetPreviousPoint(new CellPoint(this));
            }
            public bool MoveNext()
            {
                CellPoint p = GetNextPoint();
                p.Apply(this);
                return !_terminal.Equals(this);
            }
            public bool MovePrevious()
            {
                CellPoint p = GetPreviousPoint();
                p.Apply(this);
                return !_terminal.Equals(this);
            }
            public void SetTerminal(CellPoint point)
            {
                _terminal = new CellPoint(point);
                if (!IsValid(_terminal))
                {
                    _terminal = GetNextPoint(_terminal);
                }
            }
            public void SetTerminal()
            {
                _terminal = new CellPoint(this);
                if (!IsValid(_terminal))
                {
                    _terminal = GetNextPoint(_terminal);
                }
            }
            public CellInfo(SearchDataGridWindow owner)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                Owner = owner;
                DataGrid grid = owner.Target;
                DataGridCellInfo info = grid.CurrentCell;
                if (info.Item == CollectionView.NewItemPlaceholder)
                {
                    ItemIndex = -1;
                }
                else
                {
                    ItemIndex = grid.Items.IndexOf(info.Item);
                }
                Column = info.Column;
                if (!IsValid())
                {
                    MoveNext();
                }
                SetTerminal();
            }
            public CellInfo(SearchDataGridWindow owner, CellInfo previous)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                Owner = owner;
                DataGrid grid = owner.Target;
                DataGridCellInfo info = grid.CurrentCell;
                if (info.Item == CollectionView.NewItemPlaceholder)
                {
                    ItemIndex = -1;
                }
                else
                {
                    ItemIndex = grid.Items.IndexOf(info.Item);
                }
                Column = info.Column;
                if (!IsValid())
                {
                    MoveNext();
                }
                if (previous != null)
                {
                    SetTerminal(previous._terminal);
                }
                else
                {
                    SetTerminal();
                }
                if (!IsValid(_terminal))
                {
                    _terminal = GetNextPoint(_terminal);
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is CellInfo)
                {
                    return ItemIndex == ((CellInfo)obj).ItemIndex && DisplayColumnIndex == ((CellInfo)obj).DisplayColumnIndex;
                }
                if (obj is CellPoint)
                {
                    return ItemIndex == ((CellPoint)obj).ItemIndex && DisplayColumnIndex == ((CellPoint)obj).DisplayColumnIndex;
                }
                return base.Equals(obj);
            }
            public override int GetHashCode()
            {
                return ItemIndex * 17 + DisplayColumnIndex;
            }
        }

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(DataGrid), typeof(SearchDataGridWindow), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register("IgnoreCase", typeof(bool), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty WordwrapProperty = DependencyProperty.Register("Wordwrap", typeof(bool), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty UseRegexProperty = DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchDataGridWindow));
        private List<DataGridColumn> _displayColumns;
        public SearchDataGridWindow()
        {
            InitializeComponent();
        }

        public DataGrid Target
        {
            get
            {
                return (DataGrid)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        private string _lastSearchText;
        public string SearchText
        {
            get
            {
                return (string)GetValue(SearchTextProperty);
            }
            set
            {
                SetValue(SearchTextProperty, value);
            }
        }

        public bool IgnoreCase
        {
            get
            {
                return (bool)GetValue(IgnoreCaseProperty);
            }
            set
            {
                SetValue(IgnoreCaseProperty, value);
            }
        }

        public bool Wordwrap
        {
            get
            {
                return (bool)GetValue(WordwrapProperty);
            }
            set
            {
                SetValue(WordwrapProperty, value);
            }
        }

        public bool UseRegex
        {
            get
            {
                return (bool)GetValue(UseRegexProperty);
            }
            set
            {
                SetValue(UseRegexProperty, value);
            }
        }

        private int CompareDataGridColumnByDisplayIndex(DataGridColumn x, DataGridColumn y)
        {
            return x.DisplayIndex - y.DisplayIndex;
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            _displayColumns = new List<DataGridColumn>(Target.Columns.Count);
            foreach (DataGridColumn c in Target.Columns)
            {
                if (c.Visibility == Visibility.Visible)
                {
                    _displayColumns.Add(c);
                }
            }
            _displayColumns.Sort(CompareDataGridColumnByDisplayIndex);
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SearchDataGridWindow).OnTargetPropertyChanged(e);
        }

        private DataGridCellInfo FirstCell()
        {
            DataGrid grid = Target;
            if (grid.Items.Count == 0)
            {
                return new DataGridCellInfo();
            }
            if (_displayColumns.Count == 0)
            {
                return new DataGridCellInfo();
            }
            return new DataGridCellInfo(grid.Items[0], _displayColumns[0]);
        }
        private DataGridCellInfo LastCell()
        {
            DataGrid grid = Target;
            if (grid.Items.Count == 0)
            {
                return new DataGridCellInfo();
            }
            if (_displayColumns.Count == 0)
            {
                return new DataGridCellInfo();
            }
            return new DataGridCellInfo(grid.Items[grid.Items.Count - 1], _displayColumns[_displayColumns.Count - 1]);
        }
        private delegate MatchResult MatchTextProc(string text, bool ignoreCase);
        private bool _isMatchTextProcValid = false;
        private MatchTextProc _matchTextProc = null;
        private void UpdateMatchTextProc()
        {
            if (_isMatchTextProcValid)
            {
                return;
            }
            _isMatchTextProcValid = true;
            if (UseRegex)
            {
                if (Wordwrap)
                {
                    _matchTextProc = MatchesRegexWhole;
                }
                else
                {
                    _matchTextProc = MatchesRegex;
                }
            }
            else
            {
                if (Wordwrap)
                {
                    _matchTextProc = EqualsSearchText;
                }
                else
                {
                    _matchTextProc = ContainsSearchText;
                }
            }
        }
        private void InvalidateMatchTextProc()
        {
            _isMatchTextProcValid = false;
        }

        private MatchResult ContainsSearchText(string text, bool ignoreCase)
        {
            int i = text.IndexOf(SearchText, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            return new MatchResult(i, SearchText.Length);
        }
        private MatchResult EqualsSearchText(string text, bool ignoreCase)
        {
            bool flag = string.Equals(text.Trim(), SearchText, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
            return new MatchResult(flag ? 0 : -1, SearchText.Length);
        }

        private MatchResult MatchesRegex(string text, bool ignoreCase)
        {
            Regex re = new Regex(SearchText, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return new MatchResult(re.Match(text));
        }
        private MatchResult MatchesRegexWhole(string text, bool ignoreCase)
        {
            Regex re = new Regex(SearchText, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            Match m = re.Match(text);
            bool flag = (m != null && m.Index == 0 && m.Length == text.Length);
            return new MatchResult(flag ? 0 : -1, SearchText.Length);
        }

        /// <summary>
        /// UpdateMatchTextProc()を事前に呼んでいることが前提
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private MatchResult MatchesText(CellInfo cell)
        {
            if (!cell.IsValid())
            {
                return MatchResult.Unmatched;
            }
            string text = cell.GetText();
            if (string.IsNullOrEmpty(text))
            {
                return MatchResult.Unmatched;
            }
            return _matchTextProc.Invoke(text, IgnoreCase);
        }

        private CellInfo _current;

        private void FindNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid grid = Target;
            if (grid == null)
            {
                return;
            }
            if (grid.Items.Count == 0 || _displayColumns.Count == 0)
            {
                return;
            }
            if (grid.Items.Count == 1 && grid.Items[0] == CollectionView.NewItemPlaceholder)
            {
                return;
            }
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }

            UpdateMatchTextProc();
            if (_lastSearchText != SearchText)
            {
                _lastSearchText = SearchText;
                _current = null;
            }
            MatchResult found = MatchResult.Unmatched;
            try
            {
                CellInfo cur = new CellInfo(this, _current);

                if (_current == null)
                {
                    found = MatchesText(cur);
                }
                while (!found.Success && cur.MoveNext())
                {
                    found = MatchesText(cur);
                }
                if (found.Success)
                {
                    Target.ScrollIntoView(cur.Item, cur.Column);
                    DataGridCellInfo cell = new DataGridCellInfo(cur.Item, cur.Column);
                    grid.CurrentCell = cell;
                    grid.SelectedCells.Clear();
                    grid.SelectedCells.Add(cell);
                    if (grid.BeginEdit())
                    {
                        TextBox tb = cur.Column.GetCellContent(cur.Item) as TextBox;
                        if (tb != null)
                        {
                            tb.Select(found.Index, found.Length);
                        }
                    }
                    _current = cur;
                }
                else
                {
                    _current = null;
                }
            }
            finally
            {
                textBlockNotFound.Visibility = found.Success ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private void FindPreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid grid = Target;
            if (grid == null)
            {
                return;
            }
            if (grid.Items.Count == 0 || _displayColumns.Count == 0)
            {
                return;
            }
            if (grid.Items.Count == 1 && grid.Items[0] == CollectionView.NewItemPlaceholder)
            {
                return;
            }
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }

            UpdateMatchTextProc();
            if (_lastSearchText != SearchText)
            {
                _lastSearchText = SearchText;
                _current = null;
            }
            MatchResult found = MatchResult.Unmatched;
            try
            {
                CellInfo cur = new CellInfo(this, _current);

                if (_current == null)
                {
                    found = MatchesText(cur);
                }
                while (!found.Success && cur.MovePrevious())
                {
                    found = MatchesText(cur);
                }
                if (found.Success)
                {
                    Target.ScrollIntoView(cur.Item, cur.Column);
                    DataGridCellInfo cell = new DataGridCellInfo(cur.Item, cur.Column);
                    grid.CurrentCell = cell;
                    grid.SelectedCells.Clear();
                    grid.SelectedCells.Add(cell);
                    if (grid.BeginEdit())
                    {
                        TextBox tb = cur.Column.GetCellContent(cur.Item) as TextBox;
                        if (tb != null)
                        {
                            tb.Select(found.Index, found.Length);
                        }
                    }
                    _current = cur;
                }
                else
                {
                    _current = null;
                }
            }
            finally
            {
                textBlockNotFound.Visibility = found.Success ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                {
                    SearchCommands.FindPrevious.Execute(null, this);
                    e.Handled = true;
                    return;
                }
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    SearchCommands.FindNext.Execute(null, this);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetBinding(SearchTextProperty, new Binding("SearchText") { Source = MainWindow.Current });
            SetBinding(IgnoreCaseProperty, new Binding("MatchByIgnoreCase") { Source = MainWindow.Current });
            SetBinding(WordwrapProperty, new Binding("MatchByWhole") { Source = MainWindow.Current });
            SetBinding(UseRegexProperty, new Binding("MatchByRegex") { Source = MainWindow.Current });
            CommandBinding b;
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            CommandBindings.Add(b);
            comboBoxKeyword.Focus();
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindPrevious.Execute(null, this);
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindNext.Execute(null, this);
        }
    }
}
