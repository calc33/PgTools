namespace Db2Source
{
    public interface IRegistryStore
    {
        void LoadFromRegistry();
        void SaveToRegistry();
    }
}
