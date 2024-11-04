using Deaddit.Core.Configurations.Models;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    public class VoteContainerComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly SpanComponent _downvoteButton;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        private readonly SpanComponent _score;

        private readonly SpanComponent _upvoteButton;

        public event EventHandler DownvoteClicked;

        public event EventHandler UpvoteClicked;

        public VoteContainerComponent(ApplicationStyling applicationStyling, ApiPost post, IRedditClient redditClient)
        {
            _applicationStyling = applicationStyling;
            _post = post;
            _redditClient = redditClient;

            Display = "flex";
            FlexGrow = "0";
            FlexDirection = "column";
            Padding = "10px";

            _upvoteButton = new SpanComponent
            {
                TextAlign = "center",
                InnerText = "▲",
                FontSize = $"{applicationStyling.TitleFontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _score = new SpanComponent
            {
                TextAlign = "center",
                InnerText = _post.Score.ToString(),
                FontSize = $"{applicationStyling.TitleFontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _downvoteButton = new SpanComponent
            {
                TextAlign = "center",
                InnerText = "▼",
                FontSize = $"{applicationStyling.TitleFontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            this.UpdateVoteState();

            _upvoteButton.OnClick += this.Upvote;
            _downvoteButton.OnClick += this.Downvote;

            Children.Add(_upvoteButton);
            Children.Add(_score);
            Children.Add(_downvoteButton);
        }

        public void UpdateScore()
        {
            _score.InnerText = _post.Score.ToString();
        }

        public void UpdateVoteState()
        {
            _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
            _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
            _score.Color = _applicationStyling.TextColor.ToHex();

            switch (_post.Likes)
            {
                case UpvoteState.Upvote:
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }
        }

        private void Downvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case UpvoteState.None:
                    _post.Score--;
                    _post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _post.Score++;
                    _post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    _post.Score -= 2;
                    _post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score?.ToString() ?? string.Empty;
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        private void Upvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case UpvoteState.None:
                    _post.Score++;
                    _post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    _post.Score--;
                    _post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _post.Score += 2;
                    _post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score.ToString();
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }
    }
}