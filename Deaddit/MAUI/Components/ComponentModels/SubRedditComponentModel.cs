using Deaddit.Configurations.Models;
using Deaddit.MAUI.Pages.Models;

namespace Deaddit.MAUI.Components.ComponentModels
{
    internal class SubRedditComponentViewModel : BaseViewModel
    {
        public SubRedditComponentViewModel(string? displayString, ApplicationTheme applicationTheme)
        {
            SubReddit = displayString;
            PrimaryColor = applicationTheme.PrimaryColor;
            SecondaryColor = applicationTheme.SecondaryColor;
            TertiaryColor = applicationTheme.TertiaryColor;
            TextColor = applicationTheme.TextColor;
        }

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
    }
}