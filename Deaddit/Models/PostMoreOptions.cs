using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Models
{
    internal enum PostMoreOptions
    {
        [EnumMember(Value = "View Subreddit")]
        ViewSubreddit,

        [EnumMember(Value = "View Author")]
        ViewAuthor,

        [EnumMember(Value = "Block Author")]
        BlockAuthor,

        [EnumMember(Value = "Block Subreddit")]
        BlockSubreddit,

        [EnumMember(Value = "Block Flair")]
        BlockFlair,

        [EnumMember(Value = "Block Domain")]
        BlockDomain
    }
}
