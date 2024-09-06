using Deaddit.Core.Configurations.Models;
using Deaddit.MAUI.Components;

namespace Deaddit.EventArguments
{
    public class SubRedditSubscriptionRemoveEventArgs(SubRedditSubscription subscription, SubRedditComponent component) : EventArgs
    {
        public SubRedditComponent Component { get; private set; } = component;

        public SubRedditSubscription Subscription { get; private set; } = subscription;
    }
}