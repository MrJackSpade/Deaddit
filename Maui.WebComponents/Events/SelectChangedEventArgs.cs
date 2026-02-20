namespace Maui.WebComponents.Events
{
    public class SelectChangedEventArgs : EventArgs
    {
        public string? Value { get; set; }

        public int SelectedIndex { get; set; }
    }
}
