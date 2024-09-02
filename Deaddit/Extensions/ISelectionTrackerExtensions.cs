using Deaddit.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Extensions
{
    internal static class ISelectionTrackerExtensions
    {
        public static void Toggle(this ISelectionTracker selectionTracker, ISelectable item)
        {
            if (selectionTracker.IsSelected(item))
            {
                selectionTracker.Unselect(item);
            }
            else
            {
                selectionTracker.Select(item);
            }
        }
    }
}