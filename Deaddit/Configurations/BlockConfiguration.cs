using Deaddit.Interfaces;

namespace Deaddit.Configurations
{
    using System.ComponentModel.DataAnnotations;

    public class BlockConfiguration : IBlockConfiguration
    {
        [Display(Name = "Block Rules", Order = 0)]
        public List<BlockRule> BlockRules { get; set; } = [];
    }
}