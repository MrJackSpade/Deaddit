﻿using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public class RedditCredentials
    {
        [EditorDisplay(Name = "App Key", Order = 3)]
        public string? AppKey { get; set; }

        [EditorDisplay(Masked = true, Name = "App Secret", Order = 4)]
        public string? AppSecret { get; set; }

        [EditorDisplay(Masked = true, Name = "Password", Order = 2)]
        public string? Password { get; set; }

        [EditorDisplay(Name = "User Name", Order = 1)]
        public string? UserName { get; set; }
    }
}