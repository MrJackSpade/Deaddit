using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class Item
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("media_id")]
        public string? MediaId { get; set; }
    }
}