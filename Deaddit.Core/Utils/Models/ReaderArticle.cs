namespace Deaddit.Core.Utils
{
    /// <summary>
    /// The readable portion of a web page, as extracted by <see cref="ReaderModeExtractor"/>.
    /// </summary>
    public record ReaderArticle(
        string? Title,
        string? Author,
        string? SiteName,
        DateTime? PublicationDate,
        TimeSpan TimeToRead,
        string Content);
}
