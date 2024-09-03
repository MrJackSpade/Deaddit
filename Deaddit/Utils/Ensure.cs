using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Deaddit.Utils
{
    internal static class Ensure
    {
        public static void NotNull([NotNull] object? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (v is null)
            {
                throw new ArgumentNullException($"{propertyName} can not be null");
            }
        }

        public static void NotNullOrEmpty<T>([NotNull] IList<T>? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (v is null || v.Count == 0)
            {
                throw new ArgumentNullException($"{propertyName} can not be null or empty");
            }
        }

        public static void NotNullOrWhiteSpace([NotNull] string? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                throw new ArgumentException($"{propertyName} can not be null or white space");
            }
        }

        public static void NotNullOrEmpty([NotNull] string? v, [CallerArgumentExpression(nameof(v))] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                throw new ArgumentException($"{propertyName} can not be null or empty");
            }
        }
    }
}
