using System;

namespace Db2Source
{
    public class CellValueChangedEventArgs : EventArgs
    {
        public Row Row { get; private set; }
        public int Index { get; private set; }
        internal CellValueChangedEventArgs(Row row, int index)
        {
            Row = row;
            Index = index;
        }
    }
}
