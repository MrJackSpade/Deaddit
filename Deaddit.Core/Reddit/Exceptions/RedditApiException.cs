namespace Deaddit.Core.Reddit.Exceptions
{
    public class RedditApiException : Exception
    {
        public RedditApiException()
        {
        }

        public RedditApiException(string? message) : base(message)
        {
        }

        public RedditApiException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}