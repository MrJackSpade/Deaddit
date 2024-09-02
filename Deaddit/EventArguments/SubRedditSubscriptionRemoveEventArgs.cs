using Deaddit.Components;
using Deaddit.Configurations;

namespace Deaddit.EventArguments
{
    public class SubRedditSubscriptionRemoveEventArgs(SubRedditSubscription subscription, SubRedditComponent component) : EventArgs
    {
        public SubRedditSubscription Subscription { get; private set; } = subscription;

        public SubRedditComponent Component { get; private set; } = component;
    }
}
