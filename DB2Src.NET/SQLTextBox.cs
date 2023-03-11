using System;
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
        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(Db2SourceContext), typeof(SQLTextBox));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(SQLTextBox));
        
        public Db2SourceContext DataSet
        {
            get { return (Db2SourceContext)GetValue(DataSetProperty); }
            set { SetValue(DataSetProperty, value); }
        }

        private string _plainText;
        private SortedList<int, Inline> _textPosToInline = null;
        private List<int> _lineStartPos = null;
        private bool _plainTextUpdating = false;
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public SQLTextBox()
        {
            Block.SetLineHeight(this, 1.0);
            CommandBindings.Add(new CommandBinding(EditingCommands.EnterParagraphBreak, EnterParagraphBreak_Executed, EnterParagraphBreak_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, Cut_Executed, Cut_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, Copy_Executed));
            //CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete_Executed, Delete_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste_Executed, Paste_CanExecute));
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

        private void ApplyDecoratoin(Token token, FontFamily family, double? size, FontWeight? weight, FontStyle? style, Brush foreground, TextDecorationCollection decorations)
        {
            TextPointer pStart = ToTextPointer(token.StartPos, 0);
            TextPointer pEnd = ToTextPointer(token.EndPos, 1);
            if (pStart == null || pEnd == null)
            {
                return;
            }
            TextRange range = new TextRange(pStart, pEnd);
            range.ClearAllProperties();
            if (family != null)
            {
                range.ApplyPropertyValue(FontFamilyProperty, family);
            }
            if (size.HasValue)
            {
                range.ApplyPropertyValue(FontSizeProperty, size.Value);
            }
            if (weight.HasValue)
            {
                range.ApplyPropertyValue(FontWeightProperty, weight.Value);
            }
            if (style.HasValue)
            {
                range.ApplyPropertyValue(FontStyleProperty, style.Value);
            }
            if (foreground != null)
            {
                range.ApplyPropertyValue(ForegroundProperty, foreground);
            }
            if (decorations != null)
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, decorations);
            }
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
            int i = FindRunIndexRecursive(charactorPosition, 0, _textPosToInline.Count - 1);
            if (i == -1)
            {
                return null;
            }
            return _textPosToInline.Values[i];
        }

        private TextPointer ToTextPointer(int charactorPosition, int delta)
        {
            int i = FindRunIndexRecursive(charactorPosition, 0, _textPosToInline.Count - 1);
            if (i == -1)
            {
                return null;
            }
            Inline found = _textPosToInline.Values[i];
            if (found is Run)
            {
                int p = charactorPosition - _textPosToInline.Keys[i];
                return found.ContentStart.GetPositionAtOffset(p + delta) ?? found.ContentEnd;
            }
            return found.ContentStart;
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
            SelectAll();
            Selection.Text = _plainText;
            UpdateDecoration();
            TextPointer p0 = Document.ContentStart;
            Selection.Select(p0, p0);
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

        private void UpdateDecoration()
        {
            if (DataSet == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(_plainText))
            {
                return;
            }
            Token[] tokens = DataSet.Tokenize(_plainText).ToArray();
            for (int i = tokens.Length - 1; 0 <= i; i--)
            {
                Token token = tokens[i];
                switch (token.Kind)
                {
                    case TokenKind.Comment:
                        ApplyDecoratoin(token, null, null, null, null, Brushes.Green, null);
                        break;
                    case TokenKind.Identifier:
                        if (token.IsReservedWord)
                        {
                            ApplyDecoratoin(token, null, null, FontWeights.Bold, null, null, null);
                        }
                        else
                        {
                            ApplyDecoratoin(token, null, null, null, null, null, null);
                        }
                        break;
                    default:
                        ApplyDecoratoin(token, null, null, null, null, null, null);
                        break;
                }
            }
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
                _textPosToInline = new SortedList<int, Inline>();
                _lineStartPos = new List<int>();
                StringBuilder buf = new StringBuilder();
                bool needNewLine = false;
                _lineStartPos.Add(0);
                foreach (Paragraph block in Document.Blocks)
                {
                    foreach (Inline inline in block.Inlines)
                    {
                        if (needNewLine)
                        {
                            buf.AppendLine();
                            _lineStartPos.Add(buf.Length);
                        }
                        if (inline is Run)
                        {
                            Run run = (Run)inline;
                            _textPosToInline[buf.Length] = run;
                            buf.Append(run.Text);
                        }
                        else if (inline is LineBreak)
                        {
                            _textPosToInline[buf.Length] = inline;
                            buf.AppendLine();
                            _lineStartPos.Add(buf.Length);
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
                Text = _plainText;
            }
            finally
            {
                _plainTextUpdating = false;
            }
        }

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

        public void ScrollIntoTextPoint(TextPointer start, TextPointer end)
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
