using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;

namespace Deaddit.PageModels
{
    internal class ReplyPageViewModel : BaseViewModel
    {
        private readonly IAppTheme _appTheme;

        private readonly RedditThing _redditPost;

        public ReplyPageViewModel(IAppTheme appTheme, RedditThing post)
        {
            _appTheme = appTheme;
            SecondaryColor = appTheme.SecondaryColor;
            TextColor = appTheme.TextColor;
            PrimaryColor = appTheme.PrimaryColor;
            TertiaryColor = appTheme.TertiaryColor;
            _redditPost = post;
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