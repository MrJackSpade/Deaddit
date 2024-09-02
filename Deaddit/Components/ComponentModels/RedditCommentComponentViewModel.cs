using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;
using Deaddit.PageModels;
using System.Web;

namespace Deaddit.Components.ComponentModels
{
    public class RedditCommentComponentViewModel : BaseViewModel, ICanUpvote
    {
        private readonly IAppTheme _appTheme;

        private readonly RedditThing _comment;

        public RedditCommentComponentViewModel(RedditThing comment, IAppTheme appTheme, IMarkDownService markDownService)
        {
            _comment = comment;
            Author = comment.Author;
            Content = markDownService.Clean(comment.Body);
            Score = $"{comment.Score}";
            TextColor = appTheme.TextColor;
            TertiaryColor = appTheme.TertiaryColor;
            SecondaryColor = appTheme.SecondaryColor;
            TextColor = appTheme.TextColor;
            SubTextColor = appTheme.SubTextColor;
            UpvoteButtonColor = appTheme.TextColor;
            DownvoteButtonColor = appTheme.TextColor;
            HighlightColor = appTheme.HighlightColor;
            FontSize = appTheme.FontSize;
            HyperlinkColor = appTheme.HyperlinkColor;
            _appTheme = appTheme;

            if (comment.Distinguished == DistinguishedKind.Moderator)
            {
                AuthorColor = appTheme.DistinguishedColor;
            }
            else
            {
                AuthorColor = appTheme.TertiaryColor;
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

        public Color DownvoteColor => _appTheme.DownvoteColor;

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

        public Color UpvoteColor => _appTheme.UpvoteColor;

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