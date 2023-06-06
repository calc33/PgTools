using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        }

        /// <summary>
        /// データの矛盾もしくはプログラムの不備によりよりありえない結果が返された場合にこの例外を発生させる
        /// </summary>
        public class LineInfoConflictException: Exception { }

        private List<LineInfo> _lines;
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
                StringBuilder buf = new StringBuilder();
                foreach (LineInfo info in _lines)
                {
                    buf.Append(info.GetTextWithNewLine());
                }
                _text = buf.ToString();
            }
        }

        private void DisposeLines()
        {
            if (_lines == null)
            {
                return;
            }
            List<LineInfo> lines = _lines;
            _lines = null;
            foreach (LineInfo info in lines)
            {
                info.Unlink();
            }
            lines.Clear();
        }
        private void SetLines(string text)
        {
            _text = text;
            int n = text.Length;
            int pos0 = 0;
            int posNL = 0;
            bool wasCR = false;
            DisposeLines();
            _lines = new List<LineInfo>();
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
                        _lines.Add(current);
                        pos0 = p + 1;
                        posNL = -1;
                        break;
                    case '\r':
                        if (wasCR)
                        {
                            current = new LineInfo(text, pos0, posNL, p - 1, current);
                            _lines.Add(current);
                            pos0 = p;
                        }
                        posNL = p;
                        break;
                    default:
                        if (wasCR)
                        {
                            current = new LineInfo(text, pos0, posNL, p - 1, current);
                            _lines.Add(current);
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
                _lines.Add(current);
            }
            if (current == null || current.NewLine != NewLine.None)
            {
                _lines.Add(new LineInfo(current));
            }
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

        private int FindLineIndexFromCharacterPositionRecursive(int position, int index0, int index1)
        {
            if (index1 < index0)
            {
                throw new LineInfoConflictException();
            }
            int index;
            index = index0;
            switch (_lines[index].HitTest(position))
            {
                case HitTestResult.Hit:
                    return index;
                case HitTestResult.Before:
                    return index0 - 1;
            }

            int indexM = (index0 + index1 + 1) / 2;
            index = indexM;
            switch (_lines[index].HitTest(position))
            {
                case HitTestResult.Hit:
                    return index;
                case HitTestResult.Before:
                    return FindLineIndexFromCharacterPositionRecursive(position, index0 + 1, index - 1);
            }

            index = index1;
            switch (_lines[index].HitTest(position))
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
            if (_lines.Count == 0)
            {
                return -1;
            }
            int p = Math.Min(Math.Max(0, startLine), _lines.Count - 1);
            switch (_lines[p].HitTest(position))
            {
                case HitTestResult.Hit:
                    return p;
                case HitTestResult.Before:
                    // 前方は単純二分探索(LineInfo.Offsetの更新を抑制する必要がないため)
                    return FindLineIndexFromCharacterPositionRecursive(position, 0, p - 1);
                case HitTestResult.After:
                    // 後方はLineInfo.Offsetの更新を抑制するために探索範囲を徐々に広げていく
                    int nLine = _lines.Count - 1;
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

        public TextDocument() { }
    }
}
