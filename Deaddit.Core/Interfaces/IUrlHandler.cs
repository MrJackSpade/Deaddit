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
        /// Attempts to handle a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns>True if handled</returns>
        Task Launch(string url, IAggregatePostHandler aggregatePostHandler);
    }
}