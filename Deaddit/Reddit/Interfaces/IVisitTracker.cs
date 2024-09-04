using Deaddit.Reddit.Models.Api;

namespace Deaddit.Reddit.Interfaces
{
    public interface IVisitTracker
    {
        bool HasVisited(ApiThing thing);

        void Visit(ApiThing thing);
    }
}