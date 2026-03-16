using System.Diagnostics.CodeAnalysis;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class MoreCommentsResponseMeta
    {
        [NotNull]
        public MoreCommentsData Data { get; init; }

        public List<string> Errors { get; init; } = [];
    }
}