using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class GalleryData
    {
        [JsonPropertyName("items")]
        public List<Item> Items { get; init; } = [];
    }
}