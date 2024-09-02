using System.Runtime.Serialization;

namespace Deaddit.Models.Json.Response
{
    public enum CollapsedReasonKind
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "DELETED")]
        Deleted = 1,

        [EnumMember(Value = "comment score below threshold")]
        CommentScoreBelowThreshold = 2,

        [EnumMember(Value = "LOW_SCORE")]
        LowScore = 2
    }
}