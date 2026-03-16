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

        private readonly SpanComponent _voteIndicator;

        public CommentHeaderComponent(ApplicationStyling applicationStyling, ApplicationHacks applicationHacks, ApiComment comment, ApiPost parentPost)
        {
            _applicationStyling = applicationStyling;
            _comment = comment;
            _voteIndicator = new SpanComponent();

            AuthorNameComponent authorSpan = new(comment.Author, applicationStyling, _comment.Distinguished, _comment.Author == parentPost?.Author);

            _commentMeta = new()
            {
                Color = _applicationStyling.SubTextColor.ToHex(),
                FontSize = $"{_applicationStyling.SubTextFontSize}px",
                InnerText = this.GetMetaData()
            };

            Children.Add(_voteIndicator);
            Children.Add(authorSpan);

            string flairColor = comment.AuthorFlairBackgroundColor.ToHex() ?? applicationStyling.SubTextColor.ToHex();
            if (applicationHacks.ShouldResolveFlairImages() && comment.AuthorFlairRichText.Count > 0)
            {
                RichTextFlairComponent userFlair = new(comment.AuthorFlairRichText, flairColor, applicationStyling);
                Children.Add(userFlair);
            }
            else if (!string.IsNullOrWhiteSpace(comment.AuthorFlairText))
            {
                FlairComponent userFlair = new(comment.AuthorFlairText, flairColor, applicationStyling);
                Children.Add(userFlair);
            }

            Children.Add(_commentMeta);

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
            _commentMeta.InnerText = this.GetMetaData();
        }

        private string GetMetaData()
        {
            if (!_comment.ScoreHidden == true)
            {
                string arrow = _comment.Score > 0 ? "▲" : _comment.Score < 0 ? "▼" : "";
                return $"{_comment.Score}{arrow} {_comment.CreatedUtc.Elapsed()}";
            }
            else
            {
                return _comment.CreatedUtc.Elapsed();
            }
        }
    }
}