using Deaddit.Reddit.Models.Api;

namespace Deaddit.Reddit.Interfaces
{
    public interface IVisitTracker
    {
        bool HasVisited(RedditThing thing);

        void Visit(RedditThing thing);
    }
}
