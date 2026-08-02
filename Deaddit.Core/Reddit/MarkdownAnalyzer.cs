namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// Inspects markdown for features that affect how it must be submitted to Reddit.
    /// </summary>
    public static class MarkdownAnalyzer
    {
        /// <summary>
        /// True if any line in the markdown starts at blockquote depth 2 or greater
        /// (i.e. begins with "> > "). Reddit's richtext endpoint mishandles nested
        /// blockquotes — callers should route these through the markdown endpoint.
        /// </summary>
        public static bool HasNestedBlockquote(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return false;
            }

            foreach (string line in markdown.Split('\n'))
            {
                int i = 0;
                int depth = 0;

                while (i + 1 < line.Length && line[i] == '>' && line[i + 1] == ' ')
                {
                    depth++;

                    if (depth >= 2)
                    {
                        return true;
                    }

                    i += 2;
                }
            }

            return false;
        }

        /// <summary>
        /// True if the markdown contains an "![image](key)" placeholder. These are
        /// only meaningful on Reddit's richtext endpoint; the markdown endpoint
        /// cannot attach images.
        /// </summary>
        public static bool ContainsImagePlaceholder(string? markdown)
        {
            return !string.IsNullOrEmpty(markdown) && markdown.Contains("![image](");
        }
    }
}
