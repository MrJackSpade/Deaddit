using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;

namespace Deaddit.Pages.Models
{
    internal class SubRedditAboutPageModel : BaseViewModel
    {
        public SubRedditAboutPageModel(ApplicationStyling applicationTheme)
        {
            SecondaryColor = applicationTheme.SecondaryColor.ToMauiColor();
            TextColor = applicationTheme.TextColor.ToMauiColor();
            PrimaryColor = applicationTheme.PrimaryColor.ToMauiColor();
            HighlightColor = applicationTheme.HighlightColor.ToMauiColor();
            TertiaryColor = applicationTheme.TertiaryColor.ToMauiColor();
            HyperlinkColor = applicationTheme.HyperlinkColor.ToMauiColor();
            MinHeight = applicationTheme.ThumbnailSize;
            FontSize = applicationTheme.FontSize;
            SubTextColor = applicationTheme.SubTextColor.ToMauiColor();
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