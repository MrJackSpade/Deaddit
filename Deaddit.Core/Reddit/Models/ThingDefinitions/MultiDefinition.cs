using Deaddit.Core.Reddit.Models.Requests;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class MultiDefinition(string name, string user = "%USER%") : ThingDefinition(name, 'm')
    {
        public override Enum DefaultSort => ApiPostSort.Hot;

        public override ApiEndpointDefinition EndpointDefinition => new($"/user/{User}/m/{Name}");

        public override ThingKind Kind => ThingKind.Multi;

        public string User { get; } = user;
    }
}