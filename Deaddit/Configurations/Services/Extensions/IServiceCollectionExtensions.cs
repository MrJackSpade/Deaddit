using Deaddit.Configurations.Interfaces;

namespace Deaddit.Configurations.Services.Extensions
{
    internal static class IServiceCollectionExtensions
    {
        public static void AddConfiguration<T>(this IServiceCollection services) where T : class
        {
            services.AddSingleton(s => s.GetRequiredService<IConfigurationService>().Read<T>());
        }
    }
}
