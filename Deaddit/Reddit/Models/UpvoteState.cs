using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models
{
    public enum UpvoteState
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "true")]
        Upvote,

        [EnumMember(Value = "false")]
        Downvote
    }
}