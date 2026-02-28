using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class UserPartial
    {
        [JsonPropertyName("comment_karma")]
        public int CommentKarma { get; set; }

        [JsonPropertyName("created_utc")]
        public DateTime CreatedUtc { get; set; }

        [JsonPropertyName("link_karma")]
        public int LinkKarma { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profile_color")]
        public string? ProfileColor { get; set; }

        [JsonPropertyName("profile_img")]
        public string? ProfileImg { get; set; }

        [JsonPropertyName("profile_over_18")]
        public bool ProfileOver18 { get; set; }
    }
}