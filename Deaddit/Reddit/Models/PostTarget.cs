namespace Deaddit.Reddit.Models
{
    public class PostTarget(PostTargetKind kind, string? uri)
    {
        public PostTargetKind Kind { get; set; } = kind;

        public string? Uri { get; set; } = uri;
    }
}