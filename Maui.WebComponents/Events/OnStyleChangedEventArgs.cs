namespace Maui.WebComponents.Events
{
    internal class OnStyleChangedEventArgs : EventArgs
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }
}