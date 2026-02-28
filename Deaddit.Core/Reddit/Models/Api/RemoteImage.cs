using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class RemoteImage
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("resolutions")]
        public List<Source> Resolutions { get; init; } = [];

        [JsonPropertyName("source")]
        public Source? Source { get; init; }

        [JsonPropertyName("variants")]
        public Variants? Variants { get; init; }
    }
}