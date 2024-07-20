using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace SyntaxEditor
{
    public class TextDocument
    {
        internal enum NewLine
        {
            /// <summary>
            /// 最後の行だけは、改行なしの場合がある
            /// </summary>
            None,
            CRLF,
            LF,
            CR,
        }

        internal enum HitTestResult
        {
            Before = -1,
            Hit = 0,
            After = 1,
        }

        private static readonly Dictionary<NewLine, Tuple<int, string>> NewLineInfos = new Dictionary<NewLine, Tuple<int, string>>()
        {
            { NewLine.None, new Tuple<int, string>(0, string.Empty) },
            { NewLine.CRLF, new Tuple<int, string>(2, "\r\n") },
            { NewLine.LF, new Tuple<int, string>(2, "\n") },
            { NewLine.CR, new Tuple<int, string>(2, "\r") },
        };

        private static readonly Dictionary<string, NewLine> StrToNewLine = new Dictionary<string, NewLine>()
        {
            { string.Empty, NewLine.None },
            { "\r\n", NewLine.CRLF },
            { "\n", NewLine.LF },
            { "\r", NewLine.CR },
        };

        internal class LineInfo
        {
            public LineInfo Previous { get; set; }
            public LineInfo Next { get; set; }
            private int? _lineNo;
            public int LineNo
            {
                get
                {
                    UpdatePosition();
                    return _lineNo.Value;
                }
            }
            private int? _offset;
            public int Offset
            {
                get
                {
                    UpdatePosition();
                    return _offset.Value;
                }
            }
            private int? _textLength = null;
            private void UpdateTextLength()
            {
                if (_textLength.HasValue)
                {
                    return;
                }
                _textLength = Text.Length + NewLineInfos[NewLine].Item1;
            }
            private void InvalidateTextLength()
            {
                _textLength = null;
            }
            public int TextLength
            {
                get
                {
                    UpdateTextLength();
                    return _textLength.Value;
                }
            }

            private string _text;
            public string Text
            {
                get { return _text; }
                set
                {
                    _text = value;
                    InvalidateTextLength();
                    Next?.InvalidatePosition();
                }
            }

            private NewLine _newLine;
            public NewLine NewLine
            {
                get { return _newLine; }
                set
                {
                    if (_newLine == value)
                    {
                        return;
                    }
                    _newLine = value;
                    InvalidateTextLength();
                }
            }
            internal string GetTextWithNewLine()
            {
                return _text + NewLineInfos[NewLine].Item2;
            }

            public void InvalidatePosition()
            {
                if (_offset == null)
                {
                    return;
                }
                _lineNo = null;
                _offset = null;
                Next?.InvalidatePosition();
            }

            public void UpdatePosition()
            {
                if (_offset.HasValue)
                {
                    return;
                }
                if (Previous == null)
                {
                    _offset = 0;
                    _lineNo = 1;
                }
                else
                {
                    _offset = Previous.Offset + Previous.TextLength;
                    _lineNo = Previous.LineNo + 1;
                }
            }

            /// <summary>
            /// positionがこのLineInfo内の文字位置を指し示している場合はHit
            /// 前を指し示している場合はBefore
            /// 後ろを指し示している場合はAfterを返す
            /// </summary>
            /// <returns></returns>
            public HitTestResult HitTest(int position)
            {
                int p0 = Offset;
                int p1 = p0 + TextLength;
                if (p0 <= position && position < p1)
                {
                    return HitTestResult.Hit;
                }
                if (position < p0)
                {
                    return HitTestResult.Before;
                }
                //if (p1 <= position)
                {
                    return HitTestResult.After;
                }
            }

            internal void Unlink()
            {
                _text = null;
                Previous = null;
                Next = null;
            }

            internal LineInfo(string text, int startPosition, int newLineStart, int endPosition, LineInfo previous)
            {
                Previous = previous;
                if (previous != null)
                {
                    Next = previous.Next;
                    previous.Next = this;
                }
                if (Next != null)
                {
                    Next.Previous = this;
                }
                if (startPosition <= newLineStart && newLineStart <= endPosition)
                {
                    _text = text.Substring(startPosition, endPosition - startPosition + 1);
                    NewLine = NewLine.None;
                }
                else
                {
                    _text = text.Substring(startPosition, newLineStart - startPosition);
                    string nl = text.Substring(newLineStart, endPosition - newLineStart + 1);
                    if (StrToNewLine.TryGetValue(nl, out NewLine newLine))
                    {
                        NewLine = newLine;
                    }
                    else
                    {
                        NewLine = NewLine.None;
                    }
                }
                InvalidatePosition();
            }

            /// <summary>
            /// 終端用の空のLineInfo
            /// </summary>
            /// <param name="position"></param>
            /// <param name="previous"></param>
            internal LineInfo(LineInfo previous)
            {
                Previous = previous;
                if (previous != null)
                {
                    Next = previous.Next;
                    previous.Next = this;
                }
                if (Next != null)
                {
                    Next.Previous = this;
                }
                _text = string.Empty;
                NewLine = NewLine.None;
                InvalidatePosition();
            }

            internal static List<LineInfo> NewLines(string text)
            {
                int n = text.Length;
                int pos0 = 0;
                int posNL = 0;
                bool wasCR = false;
                List<LineInfo> lines = new List<LineInfo>();
                LineInfo current = null;
                for (int p = 0; p < n; p++)
                {
                    char c = text[p];
                    switch (c)
                    {
                        case '\n':
                            if (!wasCR)
                            {
                                posNL = p;
                            }
                            current = new LineInfo(text, pos0, posNL, p, current);
                            lines.Add(current);
                            pos0 = p + 1;
                            posNL = -1;
                            break;
                        case '\r':
                            if (wasCR)
                            {
                                current = new LineInfo(text, pos0, posNL, p - 1, current);
                                lines.Add(current);
                                pos0 = p;
                            }
                            posNL = p;
                            break;
                        default:
                            if (wasCR)
                            {
                                current = new LineInfo(text, pos0, posNL, p - 1, current);
                                lines.Add(current);
                                pos0 = p;
                                posNL = -1;
                            }
                            if (char.IsHighSurrogate(c))
                            {
                                p++;
                            }
                            break;
                    }
                    wasCR = (c == '\r');
                }
                if (pos0 < n)
                {
                    current = new LineInfo(text, pos0, posNL, n - 1, current);
                    lines.Add(current);
                }
                if (current == null || current.NewLine != NewLine.None)
                {
                    lines.Add(new LineInfo(current));
                }
                return lines;
            }
            internal static void DisposeLines(IEnumerable<LineInfo> lines)
            {
                if (lines == null)
                {
                    return;
                }
                foreach (LineInfo info in lines)
                {
                    info.Unlink();
                }
            }
            public static readonly LineInfo[] EmptyArray = new LineInfo[0];
        }

        /// <summary>
        /// データの矛盾もしくはプログラムの不備によりありえない結果が返された場合にこの例外を発生させる
        /// </summary>
        public class LineInfoConflictException: Exception { }

        private SyntaxTextBox _owner;
        internal List<LineInfo> Lines;

        private Position _selectionStart;
        private Position _selectionEnd;

        private string _text;
        private object _textLock = new object();

        private void RequireText()
        {
            if (_text != null)
            {
                return;
            }
            lock (_textLock)
            {
                if (_text != null)
                {
                    return;
                }
                if (Lines.Count == 0)
                {
                    _text = string.Empty;
                    return;
                }
                LineInfo last = Lines.Last();
                int n = last.Offset + last.TextLength;
                StringBuilder buf = new StringBuilder(n);
                foreach (LineInfo info in Lines)
                {
                    buf.Append(info.GetTextWithNewLine());
                }
                _text = buf.ToString();
            }
        }

        private void DisposeLines()
        {
            if (Lines == null)
            {
                return;
            }
            LineInfo.DisposeLines(Lines);
            Lines.Clear();
        }

        private void SetLines(string text)
        {
            DisposeLines();
            _text = text;
            Lines = LineInfo.NewLines(text);
        }

        public string Text
        {
            get
            {
                RequireText();
                return _text;
            }
            set
            {
                SetLines(value);
            }
        }

        public int TextLength
        {
            get
            {
                LineInfo last = (0 < Lines.Count) ? Lines[Lines.Count - 1] : null;
                if (last == null)
                {
                    return 0;
                }
                return last.Offset + last.TextLength;
            }
        }

        private int FindLineIndexFromCharacterPositionRecursive(int position, int index0, int index1)
        {
            if (index1 < index0)
            {
                throw new LineInfoConflictException();
            }
            int index;
            index = index0;
            switch (Lines[index].HitTest(position))
            {
                case HitTestResult.Hit:
                    return index;
                case HitTestResult.Before:
                    return index0 - 1;
            }

            int indexM = (index0 + index1 + 1) / 2;
            index = indexM;
            switch (Lines[index].HitTest(position))
            {
                case HitTestResult.Hit:
                    return index;
                case HitTestResult.Before:
                    return FindLineIndexFromCharacterPositionRecursive(position, index0 + 1, index - 1);
            }

            index = index1;
            switch (Lines[index].HitTest(position))
            {
                case HitTestResult.Hit:
                    return index;
                case HitTestResult.Before:
                    return FindLineIndexFromCharacterPositionRecursive(position, indexM + 1, index1 - 1);
                case HitTestResult.After:
                    return index1 + 1;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// 文字位置(0始まり)から行位置(0始まり)を返す。
        /// 前回の実行結果等からだいたいの行位置の目星がついている場合、startLineとして指定することで高速化を図る
        /// </summary>
        /// <param name="position"></param>
        /// <param name="startLine"></param>
        /// <returns></returns>
        public int GetLineFromCharacterPosition(int position, int startLine)
        {
            if (Lines.Count == 0)
            {
                return -1;
            }
            int p = Math.Min(Math.Max(0, startLine), Lines.Count - 1);
            switch (Lines[p].HitTest(position))
            {
                case HitTestResult.Hit:
                    return p;
                case HitTestResult.Before:
                    // 前方は単純二分探索(LineInfo.Offsetの更新を抑制する必要がないため)
                    return FindLineIndexFromCharacterPositionRecursive(position, 0, p - 1);
                case HitTestResult.After:
                    // 後方はLineInfo.Offsetの更新を抑制するために探索範囲を徐々に広げていく
                    int nLine = Lines.Count - 1;
                    for (int n = 2, i0 = position + 1, i1 = position + n; i0 <= nLine ; i0 = i1 + 1, n *= 2, i1 = Math.Min(i0 - n - 1, nLine))
                    {
                        int i = FindLineIndexFromCharacterPositionRecursive(position, i0, i1);
                        if (i0 <= i && i <= i1)
                        {
                            return i;
                        }
                    }
                    return nLine + 1;
            }
            throw new LineInfoConflictException();
        }

        /// <summary>
        /// 文字位置(0始まり)から行位置(0始まり)を返す
        /// </summary>
        /// <param name="position"></param>
        /// <param name="startLine"></param>
        /// <returns></returns>
        public int GetLineFromCharacterPosition(int position)
        {
            return GetLineFromCharacterPosition(position, 0);
        }

        public int SelectionStart { get { return _selectionStart.Offset; } }
        public int SelectionStartLineIndex { get { return _selectionStart.LineIndex; } }
        public int SelectionStartColumnIndex { get { return _selectionStart.ColumnIndex; } }
        public int SelectionEnd { get { return _selectionEnd.Offset; } }
        public int SelectionEndLineIndex { get { return _selectionEnd.LineIndex; } }
        public int SelectionEndColumnIndex { get { return _selectionEnd.ColumnIndex; } }

        public int SelectionLength { get { return _selectionEnd.Offset - _selectionStart.Offset; } }

        public void Select(int start, int end)
        {
            _selectionStart.Offset = Math.Min(start, end);
            _selectionEnd.Offset = Math.Max(start, end);
        }

        private LineInfo[] GetSelectedLines()
        {
            if (Lines.Count == 0)
            {
                return LineInfo.EmptyArray;
            }
            int i0 = _selectionStart.LineIndex;
            int i1 = _selectionEnd.LineIndex;
            LineInfo[] ret = new LineInfo[i1 - i0 + 1];
            Lines.CopyTo(ret, i0);
            return ret;
        }

        private void ReplaceSelectedText(string value)
        {
            var newLines = LineInfo.NewLines(value);
            var curLines = GetSelectedLines();
            if (curLines.Length == 0)
            {
                Lines = newLines;
                return;
            }
            LineInfo cur0 = curLines[0];
            LineInfo cur1 = curLines[curLines.Length - 1];
        }

        public string SelectedText
        {
            get
            {
                _selectionStart.Offset = Math.Max(0, _selectionStart.Offset);
                _selectionEnd.Offset = Math.Min(_selectionEnd.Offset, TextLength);
                if (_selectionEnd.Offset <= _selectionStart.Offset)
                {
                    return string.Empty;
                }
                return _text.Substring(_selectionStart.Offset, _selectionEnd.Offset - _selectionStart.Offset);
            }
            set
            {
                if (_selectionStart.Offset == 0 && _selectionEnd.Offset == TextLength)
                {
                    Text = value;
                }
                else if (_selectionEnd.Offset == 0)
                {
                    Text = value + Text;
                }
                else if (_selectionStart.Offset == TextLength)
                {
                    Text += value;
                }
                else if (_selectionStart.Offset == 0)
                {
                    Text = value + Text.Substring(_selectionEnd.Offset);
                }
                else if (_selectionEnd.Offset == TextLength)
                {
                    Text = Text.Substring(0, _selectionStart.Offset) + value;
                }
                else
                {
                    Text = Text.Substring(0, _selectionStart.Offset) + value + Text.Substring(_selectionEnd.Offset);
                }
                _selectionEnd.Offset = _selectionStart.Offset + value.Length;
            }
        }

        internal TextDocument(SyntaxTextBox owner)
        {
            _owner = owner;
            _selectionStart = new Position(this, 0);
            _selectionEnd = new Position(this, 0);
        }
    }
}
