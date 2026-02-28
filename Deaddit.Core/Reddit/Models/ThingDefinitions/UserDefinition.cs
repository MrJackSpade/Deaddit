using Deaddit.Core.Reddit.Models.Requests;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class UserDefinition(string name) : ThingDefinition(name, 'u')
    {
        public override Enum DefaultSort => UserProfileSort.New;

        public override ApiEndpointDefinition EndpointDefinition => new($"/user/{Name}");

        public override bool FilteredByDefault => false;

        public override ThingKind Kind => ThingKind.Account;
    }
}