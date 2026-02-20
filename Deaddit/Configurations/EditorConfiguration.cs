using Deaddit.Core.Attributes;
using Deaddit.Core.Configurations.Models;

namespace Deaddit.Configurations
{
    public class EditorConfiguration
    {
        [EditorDisplay(Name = "Hacks")]
        public ApplicationHacks ApplicationHacks { get; set; } = new ApplicationHacks();

        [EditorDisplay(Name = "Block Configuration")]
        public BlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        [EditorDisplay(Name = "Credentials")]
        public RedditCredentials Credentials { get; set; } = new RedditCredentials();

        [EditorDisplay(Name = "AI Configuration")]
        public AIConfiguration AIConfiguration { get; set; } = new AIConfiguration();

        [EditorDisplay(Name = "Styling")]
        public ApplicationStyling Styling { get; set; } = new ApplicationStyling();
    }
}