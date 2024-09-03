using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Interfaces;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;
using System.Web;

namespace Deaddit.MAUI.Components.ComponentModels
{
    public class RedditPostComponentViewModel : BaseViewModel, IVotableViewModel
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly RedditPost _redditPost;

        public RedditPostComponentViewModel(RedditPost redditPost, bool postBodyIsVisible, double opacity, ApplicationTheme applicationTheme)
        {
            _redditPost = redditPost;
            _applicationTheme = applicationTheme;
            MinHeight = applicationTheme.ThumbnailSize;
            VoteHeight = applicationTheme.ThumbnailSize / 2;
            Score = redditPost.Score?.ToString();
            FontSize = applicationTheme.FontSize;
            TertiaryColor = applicationTheme.TertiaryColor;
            SecondaryColor = applicationTheme.SecondaryColor;
            HighlightColor = applicationTheme.HighlightColor;
            LinkFlairBackgroundColor = redditPost.LinkFlairBackgroundColor;
            LinkFlairText = redditPost.LinkFlairText;
            LinkFlairTextColor = redditPost.LinkFlairTextColor;
            LinkFlairIsVisible = !string.IsNullOrWhiteSpace(LinkFlairText);
            Opacity = opacity;
            TextColor = applicationTheme.TextColor;
            SubTextColor = applicationTheme.SubTextColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            HyperlinkColor = applicationTheme.HyperlinkColor;
            Thumbnail = redditPost.TryGetPreview();
            Title = HttpUtility.HtmlDecode(redditPost.Title);
            VisibleMetaData = $"{redditPost.NumComments} comments {redditPost.SubReddit}";
            TimeUser = $"{redditPost.CreatedUtc.Elapsed()} by {redditPost.Author}";
            PostBody = MarkDownHelper.Clean(_redditPost.Body);
            PostBodyVisible = postBodyIsVisible;

            if(Uri.TryCreate(_redditPost.Url, UriKind.Absolute, out Uri result))
            {
                VisibleMetaData += $" ({result.Host})";
            } 

            this.SetUpvoteState(redditPost.Likes);
        }

        public double Opacity
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public string VisibleMetaData
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

        public Color? LinkFlairBackgroundColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public bool LinkFlairIsVisible
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }

        public string? LinkFlairText
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color? LinkFlairTextColor
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

        public Color UpvoteColor => _applicationTheme.UpvoteColor;

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