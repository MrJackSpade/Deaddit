using Deaddit.Core.Reddit.Interfaces;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMore : ApiThing, IMore
    {
        [JsonPropertyName("children")]
        public List<string> ChildNames { get; init; } = [];

        [JsonPropertyName("count")]
        public int? Count { get; init; }

        [JsonPropertyName("depth")]
        public int? Depth { get; init; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; init; }
    }
}