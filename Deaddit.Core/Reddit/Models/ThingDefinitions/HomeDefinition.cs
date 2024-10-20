using Deaddit.Core.Reddit.Models.Requests;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public class HomeDefinition : SubRedditDefinition
    {
        public override string DisplayName => "Home";

        public override ApiEndpointDefinition EndpointDefinition => new("");

        public HomeDefinition() : base("")
        {
        }
    }
}