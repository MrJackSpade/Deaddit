using Deaddit.Core.Attributes;
using Deaddit.Core.Configurations.Models;
using System.Text.Json.Serialization;

namespace Deaddit.Configurations
{
    public class EditorConfiguration
    {
        [EditorDisplay(Name = "Version", ReadOnly = true, Order = -100)]
        [JsonIgnore]
        public string Version { get; set; } = VersionInfo.Version;

        [EditorDisplay(Name = "AI Configuration")]
        public AIConfiguration AIConfiguration { get; set; } = new AIConfiguration();

        [EditorDisplay(Name = "Hacks")]
        public ApplicationHacks ApplicationHacks { get; set; } = new ApplicationHacks();

        [EditorDisplay(Name = "Block Configuration")]
        public BlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        [EditorDisplay(Name = "Styling")]
        public ApplicationStyling Styling { get; set; } = new ApplicationStyling();
    }
}