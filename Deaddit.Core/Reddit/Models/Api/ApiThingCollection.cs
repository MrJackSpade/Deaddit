
namespace Deaddit.Core.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ApiThingCollection
    {
        public string? After { get; init; }

        public string? Before { get; init; }

        public List<ApiThing> Children { get; init; } = [];

        public int? Dist { get; init; }

        public string? GeoFilter { get; init; }

        public string? ModHash { get; init; }
    }
}