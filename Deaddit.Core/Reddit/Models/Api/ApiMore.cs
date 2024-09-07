using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMore : ApiThing
    {
        [JsonPropertyName("children")]
        public List<string> ChildNames { get; init; } = [];

        [JsonPropertyName("count")]
        public int? Count { get; init; }
    }
}