using Deaddit.Core.Reddit.Models;

namespace Deaddit.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(ThingCollectionName subreddit)
        {
            SubReddit = subreddit.DisplayName;
        }

        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }
    }
}