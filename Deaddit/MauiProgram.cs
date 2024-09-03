using CommunityToolkit.Maui;
using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Configurations.Services;
using Deaddit.Configurations.Services.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit;
using Deaddit.Reddit.Interfaces;
using Deaddit.Services;
using Microsoft.Extensions.Logging;
using ApplicationTheme = Deaddit.Configurations.Models.ApplicationTheme;

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

            builder.Services.AddConfiguration<ApplicationTheme>();
            builder.Services.AddConfiguration<RedditCredentials>();
            builder.Services.AddConfiguration<BlockConfiguration>();

            builder.Services.AddTransient<IJsonClient, JsonClient>();
            builder.Services.AddSingleton<IVisitTracker, PreferencesVisitTracker>();
            builder.Services.AddTransient<IConfigurationService, PreferencesConfigurationService>();
            builder.Services.AddSingleton(s => new HttpClient());
            builder.Services.AddSingleton<LandingPage>();

            builder
                .UseMauiApp<App>()
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