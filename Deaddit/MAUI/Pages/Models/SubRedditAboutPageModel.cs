using Deaddit.Configurations.Models;

namespace Deaddit.MAUI.Pages.Models
{
    internal class SubRedditAboutPageModel : BaseViewModel
    {
        public SubRedditAboutPageModel(ApplicationStyling applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor;
            TextColor = applicationTheme.TextColor;
            PrimaryColor = applicationTheme.PrimaryColor;
            HighlightColor = applicationTheme.HighlightColor;
            TertiaryColor = applicationTheme.TertiaryColor;
            HyperlinkColor = applicationTheme.HyperlinkColor;
            MinHeight = applicationTheme.ThumbnailSize;
            FontSize = applicationTheme.FontSize;
            SubTextColor = applicationTheme.SubTextColor;
        }

        public string? Description
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public double FontSize
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public Color HighlightColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color HyperlinkColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public double MinHeight
        {
            get => this.GetValue<double>();
            set => this.SetValue(value);
        }

        public string? Name
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

        public Color SubTextColor
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

        public string? Thumbnail
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }

        public string? VisibleMetaData
        {
            get => this.GetValue<string>();
            set => this.SetValue(value);
        }
    }
}