using System.Net;

namespace Deaddit.Core.Reddit.Exceptions
{
    internal class TooManyRequestsException : RemoteException
    {
        public TooManyRequestsException(string url, string content) : base(url, content, HttpStatusCode.TooManyRequests)
        {
        }
    }
}
