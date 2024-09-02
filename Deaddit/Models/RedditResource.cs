namespace Deaddit.Models
{
    public enum RedditResourceKind
    {
        Undefined,

        Video,

        Image,

        Audio,

        Post,

        Url
    }

    public class RedditResource(RedditResourceKind kind, string? uri)
    {
        public RedditResourceKind Kind { get; set; } = kind;

        public string? Uri { get; set; } = uri;
    }
}