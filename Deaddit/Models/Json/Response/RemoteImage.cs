using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class RemoteImage
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("resolutions")]
        public List<Source> Resolutions { get; set; } = [];

        [JsonPropertyName("source")]
        public Source? Source { get; set; }

        [JsonPropertyName("variants")]
        public Variants? Variants { get; set; }
    }
}