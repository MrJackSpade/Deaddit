using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public enum EmojiHandling
    {
        None,

        Strip
    }

    public enum FlairImageHandling
    {
        None,

        Strip,

        Resolve
    }

    public class ApplicationHacks
    {
        [EditorDisplay(Name = "Comment Emoji Handling", Order = 3)]
        public EmojiHandling CommentEmojiHandling { get; set; } = EmojiHandling.None;

        [EditorDisplay(Name = "Flair Image Handling", Order = 1)]
        public FlairImageHandling FlairImageHandling { get; set; } = FlairImageHandling.None;

        [EditorDisplay(Name = "Title Emoji Handling", Order = 2)]
        public EmojiHandling TitleEmojiHandling { get; set; } = EmojiHandling.None;
    }
}