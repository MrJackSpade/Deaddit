namespace Maui.WebComponents.Events
{
    internal class OnInnerTextChangedEventArgs : EventArgs
    {
        public string Id { get; set; }

        public string InnerText { get; set; }
    }
}