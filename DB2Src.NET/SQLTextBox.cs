using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Db2Source
{
    public class SQLTextBox : RichTextBox
    {
        public class TokenCollection : IReadOnlyCollection<Token>
        {
            private Token[] _tokens;

            public Token this[int index] { get { return _tokens[index]; } }

            public int Count { get { return _tokens.Length; } }

            private int FindTokenIndexRecursive(int charactoerPosition, GapAlignment alignment, int start, int end)
            {
                if (end < start)
                {
                    return -1;
                }
                int hit = this[start].IsHit(charactoerPosition, alignment);
                if (hit <= 0)
                {
                    return start;
                }
                hit = this[end].IsHit(charactoerPosition, alignment);
                if (0 <= hit)
                {
                    return end;
                }
                int i = (start + end) / 2;
                hit = this[i].IsHit(charactoerPosition, alignment);
                if (hit == 0)
                {
                    return i;
                }
                if (hit < 0)
                {
                    return FindTokenIndexRecursive(charactoerPosition, alignment, start + 1, i - 1);
                }
                return FindTokenIndexRecursive(charactoerPosition, alignment, i + 1, end - 1);
            }

            public int GetTokenIndexAtPosition(int charactoerPosition, GapAlignment alignment)
            {
                return FindTokenIndexRecursive(charactoerPosition, alignment, 0, Count - 1);
            }

            public Token GetTokenAtPosition(int charactoerPosition, GapAlignment alignment)
            {
                int i = FindTokenIndexRecursive(charactoerPosition, alignment, 0, Count - 1);
                if (i == -1)
                {
                    return null;
                }
                i = Math.Min(i, Count - 1);
                return this[i];
            }

            public void ApplySyntaxDecoration(SQLTextBox textBox)
            {
                for (int i = _tokens.Length - 1; 0 <= i; i--)
                {
                    Token token = _tokens[i];
                    textBox.SyntaxDecorations.ApplyTo(textBox, token);
                }
                textBox.InvalidateTextPosToInline();
            }

            private static string[] SplitByNewLine(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return new string[0];
                }
                List<string> lines = new List<string>();
                bool wasCr = false;
                int i0 = 0;
                for (int i = 0, n = value.Length; i < n; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '\n':
                            if (!wasCr)
                            {
                                lines.Add(value.Substring(i0, i - i0));
                            }
                            break;
                        case '\r':
                            lines.Add(value.Substring(i0, i - i0));
                            break;
                    }
                    wasCr = c == '\r';
                }
                return lines.ToArray();
            }

            public void BuildDocument(SQLTextBox textBox)
            {
                textBox.Document.Blocks.Clear();
                Paragraph block = new Paragraph();
                textBox.Document.Blocks.Add(block);
                foreach (Token token in _tokens)
                {
                    if (token.Kind == TokenKind.NewLine)
                    {
                        block.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        Run run = new Run(token.Value);
                        textBox.SyntaxDecorations[token]?.Value?.Apply(run, true);
                        block.Inlines.Add(run);
                    }
                }
            }

            public IEnumerator<Token> GetEnumerator()
            {
                return ((IEnumerable<Token>)_tokens).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _tokens.GetEnumerator();
            }

            internal TokenCollection(IEnumerable<Token> tokens)
            {
                _tokens = tokens.ToArray();
            }
        }
        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(Db2SourceContext), typeof(SQLTextBox));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(SQLTextBox));
        public static readonly DependencyProperty SyntaxDecorationsProperty = DependencyProperty.Register("SyntaxDecorations", typeof(SyntaxDecorationCollection), typeof(SQLTextBox));

        public static SyntaxDecorationCollection DefaultDecolations;

        public Db2SourceContext DataSet
        {
            get { return (Db2SourceContext)GetValue(DataSetProperty); }
            set { SetValue(DataSetProperty, value); }
        }

        private string _plainText;
        private SortedList<int, Inline> _textPosToInline = null;
        private Dictionary<Inline, int> _inlineToTextPos = null;
        private List<int> _lineStartPos = null;

        public TokenCollection Tokens { get; private set; }

        private SortedList<int, Inline> TextPosToInline
        {
            get
            {
                UpdateTextPosToInline();
                return _textPosToInline;
            }
        }
        private Dictionary<Inline, int> InlineToTextPos
        {
            get
            {
                UpdateTextPosToInline();
                return _inlineToTextPos;
            }
        }
        private List<int> LineStartPos
        {
            get
            {
                UpdateTextPosToInline();
                return _lineStartPos;
            }
        }

        private bool _plainTextUpdating = false;
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string SelectedText
        {
            get
            {
                return Selection.Text;
            }
            set
            {
                Selection.Text = value;
            }
        }
        public int SelectionStart
        {
            get
            {
                return ToCharacterPosition(Selection.Start);
            }
        }
        public int SelectionEnd
        {
            get
            {
                return ToCharacterPosition(Selection.End);
            }
        }
        public int SelectionLength
        {
            get
            {
                return SelectionEnd - SelectionStart;
            }
        }

        public SyntaxDecorationCollection SyntaxDecorations
        {
            get { return (SyntaxDecorationCollection)GetValue(SyntaxDecorationsProperty); }
            set { SetValue(SyntaxDecorationsProperty, value); }
        }

        public SQLTextBox()
        {
            Block.SetLineHeight(this, 1.0);
            CommandBindings.Add(new CommandBinding(EditingCommands.EnterParagraphBreak, EnterParagraphBreak_Executed, EnterParagraphBreak_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, Cut_Executed, Cut_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, Copy_Executed));
            //CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete_Executed, Delete_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste_Executed, Paste_CanExecute));
            SyntaxDecorations = DefaultDecolations;
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsReadOnly;
            e.Handled = true;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Selection.Text = string.Empty;
            e.Handled = true;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetText(Selection.Text);
            e.Handled = true;
        }

        private void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsReadOnly;
            e.Handled = true;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetText(Selection.Text);
            Selection.Text = string.Empty;
            e.Handled = true;
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsReadOnly;
            e.Handled = true;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string text = Clipboard.GetText();
            Selection.Text = text;
            TextPointer newEnd = Selection.End;
            Selection.Select(newEnd, newEnd);
            e.Handled = true;
        }

        private void EnterParagraphBreak_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsReadOnly;
            e.Handled = true;
        }

        private void EnterParagraphBreak_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextPointer newEnd = Selection.End;
            newEnd = newEnd.InsertLineBreak();

            Selection.Select(newEnd, newEnd);
            e.Handled = true;
        }

        private int FindRunIndexRecursive(int charactorPosition, int start, int end)
        {
            if (end < start)
            {
                return -1;
            }
            if (start == end)
            {
                return start;
            }
            UpdateTextPosToInline();
            if (_textPosToInline.Keys[end] <= charactorPosition)
            {
                return end;
            }
            if (charactorPosition < _textPosToInline.Keys[start])
            {
                return -1;
            }
            if (_textPosToInline.Keys[start] <= charactorPosition && charactorPosition < _textPosToInline.Keys[start + 1])
            {
                return start;
            }
            int i = (start + end) / 2;
            if (_textPosToInline.Keys[i] <= charactorPosition)
            {
                return FindRunIndexRecursive(charactorPosition, i, end - 1);
            }
            else
            {
                return FindRunIndexRecursive(charactorPosition, start + 1, i - 1);
            }
        }

        private Inline ToInline(int charactorPosition, int delta)
        {
            UpdateTextPosToInline();
            int i = FindRunIndexRecursive(charactorPosition, 0, _textPosToInline.Count - 1);
            if (i == -1)
            {
                return null;
            }
            return _textPosToInline.Values[i];
        }

        public TextPointer ToTextPointer(int charactorPosition, int delta)
        {
            UpdateTextPosToInline();
            int i = FindRunIndexRecursive(charactorPosition, 0, _textPosToInline.Count - 1);
            if (i == -1)
            {
                //return null;
                return Document.ContentStart;
            }
            Inline found = _textPosToInline.Values[i];
            if (found is Run)
            {
                int p = charactorPosition - _textPosToInline.Keys[i];
                return found.ContentStart.GetPositionAtOffset(p + delta) ?? found.ContentEnd;
            }
            return found.ContentStart;
        }

        public int ToCharacterPosition(TextPointer pointer)
        {
            UpdateTextPosToInline();
            Inline inline = pointer.Parent as Inline;
            if (inline == null)
            {
                return -1;
            }
            int p;
            if (!InlineToTextPos.TryGetValue(inline, out p))
            {
                return -1;
            }
            if (inline is Run)
            {
                Run run = (Run)inline;
                p += run.ContentStart.GetOffsetToPosition(pointer);
            }
            return p;
        }

        private int FindLineRecursive(int charactorPosition, int start, int end)
        {
            if (end < start)
            {
                return -1;
            }
            if (start == end)
            {
                return start;
            }
            UpdateTextPosToInline();
            if (_lineStartPos[end] <= charactorPosition)
            {
                return end;
            }
            if (charactorPosition < _lineStartPos[start])
            {
                return -1;
            }
            if (_lineStartPos[start] <= charactorPosition && charactorPosition < _lineStartPos[start + 1])
            {
                return start;
            }
            int i = (start + end) / 2;
            if (_lineStartPos[i] <= charactorPosition)
            {
                return FindLineRecursive(charactorPosition, i, end - 1);
            }
            else
            {
                return FindLineRecursive(charactorPosition, start + 1, i - 1);
            }
        }

        private void ApplyPlainText()
        {
            if (_plainTextUpdating)
            {
                return;
            }
            _plainTextUpdating = true;
            try
            {
                if (DataSet == null)
                {
                    Document.Blocks.Clear();
                    if (!string.IsNullOrEmpty(_plainText))
                    {
                        Document.Blocks.Add(new Paragraph(new Run(_plainText)));
                    }
                }
                else
                {
                    List<Token> l = new List<Token>();
                    ExtractTokenRecursive(l, DataSet.Tokenize(_plainText));
                    Tokens = new TokenCollection(l);
                    Tokens.BuildDocument(this);
                }
                TextPointer p0 = Document.ContentStart;
                Selection.Select(p0, p0);
            }
            finally
            {
                _plainTextUpdating = false;
            }
        }

        private static T FindFirstVisualChild<T>(DependencyObject item) where T : DependencyObject
        {
            int n = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> l = new List<DependencyObject>();
            for (int i = 0; i < n; i++)
            {
                DependencyObject obj = VisualTreeHelper.GetChild(item, i);
                if (obj is T)
                {
                    return (T)obj;
                }
                l.Add(obj);
            }
            foreach (DependencyObject obj in l)
            {
                DependencyObject ret = FindFirstVisualChild<T>(obj);
                if (ret is T)
                {
                    return (T)ret;
                }
            }
            return null;
        }

        private void ExtractTokenRecursive(List<Token> list, TokenizedSQL tokens)
        {
            foreach (Token token in tokens)
            {
                if (token.DefBody != null)
                {
                    ExtractTokenRecursive(list, token.DefBody);
                }
                else
                {
                    list.Add(token);
                }
            }
        }

        private void UpdateTextPosToInline()
        {
            if (_textPosToInline != null)
            {
                return;
            }
            _textPosToInline = new SortedList<int, Inline>();
            _inlineToTextPos = new Dictionary<Inline, int>();
            _lineStartPos = new List<int>();
            bool needNewLine = false;
            int n = 0;
            int newLineLength = Environment.NewLine.Length;
            _lineStartPos.Add(0);
            foreach (Paragraph block in Document.Blocks)
            {
                foreach (Inline inline in block.Inlines)
                {
                    if (needNewLine)
                    {
                        n += newLineLength;
                        _lineStartPos.Add(n);
                    }
                    _inlineToTextPos[inline] = n;
                    if (inline is Run)
                    {
                        Run run = (Run)inline;
                        _textPosToInline[n] = run;
                        n += run.Text?.Length ?? 0;
                    }
                    else if (inline is LineBreak)
                    {
                        _textPosToInline[n] = inline;
                        n += newLineLength;
                        _lineStartPos.Add(n);
                    }
                    needNewLine = (inline.NextInline == null);
                }
            }
        }

        private void InvalidateTextPosToInline()
        {
            _textPosToInline = null;
            _inlineToTextPos = null;
            _lineStartPos = null;
        }

        private void UpdateDecoration()
        {
            if (DataSet == null)
            {
                return;
            }
            if (SyntaxDecorations == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(_plainText))
            {
                return;
            }
            List<Token> l = new List<Token>();
            ExtractTokenRecursive(l, DataSet.Tokenize(_plainText));
            Tokens = new TokenCollection(l);
            Tokens.ApplySyntaxDecoration(this);
        }

        private void UpdatePlainText()
        {
            if (_plainTextUpdating)
            {
                return;
            }
            _plainTextUpdating = true;
            try
            {
                StringBuilder buf = new StringBuilder();
                bool needNewLine = false;
                foreach (Paragraph block in Document.Blocks)
                {
                    foreach (Inline inline in block.Inlines)
                    {
                        if (needNewLine)
                        {
                            buf.AppendLine();
                        }
                        if (inline is Run)
                        {
                            Run run = (Run)inline;
                            buf.Append(run.Text);
                        }
                        else if (inline is LineBreak)
                        {
                            buf.AppendLine();
                        }
                        needNewLine = (inline.NextInline == null);
                    }
                }
                string s = buf.ToString();
                if (_plainText == s)
                {
                    return;
                }
                _plainText = s;
                UpdateDecoration();
                InvalidateTextPosToInline();
                Text = _plainText;
            }
            finally
            {
                _plainTextUpdating = false;
            }
        }

        //private void SelectWordByToken(MouseButtonEventArgs e)
        //{
        //    if (e.ClickCount == 1)
        //    {
        //        return;
        //    }
        //    if (Tokens == null)
        //    {
        //        return;
        //    }
        //    TextPointer pointer = GetPositionFromPoint(e.GetPosition(this), true);
        //    int p = ToCharacterPosition(pointer);
        //    Token token = Tokens.GetTokenAtPosition(p, GapAlignment.Before);
        //    if (token == null)
        //    {
        //        return;
        //    }
        //    if (token.Kind != TokenKind.Identifier)
        //    {
        //        return;
        //    }
        //    if (3 < e.ClickCount)
        //    {
        //        return;
        //    }
        //    if (Keyboard.Modifiers != ModifierKeys.None)
        //    {
        //        return;
        //    }
        //    bool isQuoted = token.Value.StartsWith("\"");
        //    if (!isQuoted && e.ClickCount == 3)
        //    {
        //        return;
        //    }
        //    // TextBoxの標準動作をさせたい場合はここまででreturn
        //    bool inQuote = isQuoted && e.ClickCount == 2;
        //    TextPointer start = ToTextPointer(token.StartPos, inQuote ? 1 : 0);
        //    TextPointer end = ToTextPointer(token.EndPos, inQuote ? 0 : 1);
        //    if (start == null || end == null)
        //    {
        //        return;
        //    }
        //    Selection.Select(start, end);
        //    e.Handled = true;
        //}

        //protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        //{
        //    if (e.ChangedButton == MouseButton.Left)
        //    {
        //        SelectWordByToken(e);
        //    }
        //    if (!e.Handled)
        //    {
        //        base.OnPreviewMouseDown(e);
        //    }
        //}

        //protected override void OnMouseDown(MouseButtonEventArgs e)
        //{
        //    if (e.ChangedButton == MouseButton.Left)
        //    {
        //        SelectWordByToken(e);
        //    }
        //    if (!e.Handled)
        //    {
        //        base.OnMouseDown(e);
        //    }
        //}

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            UpdatePlainText();
            base.OnTextChanged(e);
        }

        private void OnDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                return;
            }
            UpdateDecoration();
        }
        private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_plainTextUpdating || _plainText == (string)e.NewValue)
            {
                return;
            }
            _plainText = (string)e.NewValue;
            ApplyPlainText();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DataSetProperty)
            {
                OnDataSetPropertyChanged(e);
            }
            if (e.Property == TextProperty)
            {
                OnTextPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public void Select(int start, int length)
        {
            TextPointer p0 = ToTextPointer(start, 0);
            TextPointer p1 = ToTextPointer(start + length, 0);
            Selection.Select(p0, p1);
        }
        public int GetLineIndexFromCharacterIndex(int charIndex)
        {
            UpdateTextPosToInline();
            if (charIndex < _lineStartPos[0])
            {
                return -1;
            }
            int n = _lineStartPos.Count - 1;
            //if (_lineStartPos[n] < charactorPosition)
            //{
            //    return n;
            //}
            return FindLineRecursive(charIndex, 0, n);
        }

        public void ScrollToLine(int lineIndex)
        {
            UpdateTextPosToInline();
            int l = Math.Max(0, Math.Min(lineIndex, _lineStartPos.Count - 1));
            if (l <= 0)
            {
                ScrollToHome();
                return;
            }
            if (_lineStartPos.Count - 1 <= l)
            {
                ScrollToEnd();
                return;
            }
            Rect rect = ToInline(_lineStartPos[l], 0).ContentStart.GetCharacterRect(LogicalDirection.Forward);
            Rect rect2 = ToInline(_lineStartPos[l + 1], 0).ContentStart.GetCharacterRect(LogicalDirection.Forward);
            rect.Height = rect2.Top - rect.Top;
            ScrollViewer sv = FindFirstVisualChild<ScrollViewer>(this);
            double h = (sv.Content as FrameworkElement).ActualHeight;
            if (sv.VerticalOffset + h < rect.Bottom)
            {
                ScrollToVerticalOffset(rect.Bottom + Document.LineHeight - h);
                return;
            }
            if (rect.Top < sv.VerticalOffset)
            {
                ScrollToVerticalOffset(rect.Top - Document.LineHeight);
                return;
            }
        }

        /// <summary>
        /// startとendの間にある文字列の表示範囲を返す
        /// startからendまでの間に折り返しや改行がある場合は先頭行のみの表示範囲を返す
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Rect GetCharacterRect(TextPointer start, TextPointer end)
        {
            ScrollViewer sv = FindFirstVisualChild<ScrollViewer>(this);
            Rect rect = start.GetCharacterRect(LogicalDirection.Forward);
            Rect rect1 = end.GetCharacterRect(LogicalDirection.Backward);
            rect.Height = rect1.Bottom - rect.Top;
            if (rect1.Right < rect.Left)
            {
                rect.Width = ((sv.Content as ContentControl).Content as FrameworkElement).ActualWidth - rect.Left;
            }
            else
            {
                rect.Width = rect1.Right - rect.Left;
            }
            return rect;
        }

        public void ScrollIntoTextPoint(TextPointer start, TextPointer end)
        {
            Rect rect = GetCharacterRect(start, end);
            ScrollViewer sv = FindFirstVisualChild<ScrollViewer>(this);
            FrameworkElement content = sv.Content as FrameworkElement;
            double w = content.ActualWidth;
            double h = content.ActualHeight;
            if (rect.Left < sv.HorizontalOffset)
            {
                sv.ScrollToHorizontalOffset(Math.Min(rect.Left, rect.Right - w));
            }
            else if (sv.HorizontalOffset + w < rect.Right)
            {
                sv.ScrollToHorizontalOffset(Math.Max(rect.Left, rect.Right - w));
            }

            if (sv.VerticalOffset + h < rect.Bottom)
            {
                ScrollToVerticalOffset(rect.Bottom + Document.LineHeight - h);
            }
            else if (rect.Top < sv.VerticalOffset)
            {
                ScrollToVerticalOffset(rect.Top - Document.LineHeight);
            }
        }

        public void ScrollIntoTextRange(TextRange range)
        {
            ScrollIntoTextPoint(range.Start, range.End);
        }

        public void ScrollIntoTextRange(int charIndexStart, int charIndexEnd)
        {
            TextPointer p0 = ToTextPointer(charIndexStart, 0);
            TextPointer p1 = ToTextPointer(charIndexEnd, 0);
            ScrollIntoTextPoint(p0, p1);
        }

        public void ScrollIntoSelection()
        {
            ScrollIntoTextRange(Selection);
        }
    }
}
