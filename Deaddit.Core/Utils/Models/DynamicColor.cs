namespace Deaddit.Core.Utils.Models
{
    public class DynamicColor(int red, int green, int blue, int alpha)
    {
        public int Alpha { get; set; } = alpha;

        public int Blue { get; set; } = blue;

        public int Green { get; set; } = green;

        public int Red { get; set; } = red;

        public static DynamicColor Parse(string str)
        {
            string cleaned = str.Trim('#');

            if (cleaned.Length == 6)
            {
                int red = Convert.ToInt32(cleaned[..2], 16);
                int green = Convert.ToInt32(cleaned.Substring(2, 2), 16);
                int blue = Convert.ToInt32(cleaned.Substring(4, 2), 16);
                return new DynamicColor(red, green, blue, 255);
            }
            else if (cleaned.Length == 8)
            {
                int alpha = Convert.ToInt32(cleaned[..2], 16);
                int red = Convert.ToInt32(cleaned.Substring(2, 2), 16);
                int green = Convert.ToInt32(cleaned.Substring(4, 2), 16);
                int blue = Convert.ToInt32(cleaned.Substring(6, 2), 16);
                return new DynamicColor(red, green, blue, alpha);
            }
            else
            {
                throw new ArgumentException("Invalid color string format. Must be either 6 or 8 characters long (excluding '#').");
            }
        }

        public static bool TryParse(string v, out DynamicColor longValue)
        {
            if (v.Trim('#').Length is 6 or 8)
            {
                longValue = Parse(v);
                return true;
            }
            else
            {
                longValue = null;
                return false;
            }
        }

        public string ToArgbHex()
        {
            return $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
        }

        public string ToHex()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }

        public override string ToString()
        {
            return this.ToHex();
        }
    }
}