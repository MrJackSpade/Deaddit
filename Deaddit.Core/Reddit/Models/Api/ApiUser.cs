using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiUser : ApiThing
    {
        [JsonPropertyName("accept_chats")]
        public bool AcceptChats { get; set; }

        [JsonPropertyName("accept_followers")]
        public bool AcceptFollowers { get; set; }

        [JsonPropertyName("accept_pms")]
        public bool AcceptPms { get; set; }

        [JsonPropertyName("awardee_karma")]
        public int AwardeeKarma { get; set; }

        [JsonPropertyName("awarder_karma")]
        public int AwarderKarma { get; set; }

        [JsonPropertyName("comment_karma")]
        public int CommentKarma { get; set; }

        [JsonPropertyName("has_subscribed")]
        public bool HasSubscribed { get; set; }

        [JsonPropertyName("has_verified_email")]
        public bool HasVerifiedEmail { get; set; }

        [JsonPropertyName("hide_from_robots")]
        public bool HideFromRobots { get; set; }

        [JsonPropertyName("icon_img")]
        public string? IconImg { get; set; }

        [JsonPropertyName("is_blocked")]
        public bool IsBlocked { get; set; }

        [JsonPropertyName("is_employee")]
        public bool IsEmployee { get; set; }

        [JsonPropertyName("is_friend")]
        public bool IsFriend { get; set; }

        [JsonPropertyName("is_gold")]
        public bool IsGold { get; set; }

        [JsonPropertyName("is_mod")]
        public bool IsMod { get; set; }

        [JsonPropertyName("link_karma")]
        public int LinkKarma { get; set; }

        [JsonPropertyName("pref_show_snoovatar")]
        public bool PrefShowSnoovatar { get; set; }

        [JsonPropertyName("snoovatar_img")]
        public string? SnoovatarImg { get; set; }

        [JsonPropertyName("snoovatar_size")]
        public List<int> SnoovatarSize { get; set; } = [];

        [JsonPropertyName("subreddit")]
        public ApiSubReddit? SubReddit { get; init; }

        [JsonPropertyName("total_karma")]
        public int TotalKarma { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }
}