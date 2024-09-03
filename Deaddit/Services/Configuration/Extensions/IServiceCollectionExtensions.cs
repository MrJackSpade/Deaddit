using Deaddit.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Services.Configuration.Extensions
{
    internal static class IServiceCollectionExtensions
    {
        public static void AddConfiguration<T>(this IServiceCollection services) where T : class
        { 
            services.AddSingleton(s => s.GetRequiredService<IConfigurationService>().Read<T>());
        }

        public static void AddConfiguration<X, Y>(this IServiceCollection services) where Y : class, X where X : class
        {
            services.AddSingleton<X,Y>(s => s.GetRequiredService<IConfigurationService>().Read<Y>(typeof(X).FullName));
        }
    }
}
