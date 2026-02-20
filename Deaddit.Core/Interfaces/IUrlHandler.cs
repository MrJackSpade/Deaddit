using Deaddit.Core.Models;

namespace Deaddit.Core.Interfaces
{
    public interface IUrlHandler
    {
        /// <summary>
        /// Returns true if the url can be handled
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        bool CanLaunch(string url, IAggregatePostHandler? aggregatePostHandler);

        /// <summary>
        /// Returns true if the url content can be downloaded as a file
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler);

        /// <summary>
        /// Attempts to handle a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns>True if handled</returns>
        Task Launch(string url, IAggregatePostHandler aggregatePostHandler);

        /// <summary>
        /// Returns the downloadable content for the URL
        /// </summary>
        Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler);
    }
}