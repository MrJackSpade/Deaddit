using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
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