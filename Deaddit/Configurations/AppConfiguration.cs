using Deaddit.Interfaces;

namespace Deaddit.Configurations
{
    public class AppConfiguration
    {
        public IBlockConfiguration BlockConfiguration { get; set; } = new BlockConfiguration();

        public IAppCredentials Credentials { get; set; } = new AppCredentials();

        public IAppTheme Theme { get; set; } = new AppTheme();
    }
}