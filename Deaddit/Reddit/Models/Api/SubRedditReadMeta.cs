using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ReadMetaData<T>
    {
        [JsonPropertyName("after")]
        public string? After { get; init; }

        [JsonPropertyName("before")]
        public string? Before { get; init; }

        [JsonPropertyName("children")]
        public List<T> Children { get; init; } = [];

        [JsonPropertyName("dist")]
        public int? Dist { get; init; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; init; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; init; }
    }
}