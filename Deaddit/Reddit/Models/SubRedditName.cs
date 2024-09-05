using Deaddit.Utils;

namespace Deaddit.Reddit.Models
{
    public class SubRedditName
    {
        public static implicit operator SubRedditName(string name) => new(name);
        public SubRedditName(string name)
        {
            Ensure.NotNull(name);

            if (string.IsNullOrWhiteSpace(name))
            {
                DisplayName = "Home";
                RootedName = "";
            }

            name = name.Trim('/');

            if (name.Contains('/'))
            {
                DisplayName = name[(name.LastIndexOf('/') + 1)..];
            } else
            {
                DisplayName = name;
            }

            if (!name.Contains('/'))
            {
                RootedName = "/r/" + name;
            }
            else
            {
                RootedName = "/" + name;
            }
        }

        public string DisplayName { get; set; }

        public string RootedName { get; set; }
    }
}