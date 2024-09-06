using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public enum CollapsedReasonKind
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "DELETED")]
        Deleted = 1,

        [EnumMember(Value = "LOW_SCORE")]
        LowScore = 2
    }
}