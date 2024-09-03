using Deaddit.Exceptions;
using Deaddit.MAUI.Interfaces;
using Deaddit.Reddit.Models;

namespace Deaddit.MAUI.Extensions
{
    internal static class ICanUpvoteExtensions
    {
        public static void SetUpvoteState(this IVotableViewModel target, UpvoteState upvoteState)
        {
            //TODO: Convert this to a single model and bind, instead of keeping all these properties on
            //each relevant model
            switch (upvoteState)
            {
                case UpvoteState.Upvote:
                    target.UpvoteButtonColor = target.UpvoteColor;
                    target.DownvoteButtonColor = target.TextColor;
                    target.VoteIndicatorTextColor = target.UpvoteColor;
                    target.VoteIndicatorText = "▲";
                    break;

                case UpvoteState.Downvote:
                    target.UpvoteButtonColor = target.TextColor;
                    target.DownvoteButtonColor = target.DownvoteColor;
                    target.VoteIndicatorTextColor = target.DownvoteColor;
                    target.VoteIndicatorText = "▼";
                    break;

                case UpvoteState.None:
                    target.UpvoteButtonColor = target.TextColor;
                    target.DownvoteButtonColor = target.TextColor;
                    target.VoteIndicatorTextColor = target.TextColor;
                    target.VoteIndicatorText = "";
                    break;

                default: throw new UnhandledEnumException(upvoteState);
            }
        }
    }
}