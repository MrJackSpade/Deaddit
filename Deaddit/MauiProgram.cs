using CommunityToolkit.Maui;
using Deaddit.Configurations.Services;
using Deaddit.Configurations.Services.Extensions;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.Handlers.Post;
using Deaddit.Handlers.Url;
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

            builder.Services.AddSingleton<IRedditClient>(s =>
            {
                RedditCredentials ApplicationTheme = s.GetRequiredService<RedditCredentials>();
                IJsonClient jsonClient = s.GetRequiredService<IJsonClient>();
                HttpClient httpClient = s.GetRequiredService<HttpClient>();
                IDisplayExceptions displayExceptions = s.GetRequiredService<IDisplayExceptions>();

                return new RedditClient(ApplicationTheme, jsonClient, displayExceptions, httpClient);
            });

            // Register individual handlers as transient
            builder.Services.AddTransient<IUrlHandler, RedditCommentsPostUrlHandler>();
            builder.Services.AddTransient<IUrlHandler, RedditSPostUrlHandler>();
            builder.Services.AddTransient<IUrlHandler, SubredditUrlHandler>();
            builder.Services.AddTransient<IUrlHandler, UserUrlHandler>();
            builder.Services.AddTransient<IUrlHandler, GenericImageHandler>();
            builder.Services.AddTransient<IUrlHandler, GenericVideoHandler>();
            builder.Services.AddTransient<IUrlHandler, GenericUrlHandler>();

            // Register the aggregate handler that takes IEnumerable<IUrlHandler>
            builder.Services.AddTransient<IAggregateUrlHandler, AggregateUrlHandler>();

            // Register individual API post handlers as transient
            builder.Services.AddTransient<IApiPostHandler, SelfPostHandler>();
            builder.Services.AddTransient<IApiPostHandler, RedditGalleryHandler>();
            builder.Services.AddTransient<IApiPostHandler, RedditVideoHandler>();
            builder.Services.AddTransient<IApiPostHandler, CrossPostHandler>();

            // Register the aggregate API post handler that takes IEnumerable<IApiPostHandler>
            builder.Services.AddTransient<IAggregatePostHandler, AggregatePostHandler>();

            builder.Services.AddConfiguration<ApplicationStyling>();
            builder.Services.AddConfiguration<RedditCredentials>();
            builder.Services.AddConfiguration<BlockConfiguration>();
            builder.Services.AddConfiguration<ApplicationHacks>();
            builder.Services.AddSingleton<IDisplayExceptions, MauiExceptionDisplay>();
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