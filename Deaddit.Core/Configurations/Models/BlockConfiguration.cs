using Deaddit.Core.Attributes;

namespace Deaddit.Core.Configurations.Models
{
    public class BlockConfiguration
    {
        [EditorDisplay(Name = "Duplicate Link Handling")]
        public PostState DuplicateLinkHandling { get; set; }

        [EditorDisplay(Name = "Duplicate Thumb Handling", Description = "Uses the thumbnail ETAG to determine if posts link to the same content, even when the url differs")]
        public PostState DuplicateThumbHandling { get; set; }

        [EditorDisplay(Name = "Duplicate Title Handling")]
        public PostState DuplicateTitleHandling { get; set; }

        [EditorDisplay(Name = "Maximum Link Karma")]
        public int MaxLinkKarma { get; set; }

        [EditorDisplay(Name = "Maximum Link Karma Ratio")]
        public double MaxLinkKarmaRatio { get; set; } = 0;

        [EditorDisplay(Name = "Mininum Account Age (Days)")]
        public int MinAccountAgeDays { get; set; }

        [EditorDisplay(Name = "Minimum Comment Karma")]
        public int MinCommentKarma { get; set; }

        [EditorDisplay(Name = "Blacklist", Order = 1)]
        public BlockList BlackList { get; set; } = new BlockList();

        [EditorDisplay(Name = "Whitelist", Order = 1)]
        public BlockList WhiteList { get; set; } = new BlockList();
    }
}