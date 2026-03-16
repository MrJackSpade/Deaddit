
namespace Deaddit.Core.Reddit.Models.Api
{
    public class RemoteImage
    {
        public string? Id { get; init; }

        public List<Source> Resolutions { get; init; } = [];

        public Source? Source { get; init; }

        public Variants? Variants { get; init; }
    }
}