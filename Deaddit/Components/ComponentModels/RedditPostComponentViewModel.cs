using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;
using Deaddit.PageModels;
using System.Web;

namespace Deaddit.Components.ComponentModels
{
    public class RedditPostComponentViewModel : BaseViewModel, ICanUpvote
    {
        private readonly IAppTheme _appTheme;

        private readonly RedditPost _redditPost;

        public RedditPostComponentViewModel(RedditPost redditPost, bool postBodyIsVisible, IAppTheme appTheme, IMarkDownService markDownService)
        {
            _redditPost = redditPost;
            _appTheme = appTheme;
            MinHeight = appTheme.ThumbnailSize;
            VoteHeight = appTheme.ThumbnailSize / 2;
            Score = redditPost.Score?.ToString();
            FontSize = appTheme.FontSize;
            TertiaryColor = appTheme.TertiaryColor;
            SecondaryColor = appTheme.SecondaryColor;
            HighlightColor = appTheme.HighlightColor;
            LinkFlairBackgroundColor = redditPost.LinkFlairBackgroundColor;
            LinkFlairText = redditPost.LinkFlairText;
            LinkFlairTextColor = redditPost.LinkFlairTextColor;
            LinkFlairIsVisible = !string.IsNullOrWhiteSpace(LinkFlairText);

            TextColor = appTheme.TextColor;
            SubTextColor = appTheme.SubTextColor;
            PrimaryColor = appTheme.PrimaryColor;
            HyperlinkColor = appTheme.HyperlinkColor;
            Thumbnail = redditPost.TryGetPreview();
            Title = HttpUtility.HtmlDecode(redditPost.Title);
            CommentsSubReddit = $"{redditPost.NumComments} comments {redditPost.SubReddit}";
            TimeUser = $"{redditPost.CreatedUtc.Elapsed()} by {redditPost.Author}";
            PostBody = markDownService.Clean(_redditPost.Body);
            PostBodyVisible = postBodyIsVisible;
            this.SetUpvoteState(redditPost.Likes);
        }

        public bool LinkFlairIsVisible
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }

        public string CommentsSubReddit
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

        public Color LinkFlairBackgroundColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string LinkFlairText
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color LinkFlairTextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public double MinHeight
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public string PostBody
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public bool PostBodyVisible
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }

        public Color PrimaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string? Score
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
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

        public string? Thumbnail
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public string TimeUser
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public string? Title
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color UpvoteButtonColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color UpvoteColor => _appTheme.UpvoteColor;

        public double VoteHeight
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

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