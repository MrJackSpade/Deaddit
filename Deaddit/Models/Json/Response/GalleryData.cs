using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class GalleryData
    {
        [JsonPropertyName("items")]
        public List<Item> Items { get; set; } = [];
    }
}