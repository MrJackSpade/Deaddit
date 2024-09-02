using Deaddit.Converters;
using Deaddit.Interfaces;
using System.Text.Json;

namespace Deaddit.Services
{
    internal class ConfigurationService : IConfigurationService
    {
        private readonly JsonSerializerOptions _options;

        public ConfigurationService()
        {
            _options = new JsonSerializerOptions();

            _options.Converters.Add(new ColorJsonConverter());
        }
        public T Read<T>() where T : class
        {
            string json = Preferences.Get(typeof(T).FullName, "");

            if (string.IsNullOrWhiteSpace(json))
            {
                return Activator.CreateInstance<T>();
            }
            else
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options)!;
            }
        }

        public void Write<T>(T obj) where T : class
        {
            string json = System.Text.Json.JsonSerializer.Serialize(obj, _options);

            Preferences.Set(typeof(T).FullName, json);
        }
    }
}