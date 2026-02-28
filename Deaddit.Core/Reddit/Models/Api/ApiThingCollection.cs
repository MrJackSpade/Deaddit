using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ApiThingCollection
    {
        [JsonPropertyName("after")]
        public string? After { get; init; }

        [JsonPropertyName("before")]
        public string? Before { get; init; }

        [JsonPropertyName("children")]
        public List<ApiThing> Children { get; init; } = [];

        [JsonPropertyName("dist")]
        public int? Dist { get; init; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; init; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; init; }
    }
}