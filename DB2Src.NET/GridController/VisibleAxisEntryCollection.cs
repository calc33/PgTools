using System.Collections.Generic;
using System.Collections.Specialized;

namespace Db2Source
{
    public class VisibleAxisEntryCollection
    {
        private AxisEntryCollection _baseList;
        private List<AxisEntry> _list;
        public AxisEntryCollection BaseList
        {
            get
            {
                return _baseList;
            }
            private set
            {
                if (_baseList == value)
                {
                    return;
                }
                if (_baseList != null)
                {
                    _baseList.CollectionChanged -= BaseList_CollectionChanged;
                }
                _baseList = value;
                if (_baseList != null)
                {
                    _baseList.CollectionChanged += BaseList_CollectionChanged;
                }
            }
        }

        private object _listLock = new object();
        private void UpdateList()
        {
            if (_list != null)
            {
                return;
            }
            lock (_listLock)
            {
                if (_list != null)
                {
                    return;
                }
                List<AxisEntry> l = new List<AxisEntry>();
                int n = _baseList.Count;
                for (int i = 0; i < n;)
                {
                    AxisEntry entry = _baseList[i];
                    l.Add(entry);
                    if (entry.IsFolded)
                    {
                        for (; i < n && entry.Level < _baseList[i].Level; i++) ;
                    }
                    else
                    {
                        i++;
                    }
                }
                _list = l;
            }
        }

        private void InvalidateList()
        {
            _list = null;
        }

        private void BaseList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateList();
        }

        public AxisValue this[int index, int level]
        {
            get
            {
                UpdateList();
                return _list[index].Values[level];
            }
        }
        public AxisEntryStatus DisplayStatus(int index, int level)
        {
            UpdateList();
            AxisEntry entry = _list[index];
            if (entry.Level == level)
            {
                return AxisEntryStatus.Visible;
            }
            else if (entry.Level < level)
            {
                return AxisEntryStatus.JoinPriorEntry;
            }
            else //if (level < entry.Level)
            {
                return AxisEntryStatus.JoinPriorLevel;
            }
        }
    }
}