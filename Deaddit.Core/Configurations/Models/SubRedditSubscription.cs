using Deaddit.Core.Reddit.Models;

namespace Deaddit.Core.Configurations.Models
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

        public SubRedditSubscription(string displayString, string subReddit, ApiPostSort sort)
        {
            Sort = sort;
            DisplayString = displayString;
            SubReddit = subReddit;
        }

        public string? DisplayString { get; set; }

        public ApiPostSort Sort { get; set; } = ApiPostSort.Hot;

        public string? SubReddit { get; set; }
    }
}