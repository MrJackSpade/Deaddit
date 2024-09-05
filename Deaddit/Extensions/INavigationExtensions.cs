using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.Extensions
{
    internal static class INavigationExtensions
    {
        public static async Task OpenPost(this INavigation navigation, ApiPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            PostTarget? resource = post.GetResource();

            await navigation.OpenResource(resource, redditClient, applicationTheme, visitTracker, blockConfiguration, configurationService, post);
        }

        public static async Task OpenResource(this INavigation navigation, PostTarget resource, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService, ApiPost? post = null)
        {
            //This needs to be handled differently. There's too many dependencies.
            switch (resource.Kind)
            {
                case PostTargetKind.Post:
                    if (post is null)
                    {
                        throw new NotImplementedException();
                    }

                    PostPage postPage = new(post, redditClient, applicationTheme, visitTracker, blockConfiguration, configurationService);
                    await navigation.PushAsync(postPage);
                    await postPage.TryLoad();
                    break;

                case PostTargetKind.Url:
                    Ensure.NotNullOrEmpty(resource.Uris);
                    await navigation.PushAsync(new EmbeddedBrowser(resource.Uris[0], applicationTheme));
                    break;

                case PostTargetKind.Video:
                    Ensure.NotNullOrEmpty(resource.Uris);
                    await navigation.PushAsync(new EmbeddedVideo(resource.Uris[0], applicationTheme));
                    break;

                case PostTargetKind.Image:
                    Ensure.NotNullOrEmpty(resource.Uris);
                    await navigation.PushAsync(new EmbeddedImage(applicationTheme, resource.Uris));
                    break;

                default:
                    throw new EnumNotImplementedException(resource.Kind);
            }
        }
    }
}