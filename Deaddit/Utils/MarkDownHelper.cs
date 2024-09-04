using System.Text.RegularExpressions;
using System.Web;

namespace Deaddit.Utils
{
    internal static class MarkDownHelper
    {
        /// <summary>
        /// Regex pattern to match URLs that are not already wrapped
        /// </summary>
        public const string URL_PATTERN = @"(?<![\[\(])(?:https?:\/\/[^\s\)\]]+)(?!\))";

        public const string MARKDOWN_PATTERN = @"(>!.*?!<|\*\*\*.*?\*\*\*|~~.*?~~|\*\*.*?\*\*|__.*?__|_.*?_|`.*?`|\[.*?\]\(.*?\)|\*.*?\*)";

        public static bool IsMarkDown(string input)
        {
            if (Regex.IsMatch(input, URL_PATTERN))
            {
                return true;
            }

            if (Regex.IsMatch(input, MARKDOWN_PATTERN))
            {
                return true;
            }

            return false;
        }

        public static string EnableLinks(string input)
        {
            // Find all matches
            MatchCollection matches = Regex.Matches(input, URL_PATTERN);

            foreach (Match match in matches)
            {
                string url = match.Value;

                string linkText = url;

                try
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                    {
                        string domain = uri.Host.ToLower();

                        // Example of a hardcoded special case based on domain
                        if (domain == "preview.redd.it")
                        {
                            linkText = "[Image]";
                        }
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

        public static string Clean(string? markdown)
        {
            if (markdown is null)
            {
                return string.Empty;
            }

            markdown = HttpUtility.HtmlDecode(markdown);

            markdown = EnableLinks(markdown);

            return markdown;
        }
    }
}