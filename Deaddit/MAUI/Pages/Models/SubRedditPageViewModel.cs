using Deaddit.Configurations.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(string subreddit, ApplicationTheme applicationTheme)
        {
            SubReddit = subreddit;
        }
        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }
    }
}