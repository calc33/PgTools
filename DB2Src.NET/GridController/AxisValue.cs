using System;

namespace Db2Source
{
    /// <summary>
    /// クロス集計の集計項目(Axis)の要素
    /// </summary>
    public class AxisValue
    {
        public Axis Owner { get; }
        public object Value { get; }
        public bool IsNoValue
        {
            get
            {
                return Value is Axis.NoAxisValue;
            }
        }
        public string Text { get; set; }
        //public IComparable Order { get; set; }
        internal int _index = -1;
        public int Index
        {
            get
            {
                Owner?.Items?.UpdateIndex();
                return _index;
            }
        }

        public AxisValue(Axis owner, object value)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
            Value = value;
            Text = Owner.ToText(value);
        }

        public AxisValue(Axis owner, object value, string text)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
            Value = value;
            Text = text;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AxisValue))
            {
                return false;
            }
            return Equals(Value, ((AxisValue)obj).Value);
        }
        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }
            return Value.GetHashCode();
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
