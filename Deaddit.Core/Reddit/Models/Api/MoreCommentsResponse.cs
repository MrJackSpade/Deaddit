using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class MoreCommentsData
    {
        [JsonPropertyName("things")]
        public List<ApiCommentMeta> Things { get; init; } = [];
    }

    public class MoreCommentsResponse
    {
        [NotNull]
        [JsonPropertyName("json")]
        public MoreCommentsResponseMeta Json { get; init; }
    }

    public class MoreCommentsResponseMeta
    {
        [NotNull]
        [JsonPropertyName("data")]
        public MoreCommentsData Data { get; init; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; init; } = [];
    }
}