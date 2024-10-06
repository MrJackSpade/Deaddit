using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Options
{
    public enum MessageMoreOptions
    {
        [EnumMember(Value = "View Author")]
        ViewAuthor,

        [EnumMember(Value = "Block Author")]
        BlockAuthor
    }
}