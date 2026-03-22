using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiComment : ApiThing
    {
        public string? After { get; init; }

        public object? AssociatedAward { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? AssociatedAwardingId { get; set; }

        public string? Before { get; init; }

        public bool? Collapsed { get; init; }

        public object? CollapsedBecauseCrowdControl { get; init; }

        public string? CollapsedReason { get; init; }

        public CollapsedReasonCode CollapsedReasonCode { get; init; }

        public object? CommentType { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? Context { get; set; }

        public int? Controversiality { get; init; }

        public int? Depth { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? Dest { get; set; }

        public long? Dist { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? FirstMessage { get; set; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? FirstMessageName { get; set; }

        public string? GeoFilter { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public bool? IsNsfw { get; init; }

        public bool? IsSubmitter { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public string? Linkauthor { get; init; }

        public string? LinkId { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public string? LinkPermalink { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public string? LinkTitle { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public string? LinkUrl { get; init; }

        public string? ModHash { get; init; }

        /// <summary>
        /// Only used when viewing comment outside of post
        /// </summary>
        public bool? Quarantine { get; init; }

        public ApiThingCollection? Replies { get; set; }

        public RteMode RteMode { get; set; }

        public bool? ScoreHidden { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public bool? New { get; set; }

        public UnrepliableReason UnrepliableReason { get; init; }

        /// <summary>
        /// If inbox message, otherwise null
        /// </summary>
        public bool? WasComment { get; set; }

        public override string ToString()
        {
            return $"[{Author}] {Body}";
        }
    }
}