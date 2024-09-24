using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Models
{
    public class CollapsedMore : ApiThing, IMore
    {
        public List<string?> ChildNames { get; }

        public CollapsedReasonKind CollapsedReasonCode { get; }

        public int? Count { get; }

        public CollapsedMore(IEnumerable<ApiComment> comments)
        {
            List<ApiComment> commentsList = comments.ToList();

            ChildNames = commentsList.Select(c => c.Id).ToList();
            Count = commentsList.Count;
            Parent = comments.Select(c => c.Parent).Distinct().Single();
            CollapsedReasonCode = commentsList.Select(c => c.CollapsedReasonCode).Distinct().Single();
        }
    }
}