using Deaddit.Configurations.Models;
using Deaddit.MAUI.Pages.Models;

namespace Deaddit.MAUI.Components.ComponentModels
{
    internal class MoreCommentsComponentViewModel : BaseViewModel
    {
        public MoreCommentsComponentViewModel(string viewText, ApplicationTheme applicationTheme)
        {
            TertiaryColor = applicationTheme.TertiaryColor;
            SecondaryColor = applicationTheme.SecondaryColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            TextColor = applicationTheme.TextColor;
            MoreText = viewText;
            FontSize = applicationTheme.FontSize;
        }

        public string MoreText
        {
            get => this.GetValue<string>();
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

        public double FontSize
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public Color TextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }
    }
}