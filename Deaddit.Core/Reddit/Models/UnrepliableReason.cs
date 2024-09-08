using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum UnrepliableReason
    {
        [EnumMember(Value = null)]
        None = 0,

        [EnumMember(Value = "NEAR_BLOCKER")]
        NearBlocker = 1
    }
}
