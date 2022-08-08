using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum SummaryOperation
    {
        List = 0,
        Summary = 1,
        Average = 2,
        Minimum = 3,
        Maximum = 4,
        Count = 5,
        StdDev = 6,
        Variance = 7
    }

    partial class CrossTable
    {
        public abstract class SummaryOperatorBase
        {
            protected object _result;
            public abstract bool CanApply(Type valueType);
            protected bool _isResultValid = false;
            private object _resultLock = new object();
            protected abstract void UpdateResultCore();
            protected void UpdateResult()
            {
                if (_isResultValid)
                {
                    return;
                }
                lock (_resultLock)
                {
                    if (_isResultValid)
                    {
                        return;
                    }
                    UpdateResultCore();
                    _isResultValid = true;
                }
            }

            public void InvalidateResult()
            {
                _isResultValid = false;
            }
            public SummaryCell Owner { get; private set; }
            public Axis Axis { get; private set; }
            public object Result
            {
                get
                {
                    UpdateResult();
                    return _result;
                }
            }
            public SummaryOperatorBase(SummaryCell owner, Axis axis)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                if (axis == null)
                {
                    throw new ArgumentNullException("axis");
                }
                Owner = owner;
                Axis = axis;
            }
        }

        public class ListOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return true;
            }

            protected override void UpdateResultCore()
            {
                List<string> l = new List<string>();
                foreach (object rec in Owner.Items)
                {
                    l.Add(Axis.GetValueText(rec));
                }
                _result = l;
            }
            public ListOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class SummaryOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IConvertible));
            }

            protected override void UpdateResultCore()
            {
                decimal v = 0;
                foreach (object rec in Owner.Items)
                {
                    try { v += ((IConvertible)Axis.GetValue(rec)).ToDecimal(null); }
                    catch (FormatException) { }
                    catch (InvalidCastException) { }
                    catch (OverflowException) { }
                }
                _result = v;
            }
            public SummaryOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class AverageOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IConvertible));
            }

            protected override void UpdateResultCore()
            {
                double v = 0;
                int n = 0;
                foreach (object rec in Owner.Items)
                {
                    try { v += ((IConvertible)Axis.GetValue(rec)).ToDouble(null); }
                    catch (FormatException) { }
                    catch (InvalidCastException) { }
                    catch (OverflowException) { }
                }
                if (n != 0)
                {
                    _result = v / n;
                }
                else if (v == 0)
                {
                    _result = 0;
                }
                else
                {
                    _result = double.NaN;
                }
            }
            public AverageOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class MinimumOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IComparable));
            }

            protected override void UpdateResultCore()
            {
                IComparable min = null;
                foreach (object rec in Owner.Items)
                {
                    IComparable v = Axis.GetValue(rec) as IComparable;
                    if (v == null)
                    {
                        continue;
                    }
                    if (min == null || 0 < min.CompareTo(v))
                    {
                        min = v;
                    }
                }
                _result = min;
            }
            public MinimumOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class MaximumOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IComparable));
            }

            protected override void UpdateResultCore()
            {
                IComparable max = null;
                foreach (object rec in Owner.Items)
                {
                    IComparable v = Axis.GetValue(rec) as IComparable;
                    if (v == null)
                    {
                        continue;
                    }
                    if (max == null || max.CompareTo(v) < 0)
                    {
                        max = v;
                    }
                }
                _result = max;
            }
            public MaximumOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class CountOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IConvertible));
            }

            protected override void UpdateResultCore()
            {
                int n = 0;
                foreach (object rec in Owner.Items)
                {
                    object v = Axis.GetValue(rec);
                    if (v == null || v is DBNull)
                    {
                        continue;
                    }
                    n++;
                }
                _result = n;
            }

            public CountOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class StdDevOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IConvertible));
            }

            protected override void UpdateResultCore()
            {
                double sum = 0;
                double sum2 = 0;
                long count = 0;
                foreach (object rec in Owner.Items)
                {
                    try
                    {
                        double v = ((IConvertible)Axis.GetValue(rec)).ToDouble(null);
                        sum += v;
                        sum2 += v * v;
                        count++;
                    }
                    catch (FormatException) { }
                    catch (InvalidCastException) { }
                    catch (OverflowException) { }
                }
                _result = (count != 0) ? (Math.Sqrt((sum2 / count) - (sum / count) * (sum / count))) : 0;
            }

            public StdDevOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class VarianceOperator : SummaryOperatorBase
        {
            public override bool CanApply(Type valueType)
            {
                return valueType.IsAssignableFrom(typeof(IConvertible));
            }

            protected override void UpdateResultCore()
            {
                double sum = 0;
                double sum2 = 0;
                long count = 0;
                foreach (object rec in Owner.Items)
                {
                    try
                    {
                        double v = ((IConvertible)Axis.GetValue(rec)).ToDouble(null);
                        sum += v;
                        sum2 += v * v;
                        count++;
                    }
                    catch (FormatException) { }
                    catch (InvalidCastException) { }
                    catch (OverflowException) { }
                }
                _result = (count != 0) ? ((sum2 / count) - (sum / count) * (sum / count)) : 0;
            }

            public VarianceOperator(SummaryCell owner, Axis axis) : base(owner, axis) { }
        }

        public class SummaryDefinition
        {
            private static readonly Type[] OperatorTypes = new Type[]
            {
                typeof(ListOperator),
                typeof(SummaryOperator),
                typeof(AverageOperator),
                typeof(MinimumOperator),
                typeof(MaximumOperator),
                typeof(CountOperator),
                typeof(StdDevOperator),
                typeof(VarianceOperator)
            };

            public Axis Axis { get; set; }
            public SummaryOperation Operation { get; set; }

            private static readonly Type[] SummaryOperatorConstructorArgTypes = new Type[] { typeof(SummaryCell), typeof(Axis) };
            public SummaryOperatorBase NewOperator(SummaryCell owner, Axis axis)
            {
                int i = (int)Operation;
                if (i < 0 || OperatorTypes.Length <= i)
                {
                    return null;
                }
                Type t = OperatorTypes[i];
                ConstructorInfo ctor = t.GetConstructor(SummaryOperatorConstructorArgTypes);
                if (ctor == null)
                {
                    return null;
                }
                SummaryOperatorBase op = (SummaryOperatorBase)ctor.Invoke(new object[] { owner, axis });
                return op;
            }
        }
        /// <summary>
        /// 集計グリッドに表示するセル
        /// </summary>
        public class SummaryCell
        {
            public class ItemCollection : ObservableCollection<object> { }

            public ItemCollection Items { get; private set; }
            public AxisValueArray KeyAxis { get; internal set; }
            public SummaryOperatorBase[] Summaries { get; internal set; }
            

            public SummaryCell(IList<Axis> axises, AxisValueArray key, IList<SummaryDefinition> summaryDefinitions)
            {
                Items = new ItemCollection();
                Items.CollectionChanged += Items_CollectionChanged;

                KeyAxis = key;
                Summaries = new SummaryOperatorBase[summaryDefinitions.Count];
                for (int i = 0; i < Summaries.Length; i++)
                {
                    Summaries[i] = summaryDefinitions[i].NewOperator(this, axises[i]);
                }
            }

            private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                InvalidateSummaryResult();
            }

            public void Add(object record)
            {
                Items.Add(record);
            }

            private void InvalidateSummaryResult()
            {
                foreach (SummaryOperatorBase op in Summaries)
                {
                    op.InvalidateResult();
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SummaryCell))
                {
                    return false;
                }
                SummaryCell rec = (SummaryCell)obj;
                return Equals(KeyAxis, rec.KeyAxis);
            }
            public override int GetHashCode()
            {
                return KeyAxis.GetHashCode();
            }
        }

        public class SummaryCellCollection: ObservableCollection<SummaryCell> { }
    }
}
