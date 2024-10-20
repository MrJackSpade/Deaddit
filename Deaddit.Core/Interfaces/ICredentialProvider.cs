using Deaddit.Core.Configurations.Models;

namespace Deaddit.Core.Interfaces
{
    public interface ICredentialProvider
    {
        bool CanLogIn { get; }

        bool HasCredentials { get; }

        Task<RedditCredentials> GetCredentials();

        void InvalidateCredentials();
    }
}
