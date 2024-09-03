using Deaddit.Configurations.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class ObjectEditorPageViewModel : BaseViewModel
    {
        public ObjectEditorPageViewModel(ApplicationTheme applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor;
            TextColor = applicationTheme.TextColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            TertiaryColor = applicationTheme.TertiaryColor;
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