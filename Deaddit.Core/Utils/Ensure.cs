using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Deaddit.Core.Utils
{
    public static class Ensure
    {
        public static T NotNull<T>([NotNull] T? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (v is null)
            {
                throw new ArgumentNullException($"{propertyName} can not be null");
            }

            return v;
        }

        public static void NotNullOrEmpty<T>([NotNull] IList<T>? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (v is null || v.Count == 0)
            {
                throw new ArgumentNullException($"{propertyName} can not be null or empty");
            }
        }

        public static void NotNullOrEmpty([NotNull] string? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                throw new ArgumentException($"{propertyName} can not be null or empty");
            }
        }

        public static void NotNullOrWhiteSpace([NotNull] string? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                throw new ArgumentException($"{propertyName} can not be null or white space");
            }
        }
    }
}