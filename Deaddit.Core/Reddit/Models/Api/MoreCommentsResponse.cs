using System.Diagnostics.CodeAnalysis;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class MoreCommentsResponse
    {
        [NotNull]
        public MoreCommentsResponseMeta Json { get; init; }
    }
}