namespace Deaddit.Configurations.Models
{
    public class AppConfiguration
    {
        public BlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        public RedditCredentials Credentials { get; set; } = new RedditCredentials();

        public ApplicationTheme Theme { get; set; } = new ApplicationTheme();
    }
}