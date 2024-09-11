using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Models
{
    public class UserPartial
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("created_utc")]
        public DateTime CreatedUtc { get; set; }

        [JsonPropertyName("link_karma")]
        public int LinkKarma { get; set; }

        [JsonPropertyName("comment_karma")]
        public int CommentKarma { get; set; }

        [JsonPropertyName("profile_img")]
        public string? ProfileImg { get; set; }

        [JsonPropertyName("profile_color")]
        public string? ProfileColor { get; set; }

        [JsonPropertyName("profile_over_18")]
        public bool ProfileOver18 { get; set; }
    }
}
