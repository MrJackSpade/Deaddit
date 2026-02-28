using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class MoreCommentsResponseMeta
    {
        [NotNull]
        [JsonPropertyName("data")]
        public MoreCommentsData Data { get; init; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; init; } = [];
    }
}
