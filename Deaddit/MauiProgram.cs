using CommunityToolkit.Maui;
using Deaddit.Configurations.Services;
using Deaddit.Configurations.Services.Extensions;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Deaddit.Utils;
using Microsoft.Extensions.Logging;

namespace Deaddit
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();

            builder.Services.AddTransient<IRedditClient>(s =>
            {
                RedditCredentials ApplicationTheme = s.GetRequiredService<RedditCredentials>();
                IJsonClient jsonClient = s.GetRequiredService<IJsonClient>();
                HttpClient httpClient = s.GetRequiredService<HttpClient>();

                return new RedditClient(ApplicationTheme, jsonClient, httpClient);
            });

            builder.Services.AddConfiguration<ApplicationStyling>();
            builder.Services.AddConfiguration<RedditCredentials>();
            builder.Services.AddConfiguration<BlockConfiguration>();
            builder.Services.AddConfiguration<ApplicationHacks>();
            builder.Services.AddSingleton<IAppNavigator, AppNavigator>();
            builder.Services.AddTransient<IJsonClient, JsonClient>();
            builder.Services.AddSingleton<IVisitTracker, PreferencesVisitTracker>();
            builder.Services.AddTransient<IConfigurationService, PreferencesConfigurationService>();
            builder.Services.AddSingleton(s => new HttpClient());
            builder.Services.AddSingleton<LandingPage>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}