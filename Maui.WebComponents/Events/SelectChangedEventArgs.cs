namespace Maui.WebComponents.Events
{
    public class SelectChangedEventArgs : EventArgs
    {
        public int SelectedIndex { get; set; }

        public string? Value { get; set; }
    }
}