using Deaddit.Core.Attributes;

namespace Deaddit.Core.Configurations.Models
{
    public class BlockList
    {
        [EditorDisplay(Name = "Rules")]
        public List<BlockRule> Rules { get; set; } = [];
    }
}