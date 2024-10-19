using Deaddit.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Utils
{
    public class SelectBoxDisplay(INavigation navigation) : ISelectBoxDisplay
    {
        private readonly INavigation _navigation = navigation;

        public async Task<string> Select(string? title, string?[] options, string? defaultOption = null)
        {
            string[] trimmedOptions = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray()!;

            return await _navigation.NavigationStack[^1].DisplayActionSheet(title, null, null, trimmedOptions);
        }
    }
}
