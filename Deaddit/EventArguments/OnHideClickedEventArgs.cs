using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.EventArguments
{
    public class OnHideClickedEventArgs(ApiThing thing, ContentView component) : EventArgs
    {
        public ContentView Component { get; set; } = component;

        public ApiThing Thing { get; set; } = thing;
    }
}