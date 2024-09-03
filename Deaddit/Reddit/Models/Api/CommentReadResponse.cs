using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class CommentReadResponse
    {
        [JsonPropertyName("data")]
        public ReadMetaData<RedditCommentMeta>? Data { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
    }
}