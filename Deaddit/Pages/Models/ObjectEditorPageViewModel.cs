using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;

namespace Deaddit.Pages.Models
{
    internal class ObjectEditorPageViewModel : BaseViewModel
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

        public ObjectEditorPageViewModel(ApplicationStyling applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor.ToMauiColor();
            TextColor = applicationTheme.TextColor.ToMauiColor();
            PrimaryColor = applicationTheme.PrimaryColor.ToMauiColor();
            TertiaryColor = applicationTheme.TertiaryColor.ToMauiColor();
        }
    }
}