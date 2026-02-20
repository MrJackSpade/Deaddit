using Deaddit.Configurations.Ai;
using Deaddit.Core.Attributes;

namespace Deaddit.Configurations
{
    public class AIConfiguration
    {
        [EditorDisplay(Name = "Api Key")]
        public string? ApiKey { get; set; }

        [EditorDisplay(Name = "Prompts")]
        public List<AiPrompt> Prompts { get; set; } = [];
    }
}
