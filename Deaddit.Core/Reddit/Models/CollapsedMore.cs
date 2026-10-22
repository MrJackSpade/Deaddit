using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models
{
    public class CollapsedMore : ApiThing, IMore
    {
        public CollapsedMore(IEnumerable<ApiComment> comments)
        {
            Comments = comments.ToList();

            ChildNames = Comments.Select(c => c.Id).ToList();
            Count = Comments.Count;
            Parent = Comments.Select(c => c.Parent).Distinct().Single();
            CollapsedReasonCode = Comments.Select(c => c.CollapsedReasonCode).Distinct().Single();
        }

        public List<string?> ChildNames { get; }

        /// <summary>
        /// The collapsed comments themselves, already loaded with their replies, so expanding
        /// the group renders them directly instead of fetching them again.
        /// </summary>
        public List<ApiComment> Comments { get; }

        public CollapsedReasonCode CollapsedReasonCode { get; }

        public int? Count { get; }
    }
}