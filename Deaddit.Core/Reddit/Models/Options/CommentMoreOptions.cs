using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Options
{
    public enum CommentMoreOptions
    {
        [EnumMember(Value = "View Author")]
        ViewAuthor,

        [EnumMember(Value = "Block Author")]
        BlockAuthor
    }
}