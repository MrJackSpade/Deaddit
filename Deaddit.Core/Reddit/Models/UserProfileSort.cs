using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum UserProfileSort
    {
        [EnumMember(Value = null)]
        New = 1,

        [EnumMember(Value = "submitted")]
        Submitted = 2,

        [EnumMember(Value = "comments")]
        Comments = 3,
    }
}
