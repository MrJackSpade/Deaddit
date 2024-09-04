using Deaddit.Extensions;
using System.Runtime.Serialization;

namespace Deaddit.MAUI.Extensions
{
    public static class ContentViewExtensions
    {
        public static async Task<T?> DisplayActionSheet<T>(this ContentView view, string? title, string? cancel, string? destruction, Dictionary<T, string> textOverrides = null) where T : struct, Enum
        {
            //This should all be replaced with something that looks better and uses the proper theme, but this is as good as anything for now

            Page? page = view.FindPage();

            Dictionary<string, T> buttonValues = [];

            textOverrides ??= [];

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (textOverrides.TryGetValue(value, out string textOverride))
                {
                    buttonValues.Add(textOverride, value);
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

        public static async Task<string> DisplayActionSheet(this ContentView view, string? title, string? cancel, string? destruction, params string[] buttons)
        {
            //This should all be replaced with something that looks better and uses the proper theme, but this is as good as anything for now

            Page? page = view.FindPage();

            return await page.DisplayActionSheet(title, cancel, destruction, FlowDirection.MatchParent, buttons);
        }

        public static Page? FindPage(this ContentView view)
        {
            Element toCheck = view;

            while (toCheck != null)
            {
                if (toCheck.Parent is Page parentPage)
                {
                    return parentPage;
                }

                toCheck = toCheck.Parent;
            }

            return null;
        }
    }
}