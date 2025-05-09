﻿using Deaddit.Core.Attributes;
using Reddit.Api.Models;

namespace Deaddit.Core.Configurations.Models
{
    public class ApplicationHacks
    {
        [EditorDisplay(Name = "Comment Emoji Handling", Order = 3)]
        public OptionalStrip CommentEmojiHandling { get; set; } = OptionalStrip.None;

        [EditorDisplay(Name = "Default Region", Order = 4)]
        public Region DefaultRegion { get; set; } = Region.GLOBAL;

        [EditorDisplay(Name = "Flair Image Handling", Order = 1)]
        public FlairImageHandling FlairImageHandling { get; set; } = FlairImageHandling.None;

        [EditorDisplay(Name = "Minimum Comment Body Length", Order = 4)]
        public int MinimumCommentBodyLenth { get; set; } = 0;

        [EditorDisplay(Name = "Minimum Post Body Length", Order = 4)]
        public int MinimumPostBodyLenth { get; set; } = 0;

        [EditorDisplay(Name = "Page Buffer", Order = 6)]
        public int PageBuffer { get; set; } = 2;

        [EditorDisplay(Name = "Page Size", Order = 5)]
        public int PageSize { get; set; } = 25;

        [EditorDisplay(Name = "Title Emoji Handling", Order = 2)]
        public OptionalStrip TitleEmojiHandling { get; set; } = OptionalStrip.None;

        [EditorDisplay(Name = "Title Newlines", Order = 4)]
        public OptionalStrip TitleNewlines { get; set; } = OptionalStrip.None;
    }
}