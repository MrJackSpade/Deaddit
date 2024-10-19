using Deaddit.Core.Interfaces;

namespace Deaddit.Core.Utils.MultiSelect
{
    public class MultiSelector(ISelectBoxDisplay selectBoxDisplay)
    {
        private readonly ISelectBoxDisplay _selectBoxDisplay = selectBoxDisplay;

        public async Task Select(string? title, params MultiSelectOption[] optionsAndFuncActions)
        {
            string[] options = optionsAndFuncActions.Select(x => x.DisplayName).ToArray();

            string? result = await _selectBoxDisplay.Select(title, options, null);

            if (result is null)
            {
                return;
            }

            foreach (MultiSelectOption option in optionsAndFuncActions)
            {
                if (option.DisplayName == result)
                {
                    await option.Func();
                    return;
                }
            }
        }
    }
}