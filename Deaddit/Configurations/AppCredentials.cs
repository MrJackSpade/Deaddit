using Deaddit.Interfaces;

namespace Deaddit.Configurations
{
    using Deaddit.Attributes;
    using System.ComponentModel.DataAnnotations;

    public class AppCredentials : IAppCredentials
    {
        [Display(Name = "App Key", Order = 3)]
        public string? AppKey { get; set; }

        [Input(masked: true)]
        [Display(Name = "App Secret", Order = 4)]
        public string? AppSecret { get; set; }

        [Input(masked: true)]
        [Display(Name = "Password", Order = 2)]
        public string? Password { get; set; }

        [Display(Name = "User Name", Order = 1)]
        public string? UserName { get; set; }
    }
}