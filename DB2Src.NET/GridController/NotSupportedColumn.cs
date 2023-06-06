namespace Db2Source
{
    public sealed class NotSupportedColumn
    {
        public static readonly NotSupportedColumn Value = new NotSupportedColumn();
        public override string ToString()
        {
            return "(NOT SUPPORTED)";
        }
    }
}
