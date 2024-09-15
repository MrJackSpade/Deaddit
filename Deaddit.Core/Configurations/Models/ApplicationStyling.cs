using Deaddit.Core.Attributes;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Configurations.Models
{
    public class ApplicationStyling
    {
        [EditorDisplay(Name = "Distinguished Title Color", Order = 4)]
        public DynamicColor DistinguishedTitleColor { get; set; } = DynamicColor.Parse("#287F24");

        [EditorDisplay(Name = "Distinguished Author Text Color", Order = 4)]
        public DynamicColor DistinguishedAuthorTextColor { get; set; } = DynamicColor.Parse("#FFFFFF");

        [EditorDisplay(Name = "Distinguished Autho Background Color", Order = 4)]
        public DynamicColor DistinguishedAuthorBackgroundColor { get; set; } = DynamicColor.Parse("#287F24");

        [EditorDisplay(Name = "Downvote Color", Order = 10)]
        public DynamicColor DownvoteColor { get; set; } = DynamicColor.Parse("#7193ff");

        [EditorDisplay(Name = "Font Size", Order = 11)]
        public double FontSize { get; set; } = 12;

        [EditorDisplay(Name = "Highlight Color", Order = 5)]
        public DynamicColor HighlightColor { get; set; } = DynamicColor.Parse("#313E4F");

        [EditorDisplay(Name = "Hyperlink Color", Order = 8)]
        public DynamicColor HyperlinkColor { get; set; } = DynamicColor.Parse("#75AEB1");

        [EditorDisplay(Name = "Primary Color", Order = 1)]
        public DynamicColor PrimaryColor { get; set; } = DynamicColor.Parse("#212121");

        [EditorDisplay(Name = "Secondary Color", Order = 2)]
        public DynamicColor SecondaryColor { get; set; } = DynamicColor.Parse("#303030");

        [EditorDisplay(Name = "Subtext Color", Order = 7)]
        public DynamicColor SubTextColor { get; set; } = DynamicColor.Parse("#CCCCCC");

        [EditorDisplay(Name = "Tertiary Color", Order = 3)]
        public DynamicColor TertiaryColor { get; set; } = DynamicColor.Parse("#434343");

        [EditorDisplay(Name = "Text Color", Order = 6)]
        public DynamicColor TextColor { get; set; } = DynamicColor.Parse("#FFFFFF");

        [EditorDisplay(Name = "Thumbnail Size", Order = 12)]
        public int ThumbnailSize { get; set; } = 75;

        [EditorDisplay(Name = "Upvote Color", Order = 9)]
        public DynamicColor UpvoteColor { get; set; } = DynamicColor.Parse("#ff4500");

        [EditorDisplay(Name = "Visited Opacity", Order = 13)]
        public double VisitedOpacity { get; set; } = 0.3f;

        [EditorDisplay(Name = "Blocked Background Color", Order = 13)]
        public DynamicColor BlockedBackgroundColor { get; set; } = DynamicColor.Parse("#3b1f1f");

        [EditorDisplay(Name = "Op Background Color")]
        public DynamicColor OpBackgroundColor { get; set; } = DynamicColor.Parse("#0055df");

        [EditorDisplay(Name = "Op Text Color")]
        public DynamicColor OpTextColor { get; set; } = DynamicColor.Parse("#FFFFFF");
    }
}