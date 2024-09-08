using Deaddit.Core.Utils;

namespace Deaddit.Core.Reddit.Models
{
    public class ThingCollectionName
    {
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

            if(RootedName.StartsWith("/m"))
            {
                Kind = ThingKind.Listing;
            } else if (RootedName.StartsWith("/r"))
            {
                Kind = ThingKind.Subreddit;
            } else if (RootedName.StartsWith("/u"))
            {
                Kind = ThingKind.Account;
            } else
            {
                throw new NotImplementedException($"Can not determine listing kind for {RootedName}");
            }
        }

        public string DisplayName { get; private set; }

        public ThingKind Kind { get; private set; }

        public string RootedName { get; private set; }

        public static implicit operator ThingCollectionName(string name)
        {
            return new(name);
        }
    }
}