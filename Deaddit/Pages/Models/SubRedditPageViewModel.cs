using Deaddit.Core.Reddit.Models;

namespace Deaddit.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public SubRedditPageViewModel(ThingCollectionName subreddit)
        {
            SubReddit = subreddit.DisplayName;
        }
    }
}