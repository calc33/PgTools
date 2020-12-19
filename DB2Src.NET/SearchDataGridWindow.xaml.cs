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
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(DataGrid), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register("IgnoreCase", typeof(bool), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty WordwrapProperty = DependencyProperty.Register("Wordwrap", typeof(bool), typeof(SearchDataGridWindow));
        public static readonly DependencyProperty UseRegexProperty = DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchDataGridWindow));
        private List<DataGridColumn> _displayColumns;
        private DataGridCellInfo _lastSelected = new DataGridCellInfo();
        private DataGridCellInfo _endCell = new DataGridCellInfo();
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
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
        private DataGridCellInfo NextCell(DataGridCellInfo cell)
        {
            int c = _displayColumns.IndexOf(cell.Column);
            int r = Target.Items.IndexOf(cell.Item);
            c++;
            if (_displayColumns.Count <= c)
            {
                c = 0;
                r++;
            }
            if (Target.Items.Count <= r || Target.Items[r] == CollectionView.NewItemPlaceholder)
            {
                r = 0;
            }
            return new DataGridCellInfo(Target.Items[r], _displayColumns[c]);
        }
        private DataGridCellInfo PreviousCell(DataGridCellInfo cell)
        {
            int c = _displayColumns.IndexOf(cell.Column);
            int r = Target.Items.IndexOf(cell.Item);
            c--;
            if (c < 0)
            {
                c = _displayColumns.Count - 1;
                r--;
            }
            if (r <= 0)
            {
                r = Target.Items.Count - 1;
            }
            for (; 0 < r && Target.Items[r] == CollectionView.NewItemPlaceholder; r--) ;
            return new DataGridCellInfo(Target.Items[r], _displayColumns[c]);
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
        /// <param name="text"></param>
        /// <param name="item"></param>
        /// <param name="column"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private MatchResult MatchesText(DataGridCellInfo cell)
        {
            if (!cell.IsValid)
            {
                return MatchResult.Unmatched;
            }
            FrameworkElement elem = cell.Column.GetCellContent(cell.Item);
            if (elem == null)
            {
                return MatchResult.Unmatched;
            }
            PropertyInfo prop = elem.GetType().GetProperty("Text");
            string text = prop?.GetValue(elem) as string;
            if (string.IsNullOrEmpty(text))
            {
                return MatchResult.Unmatched;
            }
            return _matchTextProc.Invoke(text, IgnoreCase);
        }

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
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }

            UpdateMatchTextProc();
            MatchResult found = MatchResult.Unmatched;
            try
            {
                DataGridCellInfo cur = grid.CurrentCell;
                if (!_lastSelected.IsValid || !_lastSelected.Equals(cur))
                {
                    if (!cur.IsValid)
                    {
                        cur = FirstCell();
                    }
                    if (!cur.IsValid)
                    {
                        found = MatchResult.Unmatched;
                        return;
                    }
                    _endCell = cur;
                    found = MatchesText(cur);
                }
                if (!found.Success)
                {
                    for (cur = NextCell(cur); !found.Success && cur != _endCell; cur = NextCell(cur))
                    {
                        found = MatchesText(cur);
                        if (found.Success)
                        {
                            break;
                        }
                    }
                }
                if (found.Success)
                {
                    grid.CurrentCell = cur;
                    grid.SelectedCells.Clear();
                    grid.SelectedCells.Add(cur);
                    if (grid.BeginEdit())
                    {
                        TextBox tb = cur.Column.GetCellContent(cur.Item) as TextBox;
                        if (tb != null)
                        {
                            tb.Select(found.Index, found.Length);
                        }
                    }
                    _lastSelected = cur;
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
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }

            UpdateMatchTextProc();
            MatchResult found = MatchResult.Unmatched;
            try
            {
                DataGridCellInfo cur = grid.CurrentCell;
                if (!_lastSelected.IsValid || !_lastSelected.Equals(cur))
                {
                    if (!cur.IsValid)
                    {
                        cur = FirstCell();
                    }
                    if (!cur.IsValid)
                    {
                        found = MatchResult.Unmatched;
                        return;
                    }
                    _endCell = cur;
                    found = MatchesText(cur);
                }
                if (!found.Success)
                {
                    for (cur = PreviousCell(cur); !found.Success && cur != _endCell; cur = PreviousCell(cur))
                    {
                        found = MatchesText(cur);
                        if (found.Success)
                        {
                            break;
                        }
                    }
                }
                if (found.Success)
                {
                    grid.CurrentCell = cur;
                    grid.SelectedCells.Clear();
                    grid.SelectedCells.Add(cur);
                    if (grid.BeginEdit())
                    {
                        TextBox tb = cur.Column.GetCellContent(cur.Item) as TextBox;
                        if (tb != null)
                        {
                            tb.Select(found.Index, found.Length);
                        }
                    }
                    _lastSelected = cur;
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
    public struct MatchResult
    {
        public int Index { get; private set; }
        public int Length { get; private set; }
        public bool Success
        {
            get
            {
                return (Index != -1) && (0 < Length);
            }
        }
        public MatchResult(int start, int length)
        {
            Index = start;
            Length = length;
        }
        public MatchResult(Match match)
        {
            Index = match.Index;
            Length = match.Length;
        }
        public static readonly MatchResult Unmatched = new MatchResult(-1, 0);
    }
}
