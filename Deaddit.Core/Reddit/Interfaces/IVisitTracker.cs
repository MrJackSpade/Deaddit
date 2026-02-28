using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IVisitTracker
    {
        bool HasVisited(ApiThing thing);

        void Visit(ApiThing thing);
    }
}