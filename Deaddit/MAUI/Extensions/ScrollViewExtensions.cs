using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.MAUI.Extensions
{
    enum ViewPosition
    {
        Above,
        Within,
        Below,
        Unknown
    }

    internal static class ScrollViewExtensions
    {
        public static ViewPosition Position(this ScrollView view, VisualElement element, double padding = 0)
        {
            if (element.Bounds.Bottom < 0)
            {
                return ViewPosition.Unknown;
            }

            if (element.Bounds.Top > (view.ScrollY + view.Height) + padding)
            {
                return ViewPosition.Below;
            }

            if (element.Bounds.Bottom < view.ScrollY - padding)
            {
                return ViewPosition.Above;
            }

            return ViewPosition.Within;
        }

        public static bool InView(this ScrollView view, VisualElement element, double padding = 0)
        {
            return Position(view, element, padding) == ViewPosition.Within;
        }
    }
}
