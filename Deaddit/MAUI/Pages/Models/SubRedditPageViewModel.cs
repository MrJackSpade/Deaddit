using Deaddit.Configurations.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        public SubRedditPageViewModel(string subreddit, ApplicationTheme applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor;
            TextColor = applicationTheme.TextColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            TertiaryColor = applicationTheme.TertiaryColor;

            SubReddit = subreddit;
        }

        public Color PrimaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color SecondaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color TertiaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color TextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }
    }
}