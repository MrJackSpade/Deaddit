using Maui.WebComponents.Components;

namespace Maui.WebComponents.Events
{
    internal class OnWebComponentRemovedEventArgs(WebComponent removed) : EventArgs
    {
        public WebComponent Removed { get; private set; } = removed ?? throw new ArgumentNullException(nameof(removed));
    }
}