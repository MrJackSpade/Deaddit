using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-post")]
    public class RedditPostWebComponent : DivComponent, ISelectionGroupItem
    {
        private const string TXT_COMMENT = "🗨";

        private const string TXT_DOTS = "...";

        private const string TXT_HIDE = "Hide";

        private const string TXT_SAVE = "Save";

        private const string TXT_SHARE = "Share";

        private const string TXT_UNSAVE = "Unsave";

        private readonly DivComponent _actionButtons;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly SpanComponent _downvoteButton;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly SpanComponent _score;

        private readonly SelectionGroup? _selectionGroup;

        private readonly SpanComponent _upvoteButton;

        private readonly IVisitTracker _visitTracker;

        private readonly SpanComponent _timeUser;

        public RedditPostWebComponent(ApiPost post, IAppNavigator appNavigator, IVisitTracker visitTracker, INavigation navigation, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup)
        {
            Post = post;
            _applicationStyling = applicationStyling;
            _redditClient = redditClient;
            _selectionGroup = selectionGroup;
            _visitTracker = visitTracker;
            _navigation = navigation;
            _appNavigator = appNavigator;

            Display = "flex";
            FlexDirection = "column";
            BackgroundColor = applicationStyling.SecondaryColor.ToHex();

            DivComponent topContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
            };

            _actionButtons = new()
            {
                Display = "none",
                FlexDirection = "row",
                Width = "100%",
                BackgroundColor = applicationStyling.HighlightColor.ToHex(),
            };

            ButtonComponent shareButton = this.ActionButton(TXT_SHARE);
            ButtonComponent saveButton = this.ActionButton(TXT_SAVE);
            ButtonComponent hideButton = this.ActionButton(TXT_HIDE);
            ButtonComponent moreButton = this.ActionButton(TXT_DOTS);
            ButtonComponent commentsButton = this.ActionButton(TXT_COMMENT);

            _actionButtons.Children.Add(shareButton);
            _actionButtons.Children.Add(saveButton);
            _actionButtons.Children.Add(hideButton);
            _actionButtons.Children.Add(moreButton);
            _actionButtons.Children.Add(commentsButton);

            ImgComponent thumbnail = new()
            {
                Src = post.TryGetPreview(),
                Width = $"{applicationStyling.ThumbnailSize}px",
                Height = $"{applicationStyling.ThumbnailSize}px",
                ObjectFit = "cover"
            };

            DivComponent textContainer = new()
            {
                Display = "flex",
                FlexDirection = "column",
                Padding = "10px",
                FlexGrow = "1"
            };

            SpanComponent title = new()
            {
                InnerText = post.Title,
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _timeUser = new()
            {
                InnerText = $"{post.CreatedUtc.Elapsed()} by {post.Author}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
                Display = "none"
            };

            SpanComponent metaData = new()
            {
                InnerText = $"{post.NumComments} comments {post.SubReddit}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            textContainer.Children.Add(title);
            textContainer.Children.Add(metaData);
            textContainer.Children.Add(_timeUser);

            DivComponent voteContainer = new()
            {
                Display = "flex",
                FlexGrow = "0",
                FlexDirection = "column",
                Padding = "10px"
            };

            _upvoteButton = new()
            {
                TextAlign = "center",
                InnerText = "▲",
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _score = new()
            {
                TextAlign = "center",
                InnerText = post.Score.ToString(),
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _downvoteButton = new()
            {
                TextAlign = "center",
                InnerText = "▼",
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _downvoteButton.OnClick += this.Downvote;
            _upvoteButton.OnClick += this.Upvote;
            textContainer.OnClick += this.TextContainer_OnClick;
            thumbnail.OnClick += this.Thumbnail_OnClick;

            voteContainer.Children.Add(_upvoteButton);
            voteContainer.Children.Add(_score);
            voteContainer.Children.Add(_downvoteButton);

            topContainer.Children.Add(thumbnail);
            topContainer.Children.Add(textContainer);
            topContainer.Children.Add(voteContainer);

            Children.Add(topContainer);
            Children.Add(_actionButtons);

            if (visitTracker.HasVisited(Post))
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
            }
        }

        public ApiPost Post { get; }

        public bool SelectEnabled => true;

        public void Select()
        {
            BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _timeUser.Display = "block";
            _actionButtons.Display = "flex";
        }

        public void Unselect()
        {
            BackgroundColor = _applicationStyling.SecondaryColor.ToHex();
            _timeUser.Display = "none";
            _actionButtons.Display = "none";
        }

        private ButtonComponent ActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.FontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = _applicationStyling.HighlightColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }

        private void Downvote(object? sender, EventArgs e)
        {
            switch (Post.Likes)
            {
                case UpvoteState.None:
                    Post.Score--;
                    Post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    Post.Score++;
                    Post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    Post.Score -= 2;
                    Post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }

            _score.InnerText = Post.Score.ToString();
        }

        private void TextContainer_OnClick(object? sender, EventArgs e)
        {
            _selectionGroup?.Toggle(this);
        }

        private async void Thumbnail_OnClick(object? sender, EventArgs e)
        {
            if (_selectionGroup != null)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                _selectionGroup?.Select(this);
                _visitTracker.Visit(Post);
            }

            await _navigation.OpenPost(Post, _appNavigator);
        }

        private void Upvote(object? sender, EventArgs e)
        {
            switch (Post.Likes)
            {
                case UpvoteState.None:
                    Post.Score++;
                    Post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    Post.Score--;
                    Post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    Post.Score += 2;
                    Post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;
            }

            _score.InnerText = Post.Score.ToString();
        }
    }
}