using Deaddit.Interfaces;
using Deaddit.Models;
using Deaddit.Models.Json.Response;
using Deaddit.Pages;
using Deaddit.Services.Configuration;

namespace Deaddit.Extensions
{
    internal static class INavigationExtensions
    {
        public static async Task OpenPost(this INavigation navigation, RedditPost post, IRedditClient redditClient, IAppTheme appTheme, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            Models.RedditResource? resource = post.GetResource();

            navigation.OpenResource(resource, redditClient, appTheme, markDownService, blockConfiguration, configurationService, post);
        }

        public static async Task OpenResource(this INavigation navigation, RedditResource resource, IRedditClient redditClient, IAppTheme appTheme, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService, RedditPost? post = null)
        {
            switch (resource.Kind)
            {
                case Models.RedditResourceKind.Post:
                    if (post is null)
                    {
                        throw new NotImplementedException();
                    }

                    await navigation.PushAsync(new PostPage(post, redditClient, appTheme, markDownService, blockConfiguration, configurationService));
                    break;

                case Models.RedditResourceKind.Url:
                    await navigation.PushAsync(new EmbeddedBrowser(resource.Uri, appTheme));
                    break;

                case Models.RedditResourceKind.Video:
                    await navigation.PushAsync(new EmbeddedVideo(resource.Uri, appTheme));
                    break;

                case Models.RedditResourceKind.Image:
                    await navigation.PushAsync(new EmbeddedImage(resource.Uri, appTheme));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}