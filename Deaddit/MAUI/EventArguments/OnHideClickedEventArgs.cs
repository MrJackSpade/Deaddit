using Deaddit.MAUI.Components;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.EventArguments
{
    public class OnHideClickedEventArgs(ApiPost post, RedditPostComponent component) : EventArgs
    {
        public RedditPostComponent Component { get; set; } = component;

        public ApiPost Post { get; set; } = post;
    }
}