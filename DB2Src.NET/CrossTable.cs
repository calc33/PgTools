using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public partial class CrossTable
    {
        private SortedList<Axis.AxisValueArray, SummaryCell> _cells = null;
        private object _recordsLock = new object();

        private void AddRecordInternal(SortedList<Axis.AxisValueArray, SummaryCell> list, Axis.AxisValueArray key, object value)
        {
            SummaryCell rec;
            if (!list.TryGetValue(key, out rec))
            {
                rec = new SummaryCell(Axises, key, SummaryDefinitions);
                list.Add(key, rec);
            }
            rec.Add(value);
        }

        private void AddRecordRecursive(SortedList<Axis.AxisValueArray, SummaryCell> list, Axis.AxisValueArray key, int replacingIndex, object value)
        {
            if (replacingIndex == key.Count)
            {
                AddRecordInternal(list, key, value);
                return;
            }
            AddRecordRecursive(list, key, replacingIndex + 1, value);
            Axis.AxisValueArray key2 = new Axis.AxisValueArray(key);
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
                SortedList<Axis.AxisValueArray, SummaryCell> l = new SortedList<Axis.AxisValueArray, SummaryCell>();
                if (ItemsSource == null)
                {
                    _cells = l;
                    return;
                }
                foreach (object obj in ItemsSource)
                {
                    Axis.AxisValueArray key = new Axis.AxisValueArray(obj, axises);
                    AddRecordRecursive(l, key, 0, obj);
                }
                _cells = l;
            }
        }
        private void InvalidateCells()
        {
            _cells = null;
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
            AxisCandidates = l.ToArray();
        }

        private void UpdateAxisCandidatesByItemType()
        {
            if (ItemType == null)
            {
                AxisCandidates = new Axis[0];
                return;
            }
            PropertyInfo[] props = ItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<Axis> l = new List<Axis>(props.Length);
            foreach (PropertyInfo p in props)
            {
                l.Add(new Axis(this) { PropertyName = p.Name, Title = p.Name });
            }
            AxisCandidates = l.ToArray();
        }

        private void UpdateAxisCandidates() {
            if (ItemsSource == null)
            {
                return;
            }
            if (ItemsSource is RowCollection)
            {
                UpdateAxisCandidatesByRowCollection((RowCollection)ItemsSource);
                return;
            }
            UpdateItemType();
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
            Axis.AxisValueArray key = new Axis.AxisValueArray(Axises);
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

        public Axis[] AxisCandidates { get; private set; } = new Axis[0];

        public AxisCollection Axises { get; }

        public List<SummaryDefinition> SummaryDefinitions { get; } = new List<SummaryDefinition>();
        private IEnumerable _itemsSource;
        public IEnumerable ItemsSource
        {
            get
            {
                return _itemsSource;
            }
            set
            {
                _itemsSource = value;
                UpdateAxisCandidates();
                InvalidateCells();
            }
        }

        public CrossTable()
        {
            Axises = new AxisCollection(this);
        }
    }
}
