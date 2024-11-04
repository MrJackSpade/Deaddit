using Reddit.Api.Models.Api;

namespace Deaddit.Core.Interfaces
{
    public interface IAggregatePostHandler
    {
        IAggregateUrlHandler UrlHandler { get; }

        bool CanDownload(ApiPost post);

        bool CanLaunch(ApiPost post);

        bool CanShare(ApiPost post);

        Task Download(ApiPost post);

        Task Launch(ApiPost post);

        Task Share(ApiPost post);
    }
}