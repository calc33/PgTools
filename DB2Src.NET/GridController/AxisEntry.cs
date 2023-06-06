using System.Collections.Generic;
using System.ComponentModel;

namespace Db2Source
{
    /// <summary>
    /// クロス集計で表示する際の項目の組み合わせを定義する。
    /// クロス集計の行・列それぞれに対して作成する
    /// </summary>
    public class AxisEntry : INotifyPropertyChanged
    {
        public int Level { get; private set; }
        public AxisValueArray Values { get; set; }
        public List<CrossTable.SummaryCell> Cells { get; } = new List<CrossTable.SummaryCell>();
        /// <summary>
        /// 小計を表示して子要素を折り畳み表示する場合、子要素がここに格納される
        /// </summary>
        public List<AxisEntry> Children { get; set; } = new List<AxisEntry>();
        public AxisEntryStatus[] Status { get; private set; }
        public object[] Contents { get; private set; }


        private bool _isFolded = false;

        /// <summary>
        /// 子要素を非表示にして小計のみ表示したい場合はtrue
        /// </summary>
        public bool IsFolded
        {
            get
            {
                return _isFolded && IsFoldable;
            }
            set
            {
                bool v = value && IsFoldable;
                if (_isFolded == v)
                {
                    return;
                }
                _isFolded = v;
                OnPropertyChanged(new PropertyChangedEventArgs("IsFolded"));
            }
        }

        private bool? _isFoldable = null;
        private void UpdateIsFoldable()
        {
            if (_isFoldable.HasValue)
            {
                return;
            }
            Axis axis = (0 <= Level) ? Values[Level].Owner : null;
            _isFoldable = (axis != null) ? axis.ShowSubtotal : false;
        }
        /// <summary>
        /// 子要素を畳める場合はtrue、畳めない場合はfalse
        /// (ShowSubtotal=trueの場合にtrue)
        /// </summary>
        public bool IsFoldable
        {
            get
            {
                UpdateIsFoldable();
                return _isFoldable.Value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public AxisEntry() { }
        public AxisEntry(AxisValueArray values)
        {
            Values = values;
            int n = Values.Count;
            Status = new AxisEntryStatus[n];
            Contents = new object[n];
            for (int i = 0; i < n; i++)
            {
                if (Values[i].IsNoValue)
                {
                    Level = i - 1;
                    break;
                }
            }
            for (int i = 0; i < n; i++)
            {
                if (i < Level)
                {
                    Status[i] = AxisEntryStatus.JoinPriorEntry;
                    Contents[i] = null;
                }
                else if (i == Level)
                {
                    Status[i] = AxisEntryStatus.Visible;
                    Contents[i] = Values[i].Value;
                }
                else
                {
                    Status[i] = AxisEntryStatus.JoinPriorLevel;
                    Contents[i] = null;
                }
            }
            UpdateIsFoldable();
        }

        public void MergeSingleChild()
        {
            if (Children.Count == 1)
            {
                AxisEntry child = Children[0];
                if (Equals(Values[Level], child.Values[child.Level]))
                {
                    Status[child.Level] = AxisEntryStatus.JoinPriorLevel;
                }
                else
                {
                    Status[child.Level] = AxisEntryStatus.Visible;
                    Contents[child.Level] = child.Contents[child.Level];
                }
                Values = child.Values;
                Children = child.Children;
            }
            foreach (AxisEntry entry in Children)
            {
                entry.MergeSingleChild();
            }
        }
    }
}
