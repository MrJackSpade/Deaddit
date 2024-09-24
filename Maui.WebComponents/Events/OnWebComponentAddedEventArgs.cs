using Maui.WebComponents.Components;

namespace Maui.WebComponents.Events
{
    internal class OnWebComponentAddedEventArgs(WebComponent added) : EventArgs
    {
        public WebComponent Added { get; } = added ?? throw new ArgumentNullException(nameof(added));
    }
}
