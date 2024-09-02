using Deaddit.Interfaces;
using System.Text.RegularExpressions;
using System.Web;

namespace Deaddit.Services
{
    internal class MarkdownService : IMarkDownService
    {
        private readonly IAppTheme _appTheme;

        public MarkdownService(IAppTheme appTheme)
        {
            _appTheme = appTheme;
        }

        public static string EnableLinks(string input)
        {
            // Regex pattern to match URLs that are not already wrapped
            string pattern = @"(?<![\[\(])(?:https?:\/\/[^\s\)\]]+)(?!\))";

            // Find all matches
            var matches = Regex.Matches(input, pattern);

            foreach (Match match in matches)
            {
                string url = match.Value;
                string linkText = url;

                try
                {
                    Uri uri = new(url);
                    string domain = uri.Host.ToLower();

                    // Example of a hardcoded special case based on domain
                    if (domain == "preview.redd.it")
                    {
                        linkText = "[Image]";
                    }
                }
                catch (UriFormatException)
                {
                    // Handle potential errors in URI parsing (though the regex should already ensure valid URIs)
                    continue;
                }

                // Replace the original URL with the markdown format
                string replacement = $"[{linkText}]({url})";
                input = input.Replace(url, replacement);
            }

            return input;
        }

        public string? Clean(string? markdown)
        {
            if (markdown is null)
            {
                return null;
            }

            markdown = HttpUtility.HtmlDecode(markdown);

            markdown = EnableLinks(markdown);

            return markdown;
        }
    }
}