using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Pages;

namespace Deaddit.Utils.Login
{
    internal class BrowserLoginProvider(INavigation navigation, RedditCredentials credentials) : ICredentialProvider
    {
        private readonly RedditCredentials _credentials = credentials;

        private readonly INavigation _navigation = navigation;

        private bool _validCredentials = credentials.Valid;

        public bool CanLogIn => true;

        public bool HasCredentials => _credentials.Valid && _validCredentials;

        public async Task<RedditCredentials> GetCredentials()
        {
            if (_credentials.Valid)
            {
                return _credentials;
            }

            LoginPage loginPage = new();

            await _navigation.PushAsync(loginPage);

            RedditCredentials credentials = await loginPage.Login();

            if (credentials.Valid)
            {
                _validCredentials = true;
                _credentials.AppSecret = credentials.AppSecret;
                _credentials.AppKey = credentials.AppKey;
                _credentials.UserName = credentials.UserName;
                _credentials.Password = credentials.Password;
            }

            return _credentials;
        }

        public void InvalidateCredentials()
        {
            _validCredentials = false;
        }
    }
}