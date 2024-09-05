using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Utils.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool NotNullAny(this IEnumerable<object?> enumerable)
        {
            if(enumerable == null)
            {
                return false;
            }

            return enumerable.Any();
        }
    }
}
