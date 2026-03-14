using Deaddit.Core.Reddit.Models.Requests;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class SubRedditDefinition(string name) : ThingDefinition(name, 'r')
    {
        public override Enum DefaultSort => ApiPostSort.Hot;

        public override ApiEndpointDefinition EndpointDefinition => new("r/" + Name);

        public override ThingKind Kind => ThingKind.Subreddit;
    }
}