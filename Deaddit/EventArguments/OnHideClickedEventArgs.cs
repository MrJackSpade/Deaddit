using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.EventArguments
{
    public class OnHideClickedEventArgs(ApiThing thing, WebComponent component) : EventArgs
    {
        public WebComponent Component { get; set; } = component;

        public ApiThing Thing { get; set; } = thing;
    }
}