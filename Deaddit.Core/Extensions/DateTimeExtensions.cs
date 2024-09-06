using Deaddit.Core.Reddit.Models;

namespace Deaddit.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static string? Elapsed(this OptionalDateTime dateTime)
        {
            if (!dateTime.HasValue)
            {
                return null;
            }

            return dateTime.Value.Elapsed();
        }

        public static string? Elapsed(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return null;
            }

            return dateTime.Value.Elapsed();
        }

        public static string Elapsed(this DateTime since)
        {
            DateTime now = DateTime.Now.ToUniversalTime();

            TimeSpan difference = now - since;

            int years = (int)(difference.TotalDays / 365);
            if (years > 1)
            {
                return $"{years} years ago";
            }

            if (years > 0)
            {
                return $"{years} year ago";
            }

            int months = (int)(difference.TotalDays / 30);
            if (months > 1)
            {
                return $"{months} months ago";
            }

            if (months > 0)
            {
                return $"{months} month ago";
            }

            int days = (int)difference.TotalDays;
            if (days > 1)
            {
                return $"{days} days ago";
            }

            if (days > 0)
            {
                return $"{days} day ago";
            }

            int hours = (int)difference.TotalHours;
            if (hours > 1)
            {
                return $"{hours} hours ago";
            }

            if (hours > 0)
            {
                return $"{hours} hour ago";
            }

            int minutes = (int)difference.TotalMinutes;
            if (minutes > 1)
            {
                return $"{minutes} minutes ago";
            }

            if (minutes > 0)
            {
                return $"{minutes} minute ago";
            }

            int seconds = (int)difference.TotalSeconds;
            if (seconds > 1)
            {
                return $"{seconds} seconds ago";
            }

            if (seconds > 0)
            {
                return $"{seconds} second ago";
            }

            return "just now";
        }
    }
}