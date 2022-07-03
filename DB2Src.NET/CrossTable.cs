using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Db2Source
{
    public partial class CrossTable: DependencyObject
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(CrossTable));

        private SortedList<AxisValueArray, SummaryCell> _cells = null;
        private object _recordsLock = new object();

        private void AddRecordInternal(SortedList<AxisValueArray, SummaryCell> list, AxisValueArray key, object value)
        {
            SummaryCell rec;
            if (!list.TryGetValue(key, out rec))
            {
                rec = new SummaryCell(Axises, key, SummaryDefinitions);
                list.Add(key, rec);
            }
            rec.Add(value);
        }

        private void AddRecordRecursive(SortedList<AxisValueArray, SummaryCell> list, AxisValueArray key, int replacingIndex, object value)
        {
            if (replacingIndex == key.Count)
            {
                AddRecordInternal(list, key, value);
                return;
            }
            AddRecordRecursive(list, key, replacingIndex + 1, value);
            AxisValueArray key2 = new AxisValueArray(key);
            key2[replacingIndex] = Axises[replacingIndex].NoValue;
            AddRecordRecursive(list, key2, replacingIndex + 1, value);
        }

        private void UpdateCells()
        {
            if (_cells != null)
            {
                return;
            }
            lock (_recordsLock)
            {
                if (_cells != null)
                {
                    return;
                }
                Axis[] axises = Axises.ToArray();
                SortedList<AxisValueArray, SummaryCell> l = new SortedList<AxisValueArray, SummaryCell>();
                if (ItemsSource == null)
                {
                    _cells = l;
                    return;
                }
                foreach (object obj in ItemsSource)
                {
                    AxisValueArray key = new AxisValueArray(obj, axises);
                    AddRecordRecursive(l, key, 0, obj);
                }
                _cells = l;
            }
        }

        private void InvalidateCells()
        {
            _cells = null;
        }

        public static int CompareAxisEntry(AxisEntry item1, AxisEntry item2)
        {
            if (item1 == null || item2 == null)
            {
                return (item1 != null ? 0 : 1) - (item2 != null ? 0 : 1);
            }
            if (item1.Values.Count != item2.Values.Count)
            {
                throw new ArgumentException();
            }
            int n = item1.Values.Count;
            for (int i = 0; i < n; i++)
            {
                AxisValue v1 = item1.Values[i];
                AxisValue v2 = item2.Values[i];
                int ret = v1.Index.CompareTo(v2.Index);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }

        /// <summary>
        /// axisesで縦軸もしくは横軸に表示する可能性のある項目一覧を渡し、
        /// 表示用にグループ化したエントリの一覧を生成する
        /// </summary>
        /// <param name="axises"></param>
        /// <returns></returns>
        public List<AxisEntry>[] GetAxisEntries(Axis[][] axises)
        {
            UpdateCells();
            int n = axises.Length;
            List<AxisEntry>[] lists = new List<AxisEntry>[n];
            for(int i = 0; i < n; i++)
            {
                List<AxisEntry> entries = new List<AxisEntry>();
                Dictionary<AxisValueArray, List<SummaryCell>> dict = new Dictionary<AxisValueArray, List<SummaryCell>>();
                foreach (SummaryCell cell in _cells.Values)
                {
                    AxisValueArray subKey = new AxisValueArray(cell.KeyAxis, axises[i]);
                    List<SummaryCell> l;
                    if (!dict.TryGetValue(subKey, out l))
                    {
                        l = new List<SummaryCell>();
                        dict.Add(subKey, l);
                    }
                    l.Add(cell);
                }
                foreach (KeyValuePair<AxisValueArray, List<SummaryCell>> pair in dict)
                {
                    AxisEntry entry = new AxisEntry() { Values = pair.Key };
                    entry.Cells.AddRange(pair.Value);
                    entries.Add(entry);
                }
                entries.Sort(CompareAxisEntry);
                AxisEntry last = entries[0];
                AxisEntry[] store = new AxisEntry[last.Values.Count];
                store[0] = last;
                //for (int k = 0; k < store.Length; k++)
                //{
                //    store[k] = last;
                //}
                for (int j = 1; j < entries.Count; j++)
                {
                    AxisEntry entry = entries[j];
                    for (int k = 0; k < store.Length; k++)
                    {
                        if (last.Values[k].IsNoValue && !entry.Values[k].IsNoValue)
                        {

                        }
                    }
                }
                lists[i] = entries;
            }
            return lists;
        }

        private Type _itemType;
        protected internal Type ItemType
        {
            get { return _itemType; }
            set
            {
                if (_itemType == value)
                {
                    return;
                }
                _itemType = value;
                UpdateAxisCandidatesByItemType();
            }
        }

        private void UpdateItemType()
        {
            if (ItemsSource == null)
            {
                return;
            }
            IEnumerator enumerator = ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return;
            }
            Type t = enumerator.Current?.GetType();
            if (t == null || ItemType == t)
            {
                return;
            }
            if (_itemType != null && (t.IsSubclassOf(_itemType) || t.IsAssignableFrom(_itemType)))
            {
                return;
            }
            ItemType = t;
        }

        private void UpdateAxisCandidatesByRowCollection(RowCollection rows)
        {
            List<Axis> l = new List<Axis>(rows.Owner.Fields.Length);
            foreach (ColumnInfo column in rows.Owner.Fields)
            {
                if (column.HiddenLevel != HiddenLevel.Visible)
                {
                    continue;
                }
                Axis axis = new Axis(this) { Title = column.Name, PropertyName = "Items", PropertyIndexes = new object[] { column.Index }, StringFormat = column.StringFormat };
                l.Add(axis);
            }
            _axisCandidates = l.ToArray();
            ItemType = typeof(Row);
        }

        private void UpdateAxisCandidatesByItemType()
        {
            if (ItemType == null)
            {
                _axisCandidates = new Axis[0];
                return;
            }
            PropertyInfo[] props = ItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<Axis> l = new List<Axis>(props.Length);
            foreach (PropertyInfo p in props)
            {
                l.Add(new Axis(this) { PropertyName = p.Name, Title = p.Name });
            }
            _axisCandidates = l.ToArray();
        }

        private object _axisCandidatesLock = new object();
        private void UpdateAxisCandidates() {
            if (_axisCandidates != null)
            {
                return;
            }
            lock (_axisCandidatesLock)
            {
                if (_axisCandidates != null)
                {
                    return;
                }
                if (ItemsSource == null)
                {
                    _axisCandidates = new Axis[0];
                    return;
                }
                if (ItemsSource is RowCollection)
                {
                    UpdateAxisCandidatesByRowCollection((RowCollection)ItemsSource);
                    return;
                }
                UpdateItemType();
            }
        }

        private void InvalidateAxisCandidates()
        {
            _axisCandidates = null;
        }

        /// <summary>
        /// 指定した軸の交差するセルを取得する。
        /// 指定しない軸については小計を返す
        /// </summary>
        /// <param name="axisValues"></param>
        /// <returns></returns>
        public SummaryCell Find(params AxisValue[] axisValues)
        {
            UpdateCells();
            AxisValueArray key = new AxisValueArray(Axises);
            foreach (AxisValue value in axisValues)
            {
                int i = value.Owner.Index;
                if (i == -1)
                {
                    throw new ArgumentException();
                }
                key[i] = value;
            }
            SummaryCell cell;
            if (!_cells.TryGetValue(key, out cell))
            {
                return null;
            }
            return cell;
        }



        private Axis[] _axisCandidates = null;
        public Axis[] AxisCandidates
        {
            get
            {
                UpdateAxisCandidates();
                return _axisCandidates;
            }
            //private set;
        }

        public AxisCollection Axises { get; }

        public List<SummaryDefinition> SummaryDefinitions { get; } = new List<SummaryDefinition>();

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private void ItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateAxisCandidates();
            InvalidateCells();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsSourceProperty)
            {
                ItemsSourcePropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public CrossTable()
        {
            Axises = new AxisCollection();
            Axises.CollectionChanged += Axises_CollectionChanged;
        }

        private void Axises_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateCells();
        }
    }
}
