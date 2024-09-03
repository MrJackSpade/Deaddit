using Deaddit.Interfaces;

namespace Deaddit.Configurations
{
    using Deaddit.Attributes;
    using System.ComponentModel.DataAnnotations;

    public class AppCredentials : IAppCredentials
    {
        [Input(Name = "App Key", Order = 3)]
        public string? AppKey { get; set; }

        [Input(Masked = true, Name = "App Secret", Order = 4)]
        public string? AppSecret { get; set; }

        [Input(Masked = true, Name = "Password", Order = 2)]
        public string? Password { get; set; }

        [Input(Name = "User Name", Order = 1)]
        public string? UserName { get; set; }
    }
}