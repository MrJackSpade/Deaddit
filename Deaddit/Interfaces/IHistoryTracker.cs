using Reddit.Api.Models.Api;

namespace Deaddit.Interfaces
{
    public interface IHistoryTracker
    {
        void AddToHistory(ApiPost post, bool fromHistoryPage = false);

        IReadOnlyList<string> GetHistory();
    }
}
