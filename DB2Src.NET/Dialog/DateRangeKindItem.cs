namespace Db2Source
{
    public class DateRangeKindItem
    {
        public string Text { get; set; }
        public bool UseSpan { get; set; }
        public bool UseRange { get; set; }
        public int? Value { get; set; }
        public DateUnit Unit { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
