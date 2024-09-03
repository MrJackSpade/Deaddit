using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostCommentData
    {
        [JsonPropertyName("things")]
        public List<RedditCommentMeta> Things { get; set; } = [];
    }

    public class PostCommentResponse
    {
        [JsonPropertyName("json")]
        public PostCommentResponseMeta? Json { get; set; }
    }

    public class PostCommentResponseMeta
    {
        [JsonPropertyName("data")]
        public PostCommentData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = [];
    }
}