using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Interfaces;

namespace Deaddit.EventArguments
{
    public class SubRedditSubscriptionRemoveEventArgs(ISubscriptionComponent component, ThingDefinition thing) : EventArgs
    {
        public ISubscriptionComponent Component { get; private set; } = component;

        public ThingDefinition Thing { get; private set; } = thing;
    }
}