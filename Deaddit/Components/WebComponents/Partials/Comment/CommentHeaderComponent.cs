﻿using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class CommentHeaderComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiComment _comment;

        private readonly SpanComponent _voteIndicator;

        private SpanComponent _commentMeta;

        public CommentHeaderComponent(ApplicationStyling applicationStyling, ApiComment comment)
        {
            _applicationStyling = applicationStyling;
            _comment = comment;
            _voteIndicator = new SpanComponent();

            this.InitializeHeader();
            this.SetIndicatorState(_comment.Likes);
        }

        public void SetIndicatorState(UpvoteState state)
        {
            switch (state)
            {
                case UpvoteState.Upvote:
                    _comment.Likes = UpvoteState.Upvote;
                    _voteIndicator.InnerText = "▲";
                    _voteIndicator.Color = _applicationStyling.UpvoteColor.ToHex();
                    _voteIndicator.Display = "inline-block";
                    break;

                case UpvoteState.Downvote:
                    _comment.Likes = UpvoteState.Downvote;
                    _voteIndicator.InnerText = "▼";
                    _voteIndicator.Color = _applicationStyling.DownvoteColor.ToHex();
                    _voteIndicator.Display = "inline-block";
                    break;

                default:
                    _comment.Likes = UpvoteState.None;
                    _voteIndicator.InnerText = string.Empty;
                    _voteIndicator.Color = _applicationStyling.TextColor.ToHex();
                    _voteIndicator.Display = "none";
                    break;
            }
        }

        public void UpdateMeta()
        {
            _commentMeta.InnerText = this.GetMetaData();
        }

        private string GetMetaData()
        {
            if (!_comment.ScoreHidden == true)
            {
                return $"{_comment.Score} points {_comment.CreatedUtc.Elapsed()}";
            }
            else
            {
                return _comment.CreatedUtc.Elapsed();
            }
        }

        private void InitializeHeader()
        {
            SpanComponent authorSpan = new()
            {
                InnerText = _comment.Author,
                Color = _applicationStyling.SubTextColor.ToHex(),
                MarginRight = "5px"
            };

            _commentMeta = new()
            {
                Color = _applicationStyling.SubTextColor.ToHex(),
                FontSize = $"{(int)(_applicationStyling.FontSize * .75)}px",
                InnerText = this.GetMetaData()
            };

            Children.Add(_voteIndicator);
            Children.Add(authorSpan);
            Children.Add(_commentMeta);
        }
    }
}