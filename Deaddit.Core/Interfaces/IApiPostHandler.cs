using Reddit.Api.Models.Api;

namespace Deaddit.Core.Interfaces
{
    public interface IApiPostHandler
    {
        /// <summary>
        /// Returns true if the post can be downloaded
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns></returns>
        bool CanDownload(ApiPost apiPost, IAggregatePostHandler caller);

        /// <summary>
        /// Returns true if the handler can handle the post
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns></returns>
        bool CanLaunch(ApiPost apiPost, IAggregatePostHandler caller);

        /// <summary>
        /// Returns true if the post content can be shared
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns></returns>
        bool CanShare(ApiPost apiPost, IAggregatePostHandler caller);

        /// <summary>
        /// Gets the Urls required to download a post
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns></returns>
        Task Download(ApiPost apiPost, IAggregatePostHandler caller);

        /// <summary>
        /// Attempts to handle a post
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns>True if handled</returns>
        Task Launch(ApiPost apiPost, IAggregatePostHandler caller);

        /// <summary>
        /// Attempts to share the post content
        /// </summary>
        /// <param name="apiPost"></param>
        /// <returns></returns>
        Task Share(ApiPost apiPost, IAggregatePostHandler caller);
    }
}