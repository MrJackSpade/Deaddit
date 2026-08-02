using SmartReader;

namespace Deaddit.Core.Utils
{
    /// <summary>
    /// Extracts the readable article body out of a full web page's HTML, using SmartReader
    /// (a port of Mozilla's Readability - the algorithm behind Firefox Reader View).
    /// </summary>
    public static class ReaderModeExtractor
    {
        /// <summary>
        /// Runs the readability algorithm over <paramref name="html"/>.
        /// </summary>
        /// <param name="url">The page's URL. Used to resolve relative links and images.</param>
        /// <param name="html">The page's full HTML.</param>
        /// <returns>The extracted article, or null if the page has no recognizable article body.</returns>
        public static ReaderArticle? Extract(string url, string html)
        {
            //Use the constructor, not Reader.ParseArticle(url, html): that call binds to the
            //ParseArticle(uri, userAgent) overload, which ignores the html and refetches the page.
            Article article = new Reader(url, html).GetArticle();

            if (!article.IsReadable || string.IsNullOrWhiteSpace(article.Content))
            {
                return null;
            }

            return new ReaderArticle(
                article.Title,
                article.Author ?? article.Byline,
                article.SiteName,
                article.PublicationDate,
                article.TimeToRead,
                article.Content);
        }
    }
}
