using Deaddit.Configurations.Interfaces;
using Deaddit.Json.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Configurations.Services
{
    internal class PreferencesConfigurationService : IConfigurationService
    {
        private readonly JsonSerializerOptions _options;

        public PreferencesConfigurationService()
        {
            _options = new JsonSerializerOptions();

            _options.Converters.Add(new ColorJsonConverter());
            _options.Converters.Add(new JsonStringEnumConverter());
        }

        public T Read<T>(string name) where T : class
        {
            string json = Preferences.Get(name, "");

            if (string.IsNullOrWhiteSpace(json))
            {
                return Activator.CreateInstance<T>();
            }
            else
            {
                return JsonSerializer.Deserialize<T>(json, _options)!;
            }
        }

        public T Read<T>() where T : class
        {
            return this.Read<T>(typeof(T).FullName!);
        }

        public void Write<T>(string name, T obj) where T : class
        {
            string json = JsonSerializer.Serialize(obj, _options);

            Preferences.Set(name, json);
        }

        public void Write<T>(T obj) where T : class
        {
            this.Write(typeof(T).FullName!, obj);
        }
    }
}