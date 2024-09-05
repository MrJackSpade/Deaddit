using Deaddit.Configurations.Models;
using Deaddit.Exceptions;

namespace Deaddit.Extensions
{
    public static class ApplicationHacksExtensions
    {
        public static string? CleanBody(this ApplicationHacks hacks, string? body)
        {
            if (body is null)
            {
                return null;
            }

            switch (hacks.CommentEmojiHandling)
            {
                case EmojiHandling.None:
                    return body;

                case EmojiHandling.Strip:
                    return RemoveEmojis(body);

                default:
                    throw new EnumNotImplementedException(hacks.CommentEmojiHandling);
            }
        }

        public static string? CleanFlair(this ApplicationHacks hacks, string? flair)
        {
            if (flair is null)
            {
                return null;
            }

            switch (hacks.FlairImageHandling)
            {
                case FlairImageHandling.None:
                    return flair;

                case FlairImageHandling.Strip:
                    return StripTags(flair);

                case FlairImageHandling.Resolve:
                    //TODO: Implement flair image resolving
                    return flair;

                default:
                    throw new EnumNotImplementedException(hacks.FlairImageHandling);
            }
        }

        public static string? CleanTitle(this ApplicationHacks hacks, string? title)
        {
            if (title is null)
            {
                return null;
            }

            switch (hacks.CommentEmojiHandling)
            {
                case EmojiHandling.None:
                    return title;

                case EmojiHandling.Strip:
                    return RemoveEmojis(title).Replace("  ", " ");

                default:
                    throw new EnumNotImplementedException(hacks.CommentEmojiHandling);
            }
        }

        private static string RemoveEmojis(string input)
        {
            // Unicode ranges for most emojis
            System.Text.RegularExpressions.Regex emojiRegex = new(@"[\uD83C-\uDBFF\uDC00-\uDFFF]+");
            return emojiRegex.Replace(input, string.Empty).Trim();
        }

        private static string StripTags(string flair)
        {
            // Regular expression to match alphanumeric strings (and underscores) between colons
            return System.Text.RegularExpressions.Regex.Replace(flair, @":\w+:", string.Empty).Trim();
        }
    }
}