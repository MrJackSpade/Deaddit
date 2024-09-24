using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.Pages.Models;

namespace Deaddit.Components.ComponentModels
{
    internal class SubRedditComponentViewModel : BaseViewModel
    {
        public Color PrimaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color SecondaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public string? SubReddit
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public Color TertiaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color TextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public SubRedditComponentViewModel(string? displayString, ApplicationStyling applicationTheme)
        {
            SubReddit = displayString;
            PrimaryColor = applicationTheme.PrimaryColor.ToMauiColor();
            SecondaryColor = applicationTheme.SecondaryColor.ToMauiColor();
            TertiaryColor = applicationTheme.TertiaryColor.ToMauiColor();
            TextColor = applicationTheme.TextColor.ToMauiColor();
        }
    }
}