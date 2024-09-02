using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Extensions
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