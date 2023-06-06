using System.Windows;

namespace Db2Source
{
    public interface ISchemaObjectWpfControl : ISchemaObjectControl
    {
        DependencyObject Parent { get; }
    }
}
