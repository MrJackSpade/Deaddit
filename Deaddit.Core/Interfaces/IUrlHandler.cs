using Deaddit.Core.Models;

namespace Deaddit.Core.Interfaces
{
    public interface IUrlHandler
    {
        /// <summary>
        /// Returns true if the url content can be downloaded as a file
        /// </summary>
        bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler);

        /// <summary>
        /// Returns true if the url can be rendered as an inline image
        /// </summary>
        bool CanInline(string url);

        /// <summary>
        /// Returns true if the url can be handled
        /// </summary>
        bool CanLaunch(string url, IAggregatePostHandler? aggregatePostHandler);

        /// <summary>
        /// Returns the downloadable content for the URL
        /// </summary>
        Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler);

        /// <summary>
        /// Returns the direct image URL for inline rendering
        /// </summary>
        string? GetInlineUrl(string url);

        /// <summary>
        /// Attempts to handle a URL
        /// </summary>
        Task Launch(string url, IAggregatePostHandler aggregatePostHandler);
    }
}