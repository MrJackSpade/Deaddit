namespace Deaddit.Configurations.Models
{
    public class LandingPageConfiguration
    {
        public List<SubRedditSubscription> Subscriptions { get; set; } = [];
    }

    public class SubRedditSubscription
    {
        public SubRedditSubscription()
        {
        }

        public SubRedditSubscription(string displayString, string subReddit, string sort)
        {
            Sort = sort;
            DisplayString = displayString;
            SubReddit = subReddit;
        }

        public string? DisplayString { get; set; }

        public string? Sort { get; set; } = "Hot";

        public string? SubReddit { get; set; }
    }
}