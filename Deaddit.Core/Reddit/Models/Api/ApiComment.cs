using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiComment : ApiThing
    {
        [JsonPropertyName("after")]
        public string? After { get; init; }

        [JsonPropertyName("associated_award")]
        public object? AssociatedAward { get; init; }

        [JsonPropertyName("before")]
        public string? Before { get; init; }

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

        [JsonPropertyName("depth")]
        public int? Depth { get; init; }

        [JsonPropertyName("dist")]
        public long? Dist { get; init; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("over_18")]
        public bool? IsNsfw { get; init; }

        [JsonPropertyName("is_submitter")]
        public bool? IsSubmitter { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("link_author")]
        public string? Linkauthor { get; init; }

        [JsonPropertyName("link_id")]
        public string? LinkId { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("link_permalink")]
        public string? LinkPermalink { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("link_title")]
        public string? LinkTitle { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("link_url")]
        public string? LinkUrl { get; init; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        [JsonPropertyName("quarantine")]
        public bool? Quarantine { get; init; }

        [JsonPropertyName("replies")]
        public ApiThingCollection? Replies { get; set; }

        [JsonPropertyName("rte_mode")]
        public RteMode RteMode { get; set; }

        [JsonPropertyName("score_hidden")]
        public bool? ScoreHidden { get; init; }

        [JsonPropertyName("unrepliable_reason")]
        public UnrepliableReason UnrepliableReason { get; init; }

        public override string ToString()
        {
            return $"[{Author}] {Body}";
        }
    }
}