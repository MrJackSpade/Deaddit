using Deaddit.Core.Interfaces;

namespace Deaddit.Utils
{
    public class SelectBoxDisplay(INavigation navigation) : ISelectBoxDisplay
    {
        private readonly INavigation _navigation = navigation;

        public async Task<string> Select(string? title, string?[] options, string? defaultOption = null)
        {
            string[] trimmedOptions = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray()!;

            Page page = _navigation.NavigationStack.LastOrDefault(p => p != null)
                     ?? Shell.Current.CurrentPage;

            return await page.DisplayActionSheet(title, null, null, trimmedOptions);
        }
    }
}