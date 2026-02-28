using Deaddit.Core.Reddit.Models.Requests;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class SavedDefinition : ThingDefinition
    {
        public SavedDefinition() : base("Saved", '\0')
        {
        }

        public override Enum? DefaultSort => null;

        public override ApiEndpointDefinition EndpointDefinition => new("user/%USER%/saved");

        public override bool FilteredByDefault => false;

        public override ThingKind Kind => ThingKind.Listing;
    }
}