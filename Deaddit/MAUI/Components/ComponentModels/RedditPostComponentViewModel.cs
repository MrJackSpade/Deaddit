using Deaddit.Configurations.Models;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Interfaces;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components.ComponentModels
{
    public class RedditPostComponentViewModel : BaseViewModel, IVotableViewModel
    {
        private readonly ApplicationStyling _applicationTheme;

        private readonly ApiPost _redditPost;

        public RedditPostComponentViewModel(ApiPost redditPost, ApplicationStyling applicationTheme)
        {
            _redditPost = redditPost;
            _applicationTheme = applicationTheme;
            Score = redditPost.Score?.ToString();

            this.SetUpvoteState(redditPost.Likes);
        }

        public Color DownvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color DownvoteColor => _applicationTheme.DownvoteColor;

        public string? Score
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color TextColor => _applicationTheme.TextColor;

        public Color UpvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color UpvoteColor => _applicationTheme.UpvoteColor;

        public string VoteIndicatorText
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color VoteIndicatorTextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public void TryAdjustScore(long mod)
        {
            if (long.TryParse(Score, out long score))
            {
                Score = (score + mod).ToString();
            }
        }
    }
}