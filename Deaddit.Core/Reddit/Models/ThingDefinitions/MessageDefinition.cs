using Deaddit.Core.Reddit.Models.Requests;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class MessageDefinition() : ThingDefinition("Messages", '\0')
    {
        public override Enum DefaultSort => InboxSort.Undefined;

        public override ApiEndpointDefinition EndpointDefinition => new("message");

        public override bool FilteredByDefault => false;

        public override ThingKind Kind => ThingKind.Message;
    }
}