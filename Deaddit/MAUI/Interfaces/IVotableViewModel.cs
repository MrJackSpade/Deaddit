namespace Deaddit.MAUI.Interfaces
{
    internal interface IVotableViewModel
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