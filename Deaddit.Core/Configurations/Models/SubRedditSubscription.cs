using Deaddit.Core.Reddit.Models;
using System.Text.Json.Serialization;

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
            ThingName = subReddit;
        }

        public string? DisplayString { get; set; }

        public ApiPostSort Sort { get; set; } = ApiPostSort.Hot;

        [JsonPropertyName("SubReddit")]
        public string? ThingName { get; set; }
    }
}