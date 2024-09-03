using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models.Options
{
    internal enum MyCommentMoreOptions
    {
        [EnumMember(Value = "Delete")]
        Delete,

        [EnumMember(Value = "Disable Replies")]
        DisableReplies,

        [EnumMember(Value = "Enable Replies")]
        EnableReplies,
    }
}