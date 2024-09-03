namespace Deaddit.MAUI.Extensions
{
    public static class VisualElementExtensions
    {
        public static void TriggerSizeChanged(this VisualElement element)
        {
            element.WidthRequest += 1;
            element.WidthRequest -= 1;
        }
    }
}