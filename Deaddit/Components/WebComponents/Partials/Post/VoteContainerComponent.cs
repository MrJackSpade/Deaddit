using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Enums;

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

        public event EventHandler DownvoteClicked;

        public event EventHandler UpvoteClicked;

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
                case VoteState.Upvote:
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case VoteState.Downvote:
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }
        }

        private void Downvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case VoteState.None:
                    _post.Score--;
                    _post.Likes = VoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;

                case VoteState.Downvote:
                    _post.Score++;
                    _post.Likes = VoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case VoteState.Upvote:
                    _post.Score -= 2;
                    _post.Likes = VoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score?.ToString() ?? string.Empty;
            _redditClient.SetVoteState(_post, _post.Likes);
        }

        private void Upvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case VoteState.None:
                    _post.Score++;
                    _post.Likes = VoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case VoteState.Upvote:
                    _post.Score--;
                    _post.Likes = VoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case VoteState.Downvote:
                    _post.Score += 2;
                    _post.Likes = VoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score.ToString();
            _redditClient.SetVoteState(_post, _post.Likes);
        }
    }
}