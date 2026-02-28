using Deaddit.Core.Reddit.Models.ThingDefinitions;

namespace Deaddit.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(ThingDefinition subreddit)
        {
            SubReddit = subreddit.Name;
        }

        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }
    }
}