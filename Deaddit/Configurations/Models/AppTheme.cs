using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public class ApplicationTheme
    {
        [EditorDisplay(Name = "Distinguished Color", Order = 4)]
        public Color DistinguishedColor { get; set; } = Color.Parse("#287F24");

        [EditorDisplay(Name = "Downvote Color", Order = 10)]
        public Color DownvoteColor { get; set; } = Color.Parse("#7193ff");

        [EditorDisplay(Name = "Font Size", Order = 11)]
        public double FontSize { get; set; } = 12;

        [EditorDisplay(Name = "Highlight Color", Order = 5)]
        public Color HighlightColor { get; set; } = Color.Parse("#313E4F");

        [EditorDisplay(Name = "Hyperlink Color", Order = 8)]
        public Color HyperlinkColor { get; set; } = Color.Parse("#75AEB1");

        [EditorDisplay(Name = "Primary Color", Order = 1)]
        public Color PrimaryColor { get; set; } = Color.Parse("#212121");

        [EditorDisplay(Name = "Secondary Color", Order = 2)]
        public Color SecondaryColor { get; set; } = Color.Parse("#303030");

        [EditorDisplay(Name = "Subtext Color", Order = 7)]
        public Color SubTextColor { get; set; } = Color.Parse("#CCCCCC");

        [EditorDisplay(Name = "Tertiary Color", Order = 3)]
        public Color TertiaryColor { get; set; } = Color.Parse("#434343");

        [EditorDisplay(Name = "Text Color", Order = 6)]
        public Color TextColor { get; set; } = Color.Parse("#FFFFFF");

        [EditorDisplay(Name = "Thumbnail Size", Order = 12)]
        public int ThumbnailSize { get; set; } = 75;

        [EditorDisplay(Name = "Upvote Color", Order = 9)]
        public Color UpvoteColor { get; set; } = Color.Parse("#ff4500");

        [EditorDisplay(Name = "Visited Opacity", Order = 13)]
        public double VisitedOpacity { get; set; } = 0.3f;
    }
}