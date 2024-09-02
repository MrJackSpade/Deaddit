namespace Deaddit.Interfaces
{
    internal interface ICanUpvote
    {
        Color DownvoteButtonColor { get; set; }

        Color DownvoteColor { get; }

        Color TextColor { get; }

        Color UpvoteButtonColor { get; set; }

        Color UpvoteColor { get; }

        string VoteIndicatorText { get; set; }

        Color VoteIndicatorTextColor { get; set; }
    }
}