using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Interfaces;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components.ComponentModels
{
    public class RedditCommentComponentViewModel : BaseViewModel, IVotableViewModel
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly ApiThing _comment;

        public RedditCommentComponentViewModel(ApiThing comment, ApplicationTheme applicationTheme)
        {
            _comment = comment;
            Author = comment.Author;
            Content = MarkDownHelper.Clean(comment.Body);
            Score = $"{comment.Score}";
            TertiaryColor = applicationTheme.TertiaryColor;
            SecondaryColor = applicationTheme.SecondaryColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            TextColor = applicationTheme.TextColor;
            SubTextColor = applicationTheme.SubTextColor;
            UpvoteButtonColor = applicationTheme.TextColor;
            DownvoteButtonColor = applicationTheme.TextColor;
            HighlightColor = applicationTheme.HighlightColor;
            FontSize = applicationTheme.FontSize;
            HyperlinkColor = applicationTheme.HyperlinkColor;
            _applicationTheme = applicationTheme;

            if (comment.Distinguished == DistinguishedKind.Moderator)
            {
                AuthorColor = applicationTheme.DistinguishedColor;
            }
            else
            {
                AuthorColor = applicationTheme.TertiaryColor;
            }

            this.UpdateMetaData();
            this.SetUpvoteState(comment.Likes);
        }

        public string? Author
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color AuthorColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string? Content
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color DownvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color DownvoteColor => _applicationTheme.DownvoteColor;

        public double FontSize
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public Color HighlightColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color HyperlinkColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string? MetaData
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color PrimaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string Score
        {
            get => this.GetValue<string>();
            set
            {
                this.SetValue(value);
                this.UpdateMetaData();
            }
        }

        public Color SecondaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color SubTextColor
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

        public Color UpvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color UpvoteColor => _applicationTheme.UpvoteColor;

        public string VoteIndicatorText
        {
            get => this.GetValue<string>();
            set
            {
                this.SetValue(value);
                this.UpdateMetaData();
            }
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

        private void UpdateMetaData()
        {
            MetaData = $"{Score} points {_comment.CreatedUtc.Elapsed()}";
        }
    }
}