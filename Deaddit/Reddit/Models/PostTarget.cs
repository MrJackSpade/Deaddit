namespace Deaddit.Reddit.Models
{
    public class PostTarget(PostTargetKind kind, params string[] uri)
    {
        public PostTargetKind Kind { get; set; } = kind;

        public string[] Uris { get; set; } = uri;
    }
}