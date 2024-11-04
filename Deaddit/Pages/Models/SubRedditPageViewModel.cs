using Reddit.Api.Models.ThingDefinitions;

namespace Deaddit.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public SubRedditPageViewModel(ThingDefinition subreddit)
        {
            SubReddit = subreddit.Name;
        }
    }
}