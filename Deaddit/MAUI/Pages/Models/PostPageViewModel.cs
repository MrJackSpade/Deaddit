using Deaddit.Configurations.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class PostPageViewModel : BaseViewModel
    {
        public PostPageViewModel(ApplicationTheme applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor;
            TextColor = applicationTheme.TextColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            HighlightColor = applicationTheme.HighlightColor;
            TertiaryColor = applicationTheme.TertiaryColor;
        }

        public Color HighlightColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
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