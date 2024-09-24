using Maui.WebComponents.Components;

namespace Maui.WebComponents.Events
{
    internal class OnWebComponentInsertEventArgs(WebComponent added, int index) : EventArgs
    {
        public WebComponent Added { get; } = added ?? throw new ArgumentNullException(nameof(added));

        public int Index { get; } = index;
    }
}