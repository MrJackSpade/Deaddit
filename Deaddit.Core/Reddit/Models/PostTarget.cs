namespace Deaddit.Core.Reddit.Models
{
    public class PostTarget(PostTargetKind kind, string[] uri, string[] shareUrls)
    {
        public PostTarget(PostTargetKind post) : this(post, [], [])
        {

        }

        public PostTarget(PostTargetKind kind, string[] uri) : this(kind, uri, uri)
        {
        }

        public PostTarget(PostTargetKind kind, string uri) : this(kind, [uri])
        {
        }

        public PostTarget(PostTargetKind kind, string uri, string shareurl) : this(kind, [uri], [shareurl])
        {
        }

        public PostTargetKind Kind { get; set; } = kind;

        public string[] LaunchUrls { get; set; } = uri;

        public string[] ShareUrls { get; set; } = shareUrls;
    }
}