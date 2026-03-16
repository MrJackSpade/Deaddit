
namespace Deaddit.Core.Reddit.Models.Api
{
    public class Preview
    {
        public bool Enabled { get; init; }

        public List<RemoteImage> Images { get; init; } = [];

        public string? RedditVideoPreview { get; init; }
    }
}