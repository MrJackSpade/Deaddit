namespace Deaddit.Reddit.Exceptions
{
    internal class RedditApiException : Exception
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