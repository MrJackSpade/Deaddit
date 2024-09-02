using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class LinkFlair
    {
        [JsonPropertyName("t")]
        public string? Text { get; set; }

        [JsonPropertyName("e")]
        public string? Type { get; set; }
    }
}