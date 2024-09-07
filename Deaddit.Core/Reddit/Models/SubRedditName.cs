﻿using Deaddit.Core.Utils;

namespace Deaddit.Core.Reddit.Models
{
    public class SubRedditName
    {
        public SubRedditName(string name)
        {
            Ensure.NotNull(name);

            if (string.IsNullOrWhiteSpace(name))
            {
                DisplayName = "Home";
                RootedName = "";
                return;
            }

            name = name.Trim('/');

            if (name.Contains('/'))
            {
                DisplayName = name[(name.LastIndexOf('/') + 1)..];
            }
            else
            {
                DisplayName = name;
            }

            if (!name.Contains('/'))
            {
                RootedName = "/r/" + name;
            }

            if (name.Count(c => c == '/') == 1)
            {
                RootedName = $"/{name}";
            }
        }

        public string DisplayName { get; set; }

        public string RootedName { get; set; }

        public static implicit operator SubRedditName(string name)
        {
            return new(name);
        }
    }
}