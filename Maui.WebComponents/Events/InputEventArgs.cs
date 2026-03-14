namespace Maui.WebComponents.Events
{
    public class InputEventArgs : EventArgs
    {
        public bool? Checked { get; set; }

        public string? Value { get; set; }
    }
}