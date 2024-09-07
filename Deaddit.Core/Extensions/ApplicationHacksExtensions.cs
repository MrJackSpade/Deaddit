using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Extensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", Justification = "<Pending>")]
    public static class ApplicationHacksExtensions
    {
        public static string? CleanBody(this ApplicationHacks hacks, string? body)
        {
            if (body is null)
            {
                return null;
            }

            return hacks.CommentEmojiHandling switch
            {
                EmojiHandling.None => body,
                EmojiHandling.Strip => RemoveEmojis(body),
                _ => throw new EnumNotImplementedException(hacks.CommentEmojiHandling),
            };
        }

        public static string? CleanFlair(this ApplicationHacks hacks, string? flair)
        {
            if (flair is null)
            {
                return null;
            }

            return hacks.FlairImageHandling switch
            {
                FlairImageHandling.None => flair,
                FlairImageHandling.Strip => StripTags(flair),
                FlairImageHandling.Resolve => flair,//TODO: Implement flair image resolving
                _ => throw new EnumNotImplementedException(hacks.FlairImageHandling),
            };
        }

        public static string? CleanTitle(this ApplicationHacks hacks, string? title)
        {
            if (title is null)
            {
                return null;
            }

            return hacks.CommentEmojiHandling switch
            {
                EmojiHandling.None => title,
                EmojiHandling.Strip => RemoveEmojis(title).Replace("  ", " "),
                _ => throw new EnumNotImplementedException(hacks.CommentEmojiHandling),
            };
        }

        private static string RemoveEmojis(string input)
        {
            // Unicode ranges for most emojis
            return EmojiDetector.Replace(input, string.Empty).Trim();
        }

        private static string StripTags(string flair)
        {
            // Regular expression to match alphanumeric strings (and underscores) between colons
            return System.Text.RegularExpressions.Regex.Replace(flair, @":[a-zA-Z0-9\-_]*:", string.Empty).Trim();
        }
    }
}