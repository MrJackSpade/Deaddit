namespace Deaddit.Configurations
{
    public class AppConfiguration
    {
        public BlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        public AppCredentials Credentials { get; set; } = new AppCredentials();

        public AppTheme Theme { get; set; } = new AppTheme();
    }
}