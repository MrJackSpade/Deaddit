using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public class BlockConfiguration
    {
        [EditorDisplay(Name = "Block Duplicates")]
        public bool BlockDuplicates { get; set; }

        [EditorDisplay(Name = "Block Rules", Order = 0)]
        public List<BlockRule> BlockRules { get; set; } = [];
    }
}