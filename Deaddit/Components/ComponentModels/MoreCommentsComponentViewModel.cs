using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.Pages.Models;

namespace Deaddit.Components.ComponentModels
{
    internal class MoreCommentsComponentViewModel : BaseViewModel
    {
        public MoreCommentsComponentViewModel(string viewText, ApplicationStyling applicationTheme)
        {
            TertiaryColor = applicationTheme.TertiaryColor.ToMauiColor();
            SecondaryColor = applicationTheme.SecondaryColor.ToMauiColor();
            PrimaryColor = applicationTheme.PrimaryColor.ToMauiColor();
            TextColor = applicationTheme.TextColor.ToMauiColor();
            MoreText = viewText;
            FontSize = applicationTheme.FontSize;
        }

        public double FontSize
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
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

        public Color TextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }
    }
}