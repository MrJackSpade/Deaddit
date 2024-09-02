namespace Deaddit.Configurations
{
    public class LandingPageSubscription
    {
        public string Sort { get; set; }

        public string SubReddit { get; set; }
    }

    public class LandingPageSubscriptions
    {
        public List<LandingPageSubscription> Subscriptions { get; set; } = [];
    }
}