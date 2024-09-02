using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class CommentReadResponse
    {
        [JsonPropertyName("data")]
        public ReadMetaData<RedditCommentMeta>? Data { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
    }
}