namespace Deaddit.Extensions
{
    internal enum ViewPosition
    {
        Above,

        Within,

        Below,

        Unknown
    }

    internal static class ScrollViewExtensions
    {
        public static bool InView(this ScrollView view, VisualElement element, double padding = 0)
        {
            return view.Position(element, padding) == ViewPosition.Within;
        }

        public static ViewPosition Position(this ScrollView view, VisualElement element, double padding = 0)
        {
            if (element.Bounds.Bottom < 0)
            {
                return ViewPosition.Unknown;
            }

            if (element.Bounds.Top > view.ScrollY + view.Height + padding)
            {
                return ViewPosition.Below;
            }

            if (element.Bounds.Bottom < view.ScrollY - padding)
            {
                return ViewPosition.Above;
            }

            return ViewPosition.Within;
        }
    }
}