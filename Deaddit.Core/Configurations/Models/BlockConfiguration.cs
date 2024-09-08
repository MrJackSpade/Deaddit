using Deaddit.Core.Attributes;

namespace Deaddit.Core.Configurations.Models
{
    public class BlockConfiguration
    {
        [EditorDisplay(Name = "Block Duplicates")]
        public bool BlockDuplicates { get; set; }

        [EditorDisplay(Name = "Mininum Account Age (Days)")]
        public int MinAccountAgeDays { get; set; }

        [EditorDisplay(Name = "Minimum Comment Karma")]
        public int MinCommentKarma { get; set; }

        [EditorDisplay(Name = "Maximum Link Karma")]
        public int MaxLinkKarma { get; set; }

        [EditorDisplay(Name = "Block Rules", Order = 1)]
        public List<BlockRule> BlockRules { get; set; } = [];
    }
}