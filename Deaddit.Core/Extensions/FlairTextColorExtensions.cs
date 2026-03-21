using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Extensions
{
    public static class FlairTextColorExtensions
    {
        public static string ToFlairTextHex(this FlairTextColor flairTextColor, ApplicationStyling applicationStyling)
        {
            return flairTextColor switch
            {
                FlairTextColor.Dark => applicationStyling.FlairDarkTextColor.ToHex(),
                FlairTextColor.Light => applicationStyling.FlairLightTextColor.ToHex(),
                _ => applicationStyling.SubTextColor.ToHex()
            };
        }
    }
}
