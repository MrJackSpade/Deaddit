using Deaddit.Core.Interfaces;
using Reddit.Api.Interfaces;

namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// Extension of RedditService that adds IDisplayMessages integration
    /// for user-friendly exception handling.
    /// </summary>
    public class DeadditClient : RedditService, IRedditClient
    {
        private readonly IDisplayMessages _displayMessages;

        public DeadditClient(
            IDisplayMessages displayMessages,
            IRedditCredentials credentials,
            HttpClient httpClient)
            : base(credentials, httpClient)
        {
            _displayMessages = displayMessages;
        }

        public override async Task<bool> DisplayException(Exception ex)
        {
            return await _displayMessages.DisplayException(ex);
        }
    }
}