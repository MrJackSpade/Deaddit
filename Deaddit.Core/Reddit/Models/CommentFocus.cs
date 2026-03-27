using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Models
{
    public class CommentFocus
    {
        /// <summary>
        /// Maximum value for context supported by the Reddit API.
        /// </summary>
        public const int MAX_CONTEXT = 8;

        public CommentFocus(ApiComment comment, int context = MAX_CONTEXT)
        {
            Comment = comment;
            Context = context;
        }

        public ApiComment Comment { get; }

        public int Context { get; }
    }
}
