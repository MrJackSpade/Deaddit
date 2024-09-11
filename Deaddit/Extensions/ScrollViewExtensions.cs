using Deaddit.Pages.Models;

namespace Deaddit.Extensions
{
    internal static class ScrollViewExtensions
    {
        public static ViewPosition Position(this ScrollView view, VisualElement element, double padding = 0)
        {
            return view.Position(element.Bounds, padding);
        }

        public static ViewPosition Position(this ScrollView view, Rect elementBounds, double padding = 0)
        {
            if (elementBounds.Bottom < 0)
            {
                return ViewPosition.Unknown;
            }

            if (elementBounds.Top > view.ScrollY + view.Height + padding)
            {
                return ViewPosition.Below;
            }

            if (elementBounds.Bottom < view.ScrollY - padding)
            {
                return ViewPosition.Above;
            }

            return ViewPosition.Within;
        }
    }
}