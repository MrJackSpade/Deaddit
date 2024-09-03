using CommunityToolkit.Maui;
using Deaddit.Configurations;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Deaddit.Services;
using Deaddit.Services.Configuration;
using Deaddit.Services.Configuration.Extensions;
using Microsoft.Extensions.Logging;
using AppTheme = Deaddit.Configurations.AppTheme;

namespace Deaddit
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();

            builder.Services.AddTransient<IRedditClient>(s =>
            {
                IAppCredentials appTheme = s.GetRequiredService<IAppCredentials>();
                IJsonClient jsonClient = s.GetRequiredService<IJsonClient>();
                HttpClient httpClient = s.GetRequiredService<HttpClient>();

                return new RedditClient(appTheme, jsonClient, httpClient);
            });

            builder.Services.AddConfiguration<IAppTheme, AppTheme>();
            builder.Services.AddConfiguration<IAppCredentials, AppCredentials>();
            builder.Services.AddConfiguration<IBlockConfiguration, BlockConfiguration>();

            builder.Services.AddTransient<IJsonClient, JsonClient>();
            builder.Services.AddTransient<IConfigurationService, ConfigurationService>();
            builder.Services.AddTransient<IMarkDownService, MarkdownService>();
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