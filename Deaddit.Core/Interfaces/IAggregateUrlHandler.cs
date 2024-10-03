namespace Deaddit.Core.Interfaces
{
    public interface IAggregateUrlHandler
    {
        bool CanLaunch(string? url, IAggregatePostHandler? aggregatePostHandler);

        Task Launch(string? url, IAggregatePostHandler? aggregatePostHandler);
    }
}