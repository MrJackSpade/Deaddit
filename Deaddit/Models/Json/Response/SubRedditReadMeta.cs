using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ReadMetaData<T>
    {
        [JsonPropertyName("after")]
        public string? After { get; set; }

        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("children")]
        public List<T> Children { get; set; } = [];

        [JsonPropertyName("dist")]
        public int? Dist { get; set; }

        [JsonPropertyName("geo_filter")]
        public string? GeoFilter { get; set; }

        [JsonPropertyName("modhash")]
        public string? ModHash { get; set; }
    }
}