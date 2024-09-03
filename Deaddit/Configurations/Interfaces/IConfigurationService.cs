namespace Deaddit.Configurations.Interfaces
{
    public interface IConfigurationService
    {
        T Read<T>() where T : class;

        T Read<T>(string name) where T : class;

        void Write<T>(T obj) where T : class;

        void Write<T>(string name, T obj) where T : class;
    }
}