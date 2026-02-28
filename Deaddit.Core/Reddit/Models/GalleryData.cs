using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class GalleryData
    {
        [JsonPropertyName("items")]
        public List<Item> Items { get; init; } = [];
    }
}
