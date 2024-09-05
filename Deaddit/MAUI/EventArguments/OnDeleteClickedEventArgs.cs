using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.EventArguments
{
    public class OnDeleteClickedEventArgs(ApiThing thing, ContentView component) : EventArgs
    {
        public ContentView Component { get; set; } = component;

        public ApiThing Thing { get; set; } = thing;
    }
}