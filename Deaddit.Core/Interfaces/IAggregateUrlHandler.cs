using Deaddit.Core.Models;

namespace Deaddit.Core.Interfaces
{
    public interface IAggregateUrlHandler
    {
        bool CanDownload(string? url, IAggregatePostHandler? aggregatePostHandler);

        bool CanLaunch(string? url, IAggregatePostHandler? aggregatePostHandler);

        Task<FileDownload> Download(string? url, IAggregatePostHandler? aggregatePostHandler);

        Task Launch(string? url, IAggregatePostHandler? aggregatePostHandler);
    }
}