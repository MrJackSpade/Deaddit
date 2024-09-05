using Deaddit.Configurations.Models;
using Deaddit.Reddit.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(SubRedditName subreddit, ApplicationTheme applicationTheme)
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