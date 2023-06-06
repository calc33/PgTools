namespace Db2Source
{
    public sealed class OverflowColumn
    {
        public static readonly NotSupportedColumn Value = new NotSupportedColumn();
        public override string ToString()
        {
            return "(OVERFLOW)";
        }
    }
}
