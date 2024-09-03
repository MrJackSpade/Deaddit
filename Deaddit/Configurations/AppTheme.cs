using Deaddit.Interfaces;
using Deaddit.Attributes;

namespace Deaddit.Configurations
{
    public class AppTheme : IAppTheme
    {
        [Input(Name = "Distinguished Color", Order = 4)]
        public Color DistinguishedColor { get; set; } = Color.Parse("#287F24");

        [Input(Name = "Downvote Color", Order = 10)]
        public Color DownvoteColor { get; set; } = Color.Parse("#7193ff");

        [Input(Name = "Font Size", Order = 11)]
        public double FontSize { get; set; } = 12;

        [Input(Name = "Highlight Color", Order = 5)]
        public Color HighlightColor { get; set; } = Color.Parse("#313E4F");

        [Input(Name = "Hyperlink Color", Order = 8)]
        public Color HyperlinkColor { get; set; } = Color.Parse("#75AEB1");

        [Input(Name = "Primary Color", Order = 1)]
        public Color PrimaryColor { get; set; } = Color.Parse("#212121");

        [Input(Name = "Secondary Color", Order = 2)]
        public Color SecondaryColor { get; set; } = Color.Parse("#303030");

        [Input(Name = "Subtext Color", Order = 7)]
        public Color SubTextColor { get; set; } = Color.Parse("#CCCCCC");

        [Input(Name = "Tertiary Color", Order = 3)]
        public Color TertiaryColor { get; set; } = Color.Parse("#434343");

        [Input(Name = "Text Color", Order = 6)]
        public Color TextColor { get; set; } = Color.Parse("#FFFFFF");

        [Input(Name = "Thumbnail Size", Order = 12)]
        public int ThumbnailSize { get; set; } = 75;

        [Input(Name = "Upvote Color", Order = 9)]
        public Color UpvoteColor { get; set; } = Color.Parse("#ff4500");
    }
}