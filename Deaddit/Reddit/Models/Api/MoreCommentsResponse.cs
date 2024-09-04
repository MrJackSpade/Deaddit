using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class MoreCommentsData
    {
        [JsonPropertyName("things")]
        public List<RedditCommentMeta> Things { get; set; } = [];
    }

    public class MoreCommentsResponse
    {
        [JsonPropertyName("json")]
        public MoreCommentsResponseMeta? Json { get; set; }
    }

    public class MoreCommentsResponseMeta
    {
        [JsonPropertyName("data")]
        public MoreCommentsData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = [];
    }
}