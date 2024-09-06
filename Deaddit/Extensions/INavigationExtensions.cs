using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Pages;

namespace Deaddit.Extensions
{
    public static class INavigationExtensions
    {
        public static async Task OpenPost(this INavigation navigation,
                                          ApiPost post,
                                          IRedditClient redditClient,
                                          ApplicationStyling applicationTheme,
                                          ApplicationHacks applicationHacks,
                                          IVisitTracker visitTracker,
                                          BlockConfiguration blockConfiguration,
                                          IConfigurationService configurationService)
        {
            PostTarget? resource = post.GetPostTarget();

            await navigation.OpenResource(resource, redditClient, applicationTheme, applicationHacks, visitTracker, blockConfiguration, configurationService, post);
        }

        public static async Task OpenResource(this INavigation navigation,
                                              PostTarget resource,
                                              IRedditClient redditClient,
                                              ApplicationStyling applicationTheme,
                                              ApplicationHacks applicationHacks,
                                              IVisitTracker visitTracker,
                                              BlockConfiguration blockConfiguration,
                                              IConfigurationService configurationService,
                                              ApiPost? post = null)
        {
            //This needs to be handled differently. There's too many dependencies.
            switch (resource.Kind)
            {
                case PostTargetKind.Post:
                    if (post is null)
                    {
                        throw new NotImplementedException();
                    }

                    PostPage postPage = new(post, redditClient, applicationTheme, applicationHacks, visitTracker, blockConfiguration, configurationService);
                    await navigation.PushAsync(postPage);
                    await postPage.TryLoad();
                    break;

                case PostTargetKind.Url:
                    Ensure.NotNullOrEmpty(resource.LaunchUrls);
                    await navigation.PushAsync(new EmbeddedBrowser(resource.LaunchUrls[0], applicationTheme));
                    break;

                case PostTargetKind.Video:
                    Ensure.NotNullOrEmpty(resource.LaunchUrls);
                    await navigation.PushAsync(new EmbeddedVideo(resource.LaunchUrls[0], applicationTheme));
                    break;

                case PostTargetKind.Image:
                    Ensure.NotNullOrEmpty(resource.LaunchUrls);
                    await navigation.PushAsync(new EmbeddedImage(applicationTheme, resource.LaunchUrls));
                    break;

                default:
                    throw new EnumNotImplementedException(resource.Kind);
            }
        }
    }
}