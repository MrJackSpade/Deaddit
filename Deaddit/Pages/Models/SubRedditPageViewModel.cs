using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;

namespace Deaddit.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(SubRedditName subreddit, ApplicationStyling applicationTheme)
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