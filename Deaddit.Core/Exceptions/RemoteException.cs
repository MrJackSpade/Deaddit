using System.Net;

namespace Deaddit.Core.Exceptions
{
    public class RemoteException(string url, string content, HttpStatusCode httpStatusCode) : Exception(content)
    {
        public string Url { get; } = url;
        public HttpStatusCode HttpStatusCode { get; } = httpStatusCode;
    }
}