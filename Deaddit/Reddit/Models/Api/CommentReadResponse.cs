using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class CommentReadResponse
    {
        [JsonPropertyName("data")]
        public ReadMetaData<ApiCommentMeta>? Data { get; set; }

        [JsonPropertyName("kind")]
        public ThingKind Kind { get; init; }
    }
}