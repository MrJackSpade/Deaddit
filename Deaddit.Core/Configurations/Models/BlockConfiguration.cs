using Deaddit.Core.Attributes;

namespace Deaddit.Core.Configurations.Models
{
    public class BlockConfiguration
    {
        [EditorDisplay(Name = "Block Rules", Order = 1)]
        public List<BlockRule> BlockRules { get; set; } = [];

        [EditorDisplay(Name = "Duplicate Link Handling")]
        public PostState DuplicateLinkHandling { get; set; }

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
    }
}