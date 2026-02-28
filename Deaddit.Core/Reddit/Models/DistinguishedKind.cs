using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum DistinguishedKind
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "moderator")]
        Moderator,

        [EnumMember(Value = "admin")]
        Admin
    }
}
