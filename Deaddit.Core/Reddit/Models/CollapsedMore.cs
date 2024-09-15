using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Models
{
    public class CollapsedMore : ApiThing, IMore
    {
        public CollapsedMore(ApiComment comment)
        {
            ChildNames = [comment.Id];
            Count = 1;
            Parent = comment.Parent;
            CollapsedReasonCode = comment.CollapsedReasonCode;
        }

        public CollapsedReasonKind CollapsedReasonCode { get; }

        public List<string> ChildNames { get; }

        public int? Count { get; }

        public ApiThing Parent { get; }
    }
}