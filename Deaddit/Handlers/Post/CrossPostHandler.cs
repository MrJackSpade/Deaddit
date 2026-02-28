using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Validation;

namespace Deaddit.Handlers.Post
{
    internal class CrossPostHandler() : IApiPostHandler
    {
        public bool CanDownload(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanDownload(crossPost))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanLaunch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanLaunch(crossPost))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanShare(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanShare(crossPost))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task Download(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanDownload(crossPost))
                {
                    await caller.Download(crossPost);
                    return;
                }
            }
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanLaunch(crossPost))
                {
                    await caller.Launch(crossPost);
                    return;
                }
            }
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            foreach (ApiPost crossPost in apiPost.CrossPostParentList)
            {
                if (caller.CanShare(crossPost))
                {
                    await caller.Share(crossPost);
                    return;
                }
            }
        }
    }
}