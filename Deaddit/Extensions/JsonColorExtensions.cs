using Reddit.Api.Models;

namespace Deaddit.Extensions
{
    public static class JsonColorExtensions
    {
        /// <summary>
        /// Converts a JsonColor to a MAUI Color.
        /// Returns null if the JsonColor has no value.
        /// </summary>
        public static Color? ToMauiColor(this JsonColor jsonColor)
        {
            if (!jsonColor.HasValue)
            {
                return null;
            }

            string hex = jsonColor.Value;

            // Remove # prefix if present
            if (hex.StartsWith('#'))
            {
                hex = hex[1..];
            }

            // Parse hex color
            if (hex.Length == 6)
            {
                int red = Convert.ToInt32(hex[..2], 16);
                int green = Convert.ToInt32(hex.Substring(2, 2), 16);
                int blue = Convert.ToInt32(hex.Substring(4, 2), 16);
                return new Color(red, green, blue, 255);
            }
            else if (hex.Length == 8)
            {
                int alpha = Convert.ToInt32(hex[..2], 16);
                int red = Convert.ToInt32(hex.Substring(2, 2), 16);
                int green = Convert.ToInt32(hex.Substring(4, 2), 16);
                int blue = Convert.ToInt32(hex.Substring(6, 2), 16);
                return new Color(red, green, blue, alpha);
            }

            return null;
        }
    }
}
