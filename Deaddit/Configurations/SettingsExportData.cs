using Deaddit.Core.Configurations.Models;

namespace Deaddit.Configurations
{
    public class SettingsExportData
    {
        public string ExportVersion { get; set; } = "1.0";

        public ApplicationStyling Styling { get; set; } = new();

        public AIConfiguration AIConfiguration { get; set; } = new();

        public BlockConfiguration BlockConfiguration { get; set; } = new();

        public ApplicationHacks ApplicationHacks { get; set; } = new();

        public SavePathConfiguration SavePaths { get; set; } = new();

        public LandingPageConfiguration LandingPageConfiguration { get; set; } = new();

        public List<string> PostHistory { get; set; } = [];

        public string? BearerTokenJson { get; set; }

        public Dictionary<string, string> UserTags { get; set; } = [];
    }
}
