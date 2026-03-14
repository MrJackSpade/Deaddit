using System.Net;

namespace Deaddit.Core.Reddit.Exceptions
{
    internal class EntityNotFoundException : RemoteException
    {
        public EntityNotFoundException(string url, string content) : base(url, content, HttpStatusCode.NotFound)
        {
        }
    }
}