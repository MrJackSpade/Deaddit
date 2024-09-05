using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public class ApplicationConfiguration
    {
        [EditorDisplay(Name = "Hacks")]
        public ApplicationHacks ApplicationHacks { get; set; } = new ApplicationHacks();

        [EditorDisplay(Name = "Block Configuration")]
        public BlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        [EditorDisplay(Name = "Credentials")]
        public RedditCredentials Credentials { get; set; } = new RedditCredentials();

        [EditorDisplay(Name = "Styling")]
        public ApplicationStyling Styling { get; set; } = new ApplicationStyling();
    }
}