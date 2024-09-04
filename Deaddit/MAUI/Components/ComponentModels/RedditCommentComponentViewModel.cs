using Deaddit.Configurations.Models;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Interfaces;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components.ComponentModels
{
    public class RedditCommentComponentViewModel : BaseViewModel, IVotableViewModel
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly ApiThing _comment;

        public RedditCommentComponentViewModel(ApiThing comment, ApplicationTheme applicationTheme)
        {
            _comment = comment;

            UpvoteButtonColor = applicationTheme.TextColor;
            DownvoteButtonColor = applicationTheme.TextColor;
            _applicationTheme = applicationTheme;

            this.SetUpvoteState(comment.Likes);
        }

        public Color DownvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color DownvoteColor => _applicationTheme.DownvoteColor;

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
    }
}