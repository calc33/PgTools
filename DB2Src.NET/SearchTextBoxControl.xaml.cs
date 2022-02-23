using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// UserControl1.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchTextBoxControl: UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(TextBox), typeof(SearchTextBoxControl));
        private static readonly RotateTransform _rotate90 = new RotateTransform(90);
        public RotateTransform Rotate90 { get { return _rotate90; } }
        private static Dictionary<TextBox, SearchTextBoxControl> _textBoxToSearchTextBoxControl = new Dictionary<TextBox, SearchTextBoxControl>();

        public static SearchTextBoxControl GetSearchTextBoxControlFor(TextBox textBox)
        {
            SearchTextBoxControl ret;
            if (!_textBoxToSearchTextBoxControl.TryGetValue(textBox, out ret))
            {
                return null;
            }
            return ret;
        }

        public TextBox Target
        {
            get
            {
                return (TextBox)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        private bool _needFocusTextBoxSearch = false;
        private void Find_Executed(object sender, RoutedEventArgs e)
        {
            if (IsVisible)
            {
                textBoxSearch.SelectAll();
                textBoxSearch.Focus();
                _needFocusTextBoxSearch = false;
            }
            else
            {
                Visibility = Visibility.Visible;
                _needFocusTextBoxSearch = true;
            }
        }
        private void FindNext_Executed(object sender, RoutedEventArgs e)
        {
            SearchForward();
        }
        private void FindPrevious_Executed(object sender, RoutedEventArgs e)
        {
            SearchBackword();
        }
        private void UpdateMargin()
        {
            if (Target == null)
            {
                return;
            }
            if (!IsVisible)
            {
                return;
            }
            ScrollViewer sc = Target.Template.FindName("PART_ContentHost", Target) as ScrollViewer;
            if (sc == null)
            {
                return;
            }
            ScrollBar sbV = sc.Template.FindName("PART_VerticalScrollBar", sc) as ScrollBar;
            ScrollBar sbH = sc.Template.FindName("PART_HorizontalScrollBar", sc) as ScrollBar;

            double w = (sbV != null) && sbV.IsVisible ? sbV.ActualWidth : 0;
            double h = (sbH != null) && sbH.IsVisible ? sbH.ActualHeight : 0;
            Thickness m = new Thickness(
                Target.Margin.Left + Target.BorderThickness.Left,
                Target.Margin.Top + Target.BorderThickness.Top,
                Target.Margin.Right + Target.BorderThickness.Right + w,
                Target.Margin.Bottom + Target.BorderThickness.Bottom + h);
            if (!Margin.Equals(m))
            {
                Margin = m;
            }
        }

        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Target != null && !Target.IsFocused)
            {
                Target.Focus();
            }
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Target != null && !Target.IsFocused)
            {
                Target.Focus();
            }
        }

        private void RemoveTextBoxHandler(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }
            for (int i = textBox.CommandBindings.Count - 1; 0 <= i; i--)
            {
                ICommand cmd = textBox.CommandBindings[i].Command;
                if (cmd == ApplicationCommands.Find || cmd == SearchCommands.FindNext || cmd == SearchCommands.FindPrevious)
                {
                    textBox.CommandBindings.RemoveAt(i);
                }
            }
            textBox.PreviewMouseWheel -= TextBox_PreviewMouseWheel;
            textBox.PreviewMouseDown -= TextBox_PreviewMouseDown;
        }

        private void AddTextBoxHandler(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, Find_Executed));
            textBox.CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, FindNext_Executed));
            textBox.CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, FindPrevious_Executed));
            textBox.IsInactiveSelectionHighlightEnabled = true;
            textBox.PreviewMouseWheel += TextBox_PreviewMouseWheel;
            textBox.PreviewMouseDown += TextBox_PreviewMouseDown;
            textBox.MouseDoubleClick += TextBox_MouseDoubleClick;
            textBox.PreviewMouseDoubleClick += TextBox_MouseDoubleClick;
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }
            TextBox tb = e.Source as TextBox;
            if (tb == null)
            {
                return;
            }
            int p = tb.GetCharacterIndexFromPoint(e.MouseDevice.GetPosition(tb), true);
            Tuple<int, int> ret;
            if (tb.SelectionLength == 0)
            {
                ret = GetWordAt(tb.Text, p);
            }
            else
            {
                ret = GetLineAt(tb.Text, p, false);
            }
            if (ret != null)
            {
                tb.Select(ret.Item1, ret.Item2);
                e.Handled = true;
            }
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                TextBox tb = (TextBox)e.OldValue;
                _textBoxToSearchTextBoxControl.Remove(tb);
                RemoveTextBoxHandler(tb);
            }
            if (e.NewValue != null)
            {
                TextBox tb = (TextBox)e.NewValue;
                _textBoxToSearchTextBoxControl[tb] = this;
                AddTextBoxHandler(tb);
                UpdateMargin();
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            //FrameworkElement elem = Parent as FrameworkElement;
            //elem.LayoutUpdated += Parent_LayoutUpdated;
            base.OnVisualParentChanged(oldParent);
        }

        //private void Parent_LayoutUpdated(object sender, EventArgs e)
        //{
        //    UpdateMargin();
        //}

        public SearchTextBoxControl()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Visibility = Visibility.Collapsed;
            }
            textBoxSearch.IsVisibleChanged += TextBoxSearch_IsVisibleChanged;
            IsVisibleChanged += SearchTextBoxControl_IsVisibleChanged;
        }

        private void SearchTextBoxControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                UpdateMargin();
                Dispatcher.InvokeAsync(textBoxSearch.Focus, System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        public static Tuple<int, int> GetWordAt(string value, int position)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            //List<string> l = new List<string>();
            Tuple<int, int> target = null;
            int p = position;
            for (int i = 0, n = value.Length; i < n;)
            {
                int i0 = i;
                char c = value[i];
                if (char.IsWhiteSpace(value, i))
                {
                    for (i++; i < n && char.IsWhiteSpace(value, i); i++) ;
                    //l.Add(value.Substring(i0, i - i0));
                    if (p < i)
                    {
                        return target;
                    }
                }
                else if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.CurrencySymbol)
                {
                    i++;
                    if (char.IsDigit(value, i))
                    {
                        for (; i < n && char.IsDigit(value, i); i++) ;
                    }
                    else
                    {
                        for (; i < n && value[i] == c; i++) ;
                    }
                    //l.Add(value.Substring(i0, i - i0));
                    target = new Tuple<int, int>(i0, i - i0);
                    if (p < i)
                    {
                        return target;
                    }
                }
                else if (".+-".IndexOf(c) != -1)
                {
                    i++;
                    if (char.IsDigit(value, i))
                    {
                        for (; i < n && (char.IsDigit(value, i) || (".,-".IndexOf(value[i]) != -1)); i++) ;
                    }
                    else
                    {
                        for (; i < n && value[i] == c; i++) ;
                    }
                    target = new Tuple<int, int>(i0, i - i0);
                    if (p < i)
                    {
                        return target;
                    }
                }
                else if (char.IsDigit(value, i))
                {
                    for (i++; i < n && (char.IsDigit(value, i) || (".,-".IndexOf(value[i]) != -1)); i++) ;
                    //l.Add(value.Substring(i0, i - i0));
                    target = new Tuple<int, int>(i0, i - i0);
                    if (p < i)
                    {
                        return target;
                    }
                }
                else if (char.IsLetter(value, i) || c == '_')
                {
                    for (; i < n && (char.IsLetter(value, i) || value[i] == '_'); i++)
                    {
                        if (char.IsSurrogate(value, i))
                        {
                            i++;
                        }
                    }
                    //l.Add(value.Substring(i0, i - i0));
                    target = new Tuple<int, int>(i0, i - i0);
                    if (p < i)
                    {
                        return target;
                    }
                }
                else
                {
                    for (i++; i < n && value[i] == c; i++) ;
                    //l.Add(value.Substring(i0, i - i0));
                    target = new Tuple<int, int>(i0, i - i0);
                    if (p < i)
                    {
                        return target;
                    }
                }
            }
            return target;
        }

        private static Tuple<int, int> _GetLineWithoutNL(string value, int position)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            Tuple<int, int> target = null;
            int p = position;
            int i0 = 0;
            for (int i = 0, n = value.Length; i < n; i++)
            {
                if (char.IsSurrogate(value, i))
                {
                    i++;
                    continue;
                }
                char c = value[i];
                if (c == '\r' || c == '\n')
                {
                    if (position < i)
                    {
                        return new Tuple<int, int>(i0, i - i0);
                    }
                    i0 = i + 1;
                }
            }
            return target;
        }
        private static Tuple<int, int> _GetLineWithNL(string value, int position)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            //List<string> l = new List<string>();
            Tuple<int, int> target = null;
            int p = position;
            int i0 = 0;
            bool wasCR = false;
            for (int i = 0, n = value.Length; i < n; i++)
            {
                if (char.IsSurrogate(value, i))
                {
                    i++;
                    continue;
                }
                char c = value[i];
                if (c == '\n')
                {
                    if (position <= i)
                    {
                        return new Tuple<int, int>(i0, i - i0 + 1);
                    }
                    i0 = i + 1;
                }
                else if (wasCR)
                {
                    if (position < i)
                    {
                        return new Tuple<int, int>(i0, i - i0);
                    }
                    i0 = i;
                }
                wasCR = (c == '\r');
            }
            return target;
        }

        public static Tuple<int, int> GetLineAt(string value, int position, bool includeNewLine)
        {
            if (includeNewLine)
            {
                return _GetLineWithNL(value, position);
            }
            else
            {
                return _GetLineWithoutNL(value, position);
            }
        }

        private void TextBoxSearch_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!textBoxSearch.IsVisible)
            {
                return;
            }
            if (!_needFocusTextBoxSearch)
            {
                return;
            }
            try
            {
                textBoxSearch.Focus();
                try
                {
                    //if (!string.IsNullOrEmpty(textBoxSearch.Text))
                    //{
                    //    return;
                    //}
                    if (Target == null)
                    {
                        return;
                    }
                    string s = Target.SelectedText;
                    if (string.IsNullOrEmpty(s))
                    {
                        string txt = Target.Text;
                        Tuple<int, int> ret = GetWordAt(txt, Target.SelectionStart);
                        if (ret != null)
                        {
                            s = txt.Substring(ret.Item1, ret.Item2);
                        }
                    }
                    if (!string.IsNullOrEmpty(s))
                    {
                        textBoxSearch.Text = s;
                    }
                }
                finally
                {
                    textBoxSearch.SelectAll();
                }
            }
            finally
            {
                _needFocusTextBoxSearch = false;
            }
        }

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }

        private delegate int FindProc(string text, int start, string keyword, StringComparison comparison);

        private static int FindNextSimple(string text, int start, string keyword, StringComparison comparison)
        {
            return text.IndexOf(keyword, start, comparison);
        }

        private static int FindPrevSimple(string text, int start, string keyword, StringComparison comparison)
        {
            return text.LastIndexOf(keyword, start, comparison);
        }

        private static bool IsWordSeparator(string s, int index)
        {
            if (index < 0 || s.Length <= index)
            {
                return true;
            }
            char c = s[index];
            return (char.IsWhiteSpace(c) || char.IsSeparator(c) || char.IsPunctuation(c)) && (c != '_');

        }
        private static int FindNextWord(string text, int start, string keyword, StringComparison comparison)
        {
            int p = start;
            int n = text.Length;
            while (p < n)
            {
                p = FindNextSimple(text, p, keyword, comparison);
                if (p == -1)
                {
                    return -1;
                }
                if (IsWordSeparator(text, p - 1) && IsWordSeparator(text, p + keyword.Length))
                {
                    return p;
                }
                p++;
            }
            return -1;
        }

        private static int FindPrevWord(string text, int start, string keyword, StringComparison comparison)
        {
            int p = start - 1;
            int n = text.Length;
            while (p < n)
            {
                p = FindPrevSimple(text, p, keyword, comparison);
                if (p == -1)
                {
                    return -1;
                }
                if (IsWordSeparator(text, p - 1) && IsWordSeparator(text, p + keyword.Length))
                {
                    return p;
                }
                p--;
            }
            return -1;
        }

        private static readonly Dictionary<StringComparison, RegexOptions> _stringComparisonToRegexOptions = new Dictionary<StringComparison, RegexOptions>()
        {
            { StringComparison.CurrentCulture, RegexOptions.None },
            { StringComparison.CurrentCultureIgnoreCase, RegexOptions.IgnoreCase },
            { StringComparison.InvariantCulture, RegexOptions.CultureInvariant },
            { StringComparison.InvariantCultureIgnoreCase,  RegexOptions.CultureInvariant | RegexOptions.IgnoreCase },
            { StringComparison.Ordinal, RegexOptions.None },
            { StringComparison.OrdinalIgnoreCase, RegexOptions.IgnoreCase },
        };
        private static int FindNextRegex(string text, int start, string keyword, StringComparison comparison)
        {
            RegexOptions opt;
            if (!_stringComparisonToRegexOptions.TryGetValue(comparison, out opt))
            {
                opt = RegexOptions.None;
            }
            Regex re = new Regex(keyword, opt);
            Match ret = re.Match(text, start);
            if (!ret.Success)
            {
                return -1;
            }
            return ret.Index;
        }

        private static int FindPrevRegex(string text, int start, string keyword, StringComparison comparison)
        {
            RegexOptions opt;
            if (!_stringComparisonToRegexOptions.TryGetValue(comparison, out opt))
            {
                opt = RegexOptions.None;
            }
            opt |= RegexOptions.RightToLeft;
            Regex re = new Regex(keyword, opt);
            Match ret = re.Match(text, start);
            if (!ret.Success)
            {
                return -1;
            }
            return ret.Index;
        }

        //private static Size GetTextBoxClientSize(TextBox textBox)
        //{
        //    ScrollViewer sc = textBox.Template.FindName("PART_ContentHost", textBox) as ScrollViewer;
        //    return (sc != null) ? new Size(sc.ViewportWidth, sc.ViewportHeight) : new Size(textBox.ActualWidth, textBox.ActualHeight);
        //}

        /// <summary>
        /// テキストボックスの選択領域の座標を返す
        /// 選択領域が複数行にまたがっている場合は最初の行のみの座標を返す
        /// </summary>
        /// <param name="textBox"></param>
        /// <returns></returns>
        private static Rect GetSelectionFirstLineRect(TextBox textBox)
        {
            int p0 = textBox.SelectionStart;
            int p1 = textBox.SelectionStart + textBox.SelectionLength - 1;
            int l0 = textBox.GetLineIndexFromCharacterIndex(p0);
            int l1 = textBox.GetLineIndexFromCharacterIndex(p1);
            Rect r = textBox.GetRectFromCharacterIndex(p0);
            if (l0 != l1)
            {
                p1 = textBox.GetCharacterIndexFromLineIndex(l0 + 1) - 1;
            }
            r = Rect.Union(r, textBox.GetRectFromCharacterIndex(p1));
            return r;
        }

        private void AdjustControlPosition(TextBox textBox)
        {
            Rect rS = GetSelectionFirstLineRect(textBox);
            FrameworkElement parent = Parent as FrameworkElement;
            double x = 0;
            double y = 0;
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    x = 0;
                    break;
                case HorizontalAlignment.Center:
                    x = (parent.ActualWidth - ActualWidth) / 2;
                    break;
                case HorizontalAlignment.Right:
                    x = parent.ActualWidth - ActualWidth;
                    break;
            }
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    y = 0;
                    break;
                case VerticalAlignment.Center:
                    y = (parent.ActualHeight - ActualHeight) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    y = parent.ActualHeight - ActualHeight;
                    break;
            }
            Rect rC = new Rect(x, y, ActualWidth, ActualHeight);
            if (rS.IntersectsWith(rC))
            {
                if (VerticalAlignment == VerticalAlignment.Top)
                {
                    VerticalAlignment = VerticalAlignment.Bottom;
                }
                else
                {
                    VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }
        private void SelectTextBox(TextBox textBox, int start, int length)
        {
            IInputElement old = Keyboard.FocusedElement;
            try
            {
                textBox.Focus();
                textBox.Select(start, length);
                AdjustControlPosition(textBox);
            }
            finally
            {
                old.Focus();
            }
        }

        public bool SearchForward()
        {
            if (Target == null)
            {
                return false;
            }
            string key = textBoxSearch.Text;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            string s = Target.Text;
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            int p0 = Target.SelectionStart;
            StringComparison comp = IsChecked(checkBoxCaseful) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            FindProc proc = FindNextSimple;
            int p;
            if (IsChecked(checkBoxWardwrap))
            {
                proc = FindNextWord;
            }
            if (IsChecked(checkBoxRegex))
            {
                proc = FindNextRegex;
            }
            p = proc(s, p0 + 1, key, comp);
            if (p == -1)
            {
                p = proc(s, 0, key, comp);
                if (p0 <= p)
                {
                    p = -1;
                }
            }
            if (p == -1)
            {
                return false;
            }
            SelectTextBox(Target, p, key.Length);
            return true;
        }

        public bool SearchBackword()
        {
            if (Target == null)
            {
                return false;
            }
            string key = textBoxSearch.Text;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            string s = Target.Text;
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            int p0 = Target.SelectionStart;
            StringComparison comp = IsChecked(checkBoxCaseful) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            FindProc proc = FindPrevSimple;
            int p;
            if (IsChecked(checkBoxWardwrap))
            {
                proc = FindPrevWord;
            }
            if (IsChecked(checkBoxRegex))
            {
                proc = FindPrevRegex;
            }
            p = proc(s, p0 - 1, key, comp);
            if (p == -1)
            {
                p = proc(s, s.Length - 1, key, comp);
                if (p <= p0)
                {
                    p = -1;
                }
            }
            if (p == -1)
            {
                return false;
            }
            SelectTextBox(Target, p, key.Length);
            return true;
        }

        public bool Search()
        {
            SaveToRegistry();
            if ((int)buttonSearch.Tag == -1)
            {
                return SearchBackword();
            }
            else
            {
                return SearchForward();
            }
        }

        private RegistryBinding _registryBinding = new RegistryBinding();
        private void InitRegistryBinding()
        {
            _registryBinding.Register("SearchText", "Keyword", textBoxSearch, "Text", new StringOperator());
            _registryBinding.Register("SearchText", "ShowOption", checkBoxFoldOption, "IsChecked", new NullableBoolOperator());
            _registryBinding.Register("SearchText", "Caseful", checkBoxCaseful, "IsChecked", new NullableBoolOperator());
            _registryBinding.Register("SearchText", "Wordwrap", checkBoxWardwrap, "IsChecked", new NullableBoolOperator());
            _registryBinding.Register("SearchText", "Regex", checkBoxRegex, "IsChecked", new NullableBoolOperator());
        }

        public void LoadFromRegistry()
        {
            _registryBinding.Load(App.RegistryFinder);
        }
        public void SaveToRegistry()
        {
            _registryBinding.Save(App.RegistryFinder);
        }

        private void buttonReverse_Click(object sender, RoutedEventArgs e)
        {
            SearchTextDirectionDropDown win = new SearchTextDirectionDropDown();
            win.Owner = Window.GetWindow(this);
            win.Target = buttonSearch;
            win.Show();
            //int dir = (int)buttonSearch.Tag;
            //if (dir == 1)
            //{
            //    buttonSearch.RenderTransform = new ScaleTransform(-1.0, 1.0);
            //    buttonSearch.Tag = -1;
            //}
            //else
            //{
            //    buttonSearch.RenderTransform = null;
            //    buttonSearch.Tag = 1;
            //}
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            Target?.Focus();
            Visibility = Visibility.Collapsed;
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void textBoxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Search();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    Target?.Focus();
                    Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    break;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFromRegistry();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                SaveToRegistry();
            }
        }

        private void buttonMovePos_Click(object sender, RoutedEventArgs e)
        {
            if (VerticalAlignment == VerticalAlignment.Top)
            {
                VerticalAlignment = VerticalAlignment.Bottom;
                buttonMovePos.Content = "⇧";
            }
            else
            {
                VerticalAlignment = VerticalAlignment.Top;
                buttonMovePos.Content = "⇩";
            }
        }
    }
    public static class SearchCommands
    {
        public static RoutedCommand FindNext = new RoutedCommand(Properties.Resources.FindNextCommand_Name, typeof(FrameworkElement), new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F3) }));
        public static RoutedCommand FindPrevious = new RoutedCommand(Properties.Resources.FindPreviousCommand_Name, typeof(FrameworkElement), new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F3, ModifierKeys.Shift) }));
    }
}
