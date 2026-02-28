using Deaddit.Core.Reddit.Interfaces;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class ApiMore : ApiThing, IMore
    {
        [JsonPropertyName("children")]
        public List<string> ChildNames { get; set; } = [];

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("depth")]
        public int? Depth { get; set; }
    }
}
