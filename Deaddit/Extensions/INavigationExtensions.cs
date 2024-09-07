using Deaddit.Core.Exceptions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Interfaces;
using Deaddit.Pages;

namespace Deaddit.Extensions
{
    public static class INavigationExtensions
    {
        [Obsolete("remove this")]
        public static async Task OpenPost(this INavigation navigation,
                                          ApiPost post,
                                          IAppNavigator appNavigator)
        {
            PostItems? resource = post.GetPostItems();

            await navigation.OpenResource(resource, appNavigator, post);
        }

        [Obsolete("remove this")]
        public static async Task OpenResource(this INavigation navigation, PostItems resource, IAppNavigator appNavigator, ApiPost? post = null)
        {
            //This needs to be handled differently. There's too many dependencies.
            switch (resource.Kind)
            {
                case PostTargetKind.Post:
                    if (post is null)
                    {
                        throw new NotImplementedException();
                    }

                    PostPage postPage = await appNavigator.OpenPost(post);
                    break;

                case PostTargetKind.Url:
                    Ensure.NotNullOrEmpty(resource.Items);
                    await appNavigator.OpenBrowser(resource);
                    break;

                case PostTargetKind.Video:
                    Ensure.NotNullOrEmpty(resource.Items);
                    await appNavigator.OpenVideo(resource);
                    break;

                case PostTargetKind.Image:
                    Ensure.NotNullOrEmpty(resource.Items);
                    await appNavigator.OpenImage(resource);
                    break;

                default:
                    throw new EnumNotImplementedException(resource.Kind);
            }
        }
    }
}