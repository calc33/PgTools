namespace Db2Source
{
    public class SortOrderItem
    {
        public SortOrder Value { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public SortOrderItem() { Value = SortOrder.Asc; }
        public SortOrderItem(SortOrder value, string name, string tooltip)
        {
            Value = value;
            Text = name;
            ToolTip = tooltip;
        }
        public override string ToString()
        {
            return Text;
        }
        public override bool Equals(object obj)
        {
            if (obj is SortOrderItem)
            {
                return Value == ((SortOrderItem)obj).Value;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
