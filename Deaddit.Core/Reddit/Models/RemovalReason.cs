using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum RemovalReason
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "legal")]
        Legal
    }
}