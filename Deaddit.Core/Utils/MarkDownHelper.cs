using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils
{
    public static class MarkDownHelper
    {
        public const string MARKDOWN_PATTERN = @"(>!.*?!<|\*\*\*.*?\*\*\*|~~.*?~~|\*\*.*?\*\*|__.*?__|_.*?_|`.*?`|\[.*?\]\(.*?\)|\*.*?\*)";

        /// <summary>
        /// Regex pattern to match URLs that are not already wrapped
        /// </summary>
        public const string URL_PATTERN = @"(?<![\[\(])(?:https?:\/\/[^\s\)\]]+)(?!\))";

        public static string Clean(string? markdown)
        {
            if (markdown is null)
            {
                return string.Empty;
            }

            markdown = EnableLinks(markdown);

            while (markdown.Contains("\n\n\n"))
            {
                markdown = markdown.Replace("\n\n\n", "\n\n");
            }

            return markdown;
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

        public static bool IsBlockQuote(string line)
        {
            string trimmedLine = line.TrimStart();

            if (trimmedLine.Length < 1)
            {
                return false;
            }

            if (trimmedLine.Length > 1)
            {
                if (trimmedLine[1] == '!')
                {
                    //spoiler
                    return false;
                }
            }

            return trimmedLine[0] == '>';
        }

        public static bool IsCodeBlock(string line, out bool isSingleLineCodeBlock)
        {
            isSingleLineCodeBlock = true;

            return line.StartsWith("    ");
        }

        public static bool IsHeadline(string line, out int level)
        {
            level = 0;
            line = line.TrimStart();
            while (level < line.Length && line[level] == '#')
            {
                level++;
            }

            bool isHeadline = level > 0 && level < 7 && line.Length > level && line[level] == ' ';

            if (!isHeadline)
            {
                level = 0;
            }

            return isHeadline;
        }

        public static bool IsHorizontalRule(string line)
        {
            string compactLine = line.Replace(" ", string.Empty);

            return compactLine.Length >= 3 &&
                   (compactLine.All(c => c == '-') || compactLine.All(c => c == '*') || compactLine.All(c => c == '_'));
        }

        public static bool IsImage(string line)
        {
            string trimmedLine = line.TrimStart();

            return trimmedLine.StartsWith("![");
        }

        public static bool IsMarkDown(string? input)
        {
            if (input is null)
            {
                return false;
            }

            string[] lines = Regex.Split(input, @"\r\n?|\n", RegexOptions.Compiled);

            lines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (IsTable(lines, i, out _))
                {
                    return true;
                }

                if (Regex.IsMatch(input, URL_PATTERN))
                {
                    return true;
                }

                if (Regex.IsMatch(input, MARKDOWN_PATTERN))
                {
                    return true;
                }

                if (IsBlockQuote(input))
                {
                    return true;
                }

                if (IsCodeBlock(input, out _))
                {
                    return true;
                }

                if (IsHeadline(input, out _))
                {
                    return true;
                }

                if (IsHorizontalRule(input))
                {
                    return true;
                }

                if (IsImage(input))
                {
                    return true;
                }

                if (IsOrderedList(input, out _))
                {
                    return true;
                }

                if (IsUnorderedList(input))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOrderedList(string line, out int listItemIndex)
        {
            listItemIndex = 0;
            string trimmedLine = line.TrimStart();

            Match match = Regex.Match(trimmedLine, @"^(\d+)\. ");
            if (match.Success)
            {
                listItemIndex = int.Parse(match.Groups[1].Value);
                return true;
            }

            return false;
        }

        public static bool IsTable(string[] lines, int currentIndex, out int tableEndIndex)
        {
            tableEndIndex = currentIndex;

            int barCount = TryGetBars(lines, currentIndex);

            if (barCount == 0)
            {
                return false;
            }

            if (TryGetBars(lines, currentIndex + 1) != barCount &&
               TryGetBars(lines, barCount - 1) != barCount)
            {
                return false;
            }

            for (int i = currentIndex + 1; i < lines.Length; i++)
            {
                if (TryGetBars(lines, i) != barCount)
                {
                    tableEndIndex = i - 1;
                    return true;
                }
            }

            tableEndIndex = lines.Length - 1;
            return true;
        }

        public static bool IsUnorderedList(string line)
        {
            string trimmedLine = line.TrimStart();

            return trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("+ ");
        }

        public static int TryGetBars(string[] lines, int index)
        {
            if (index < 0)
            {
                return -1;
            }

            if (index >= lines.Length)
            {
                return -1;
            }

            return lines[index].Count(c => c == '|');
        }
    }
}