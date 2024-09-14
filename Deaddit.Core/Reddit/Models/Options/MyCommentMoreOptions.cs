using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Options
{
    public enum MyCommentMoreOptions
    {
        [EnumMember(Value = "Delete")]
        Delete,

        [EnumMember(Value = "Disable Replies")]
        ToggleReplies,

        [EnumMember(Value = "Edit")]
        Edit
    }
}