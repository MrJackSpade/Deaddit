namespace Deaddit.Core.Reddit.Exceptions
{
    public class InvalidApiResponseException : RedditApiException
    {
        public InvalidApiResponseException()
        {
        }

        public InvalidApiResponseException(string? message) : base(message)
        {
        }

        public InvalidApiResponseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}