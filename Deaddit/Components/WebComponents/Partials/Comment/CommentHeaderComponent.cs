using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Enums;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class CommentHeaderComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiComment _comment;

        private readonly SpanComponent _commentMeta;

        private readonly SpanComponent _scoreArrow;

        private readonly SpanComponent _voteIndicator;

        public CommentHeaderComponent(ApplicationStyling applicationStyling, ApplicationHacks applicationHacks, ApiComment comment, ApiPost parentPost)
        {
            _applicationStyling = applicationStyling;
            _comment = comment;
            _voteIndicator = new SpanComponent();

            AuthorNameComponent authorSpan = new(comment.Author, applicationStyling, _comment.Distinguished, _comment.Author == parentPost?.Author);

            _scoreArrow = new()
            {
                FontSize = $"{_applicationStyling.SubTextFontSize * 0.5}px",
                LineHeight = "1",
                MarginRight = "3px"
            };

            _commentMeta = new()
            {
                Color = _applicationStyling.SubTextColor.ToHex(),
                FontSize = $"{_applicationStyling.SubTextFontSize}px",
            };

            Children.Add(_voteIndicator);
            Children.Add(authorSpan);
            Children.Add(_scoreArrow);
            Children.Add(_commentMeta);

            string? flairBackgroundColor = comment.AuthorFlairBackgroundColor.ToHex();
            string flairTextColor = comment.AuthorFlairTextColor.ToFlairTextHex(applicationStyling);
            if (applicationHacks.ShouldResolveFlairImages() && comment.AuthorFlairRichText.Count > 0)
            {
                DivComponent flairContainer = new();
                RichTextFlairComponent userFlair = new(comment.AuthorFlairRichText, flairTextColor, applicationStyling, flairBackgroundColor);
                flairContainer.Children.Add(userFlair);
                Children.Add(flairContainer);
            }
            else if (!string.IsNullOrWhiteSpace(comment.AuthorFlairText))
            {
                DivComponent flairContainer = new();
                FlairComponent userFlair = new(comment.AuthorFlairText, flairTextColor, applicationStyling, flairBackgroundColor);
                flairContainer.Children.Add(userFlair);
                Children.Add(flairContainer);
            }

            this.SetIndicatorState(_comment.Likes);
        }

        public void SetIndicatorState(VoteState state)
        {
            switch (state)
            {
                case VoteState.Upvote:
                    _comment.Likes = VoteState.Upvote;
                    _voteIndicator.InnerText = "▲";
                    _voteIndicator.Color = _applicationStyling.UpvoteColor.ToHex();
                    _voteIndicator.Display = "inline-block";
                    break;

                case VoteState.Downvote:
                    _comment.Likes = VoteState.Downvote;
                    _voteIndicator.InnerText = "▼";
                    _voteIndicator.Color = _applicationStyling.DownvoteColor.ToHex();
                    _voteIndicator.Display = "inline-block";
                    break;

                default:
                    _comment.Likes = VoteState.None;
                    _voteIndicator.InnerText = string.Empty;
                    _voteIndicator.Color = _applicationStyling.TextColor.ToHex();
                    _voteIndicator.Display = "none";
                    break;
            }

            this.UpdateMeta();
        }

        public void UpdateMeta()
        {
            if (!_comment.ScoreHidden == true)
            {
                string arrow = _comment.Score > 0 ? "▲" : _comment.Score < 0 ? "▼" : "";
                _scoreArrow.InnerText = arrow;
                _scoreArrow.Display = string.IsNullOrEmpty(arrow) ? "none" : "inline-block";
                _commentMeta.InnerText = $"{_comment.Score} {_comment.CreatedUtc.Elapsed()}";
            }
            else
            {
                _scoreArrow.InnerText = string.Empty;
                _scoreArrow.Display = "none";
                _commentMeta.InnerText = _comment.CreatedUtc.Elapsed();
            }
        }
    }
}