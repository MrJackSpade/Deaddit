using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models.Options
{
    internal enum PostMoreOptions
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