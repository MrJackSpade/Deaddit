using Deaddit.Interfaces;

namespace Deaddit.PageModels
{
    internal class SubRedditPageViewModel : BaseViewModel
    {
        private readonly IAppTheme _appTheme;

        public SubRedditPageViewModel(string subreddit, IAppTheme appTheme)
        {
            _appTheme = appTheme;
            SecondaryColor = appTheme.SecondaryColor;
            TextColor = appTheme.TextColor;
            PrimaryColor = appTheme.PrimaryColor;
            TertiaryColor = appTheme.TertiaryColor;

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