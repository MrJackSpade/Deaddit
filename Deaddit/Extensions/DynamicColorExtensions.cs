using Deaddit.Core.Utils;

namespace Deaddit.Extensions
{
    public static class DynamicColorExtensions
    {
        public static Color ToMauiColor(this DynamicColor dynamicColor)
        {
            if(dynamicColor is null)
            {
                return null;
            }

            return new Color(dynamicColor.Red, dynamicColor.Green, dynamicColor.Blue, dynamicColor.Alpha);
        }
    }
}
