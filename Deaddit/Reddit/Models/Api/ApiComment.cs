using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class ApiComment : ApiThing
    {
        [JsonPropertyName("after")]
        public string? After { get; init; }

        [JsonPropertyName("associated_award")]
        public object? AssociatedAward { get; init; }

        [JsonPropertyName("author_cakeday")]
        public bool? AuthorCakeDay { get; init; }

        [JsonPropertyName("before")]
        public string? Before { get; init; }

        [JsonPropertyName("children")]
        public List<string> ChildNames { get; init; } = [];

        [JsonPropertyName("collapsed")]
        public bool? Collapsed { get; init; }

        [JsonPropertyName("collapsed_because_crowd_control")]
        public object? CollapsedBecauseCrowdControl { get; init; }

        [JsonPropertyName("collapsed_reason")]
        public string? CollapsedReason { get; init; }

        [JsonPropertyName("collapsed_reason_code")]
        public CollapsedReasonKind CollapsedReasonCode { get; init; }

        [JsonPropertyName("comment_type")]
        public object? CommentType { get; init; }

        [JsonPropertyName("controversiality")]
        public int? Controversiality { get; init; }

        [JsonPropertyName("count")]
        public int? Count { get; init; }

        [JsonPropertyName("depth")]
        public int? Depth { get; init; }

        [JsonPropertyName("dist")]
        public long? Dist { get; init; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; init; }

        [JsonPropertyName("is_submitter")]
        public bool? IsSubmitter { get; init; }

        [JsonPropertyName("link_id")]
        public string? LinkId { get; init; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; init; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; init; }

        [JsonPropertyName("replies")]
        public CommentReadResponse? Replies { get; set; }

        [JsonPropertyName("score_hidden")]
        public bool? ScoreHidden { get; init; }

        [JsonPropertyName("unrepliable_reason")]
        public object? UnrepliableReason { get; init; }
    }
}