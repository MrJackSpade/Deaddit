using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class Item
    {
        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("media_id")]
        public string? MediaId { get; init; }

        [JsonPropertyName("outbound_url")]
        public string? OutboundUrl { get; set; }
    }
}