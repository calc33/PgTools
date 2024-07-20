using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxEditor
{
    public struct Position
    {
        private readonly TextDocument _owner;
        private int _offset;
        /// <summary>
        /// 文字位置(0始まり)
        /// </summary>
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        private int _lineIndex;
        private int _columnIndex;
        /// <summary>
        /// _lineIndexと_columnIndexの値を更新する
        /// 高速化のため_lineIndex,_columnIndexが既に正しい値が入っていれば更新しない
        /// </summary>
        private void AdjustLineColumn()
        {
            try
            {
                if (0 <= _lineIndex && _lineIndex < _owner.Lines.Count && _owner.Lines[_lineIndex].Offset + _columnIndex == _offset)
                {
                    return;
                }
                if (_owner.Lines.Count == 0 && _lineIndex == -1 && _columnIndex == 0)
                {
                    return;
                }
            }
            catch { }
            _lineIndex = _owner.GetLineFromCharacterPosition(_offset);
            _columnIndex = (0 <= _lineIndex) ? _offset - _owner.Lines[_lineIndex].Offset : 0;
        }

        /// <summary>
        /// 行位置(0始まり)
        /// </summary>
        public int LineIndex
        {
            get
            {
                AdjustLineColumn();
                return _lineIndex;
            }
        }
        public int ColumnIndex
        {
            get
            {
                AdjustLineColumn();
                return _columnIndex;
            }
        }

        /// <summary>
        /// 行内の列位置(0始まり)
        /// </summary>
        public Position(TextDocument owner, int offset)
        {
            _owner = owner;
            _offset = offset;
            _lineIndex = -1;
            _columnIndex = 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Position))
            {
                return false;
            }
            Position other = (Position)obj;
            return _offset == other._offset;
        }

        public override int GetHashCode()
        {
            return _offset.GetHashCode();
        }

        public override string ToString()
        {
            AdjustLineColumn();
            if (_lineIndex == -1)
            {
                return string.Format("%d(invalid)", _offset);
            }
            else
            {
                return string.Format("%d(L%d C%d)", _offset, _lineIndex + 1, _columnIndex + 1);
            }
        }
    }
}
