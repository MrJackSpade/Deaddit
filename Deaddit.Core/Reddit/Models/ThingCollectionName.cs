﻿using Deaddit.Core.Utils.Validation;

namespace Deaddit.Core.Reddit.Models
{
    public class ThingCollectionName
    {
        public string DisplayName { get; private set; }

        public ThingKind Kind { get; private set; }

        public string RootedName { get; private set; }

        public ThingCollectionName(string name, string rootedname, ThingKind kind)
        {
            Ensure.NotNull(name);
            Ensure.NotNull(rootedname);

            RootedName = rootedname;
            Kind = kind;
            DisplayName = name;
        }

        public ThingCollectionName(string name)
        {
            Ensure.NotNull(name);

            if (string.IsNullOrWhiteSpace(name))
            {
                DisplayName = "Home";
                RootedName = "";
                Kind = ThingKind.Listing;
                return;
            }
            else
            {
                DisplayName = name;
                RootedName = name;
            }

            RootedName = RootedName.Trim('/');

            if (RootedName.Contains('/'))
            {
                DisplayName = RootedName[(RootedName.LastIndexOf('/') + 1)..];
            }

            if (!RootedName.Contains('/'))
            {
                RootedName = "/r/" + RootedName;
            }

            if (!RootedName.StartsWith('/'))
            {
                RootedName = "/" + RootedName;
            }

            if (RootedName.StartsWith("/m"))
            {
                Kind = ThingKind.Listing;
            }
            else if (RootedName.StartsWith("/r"))
            {
                Kind = ThingKind.Subreddit;
            }
            else if (RootedName.StartsWith("/u"))
            {
                Kind = ThingKind.Account;
            }
            else
            {
                throw new NotImplementedException($"Can not determine listing kind for {RootedName}");
            }
        }

        public static implicit operator ThingCollectionName(string name)
        {
            return new(name);
        }
    }
}