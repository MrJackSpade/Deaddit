using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Options
{
    public enum PostMoreOptions
    {
        [EnumMember(Value = "View Subreddit")]
        ViewSubreddit,

        [EnumMember(Value = "View Author")]
        ViewAuthor,

        [EnumMember(Value = "Block Subreddit")]
        BlockSubreddit,

        [EnumMember(Value = "Block Author")]
        BlockAuthor,

        [EnumMember(Value = "Block Flair")]
        BlockFlair,

        [EnumMember(Value = "Block Domain")]
        BlockDomain
    }
}