using Deaddit.Core.Attributes;

namespace Deaddit.Core.Configurations.Models
{
    public class BlockConfiguration
    {
        [EditorDisplay(Name = "Block Duplicates")]
        public bool BlockDuplicates { get; set; }

        [EditorDisplay(Name = "Block Rules", Order = 0)]
        public List<BlockRule> BlockRules { get; set; } = [];
    }
}