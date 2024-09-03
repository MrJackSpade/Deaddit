using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models.Options
{
    internal enum CommentMoreOptions
    {
        [EnumMember(Value = "View Author")]
        ViewAuthor,

        [EnumMember(Value = "Block Author")]
        BlockAuthor
    }
}