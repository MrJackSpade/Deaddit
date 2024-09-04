using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class MediaMetaData
    {
        [JsonPropertyName("e")]
        public string? E { get; set; }

        [JsonPropertyName("ext")]
        public string? Ext { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("m")]
        public string? M { get; set; }

        [JsonPropertyName("p")]
        public List<ImageLocation> P { get; set; } = [];

        [JsonPropertyName("s")]
        public ImageLocation? S { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("t")]
        public string? Text { get; set; }
    }
}