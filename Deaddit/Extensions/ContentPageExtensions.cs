using Deaddit.Core.Extensions;
using System.Runtime.Serialization;

namespace Deaddit.Extensions
{
    public static class ContentPageExtensions
    {
        public static async Task<T?> DisplayActionSheet<T>(this ContentPage page, string? title, string? cancel, string? destruction, Dictionary<T, string?> textOverrides = null) where T : struct, Enum
        {
            //This should all be replaced with something that looks better and uses the proper theme, but this is as good as anything for now

            Dictionary<string, T> buttonValues = [];

            textOverrides ??= [];

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (textOverrides.TryGetValue(value, out string? textOverride))
                {
                    if (!string.IsNullOrWhiteSpace(textOverride))
                    {
                        buttonValues.Add(textOverride, value);
                    }
                }
                else if (value.GetAttribute<EnumMemberAttribute>() is EnumMemberAttribute ea && !string.IsNullOrWhiteSpace(ea.Value))
                {
                    buttonValues.Add(ea.Value, value);
                }
                else
                {
                    buttonValues.Add(value.ToString(), value);
                }
            }

            string result = await page.DisplayActionSheet(title, cancel, destruction, FlowDirection.MatchParent, [.. buttonValues.Keys]);

            if (result is null)
            {
                return null;
            }

            return new T?(buttonValues[result]);
        }
    }
}