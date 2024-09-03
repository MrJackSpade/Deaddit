using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Extensions
{
    public static class ContentViewExtensions
    {
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

        public static async Task<Nullable<T>> DisplayActionSheet<T>(this ContentView view, string? title, string? cancel, string? destruction) where T : struct, Enum
        {
            //This should all be replaced with something that looks better and uses the proper theme, but this is as good as anything for now

            Page? page = view.FindPage();

            Dictionary<string, T> buttonValues = [];

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (value.GetAttribute<EnumMemberAttribute>() is EnumMemberAttribute ea && !string.IsNullOrWhiteSpace(ea.Value))
                {
                    buttonValues.Add(ea.Value, value);
                } else
                {
                    buttonValues.Add(value.ToString(), value);
                }
            }

            string result = await page.DisplayActionSheet(title, cancel, destruction, FlowDirection.MatchParent, buttonValues.Keys.ToArray());

            if(result is null)
            {
                return null;
            }

            return new Nullable<T>(buttonValues[result]);
        }

        public async static Task<string> DisplayActionSheet(this ContentView view, string? title, string? cancel, string? destruction, params string[] buttons)
        {
            //This should all be replaced with something that looks better and uses the proper theme, but this is as good as anything for now

            Page? page = view.FindPage();

            return await page.DisplayActionSheet(title, cancel, destruction, FlowDirection.MatchParent, buttons);
        }
    }
}
