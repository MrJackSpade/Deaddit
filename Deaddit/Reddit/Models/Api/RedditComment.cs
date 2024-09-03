﻿using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditComment : RedditThing
    {
        [JsonPropertyName("after")]
        public string? After { get; set; }

        [JsonPropertyName("associated_award")]
        public object? AssociatedAward { get; set; }

        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("children")]
        [Obsolete("This doesn't appear to be used", true)]
        public List<RedditCommentMeta> Children { get; set; } = [];

        [JsonPropertyName("collapsed")]
        public bool? Collapsed { get; set; }

        [JsonPropertyName("collapsed_because_crowd_control")]
        public object? CollapsedBecauseCrowdControl { get; set; }

        [JsonPropertyName("collapsed_reason")]
        public CollapsedReasonKind? CollapsedReason { get; set; }

        [JsonPropertyName("collapsed_reason_code")]
        public CollapsedReasonKind CollapsedReasonCode { get; set; }

        [JsonPropertyName("comment_type")]
        public object? CommentType { get; set; }

        [JsonPropertyName("controversiality")]
        public int? Controversiality { get; set; }

        [JsonPropertyName("depth")]
        public int? Depth { get; set; }

        [JsonPropertyName("dist")]
        public long? Dist { get; set; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; set; }

        [JsonPropertyName("is_submitter")]
        public bool? IsSubmitter { get; set; }

        [JsonPropertyName("link_id")]
        public string? LinkId { get; set; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; set; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        [JsonPropertyName("replies")]
        public CommentReadResponse? Replies { get; set; }

        [JsonPropertyName("score_hidden")]
        public bool? ScoreHidden { get; set; }

        [JsonPropertyName("unrepliable_reason")]
        public object? UnrepliableReason { get; set; }
    }
}