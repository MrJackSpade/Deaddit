using Deaddit.Attributes;
using Deaddit.Interfaces;
    using System.ComponentModel.DataAnnotations;

namespace Deaddit.Configurations
{

    public class BlockConfiguration : IBlockConfiguration
    {
        [Input(Name = "Block Rules", Order = 0)]
        public List<BlockRule> BlockRules { get; set; } = [];

        [Input(Name = "Block Duplicates")]
        public bool BlockDuplicates { get; set; }
    }
}