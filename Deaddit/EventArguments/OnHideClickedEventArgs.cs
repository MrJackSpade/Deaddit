using Maui.WebComponents.Components;
using Reddit.Api.Models.Api;

namespace Deaddit.EventArguments
{
    public class OnHideClickedEventArgs(ApiThing thing, WebComponent component) : EventArgs
    {
        public WebComponent Component { get; set; } = component;

        public ApiThing Thing { get; set; } = thing;
    }
}