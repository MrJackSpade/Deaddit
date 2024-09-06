﻿using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils
{
    public static class EmojiDetector
    {
        public static bool IsMatch(string str)
        {
            return Regex.IsMatch(str, @"\p{So}|\p{Cs}\p{Cs}(\p{Cf}\p{Cs}\p{Cs})*", RegexOptions.Compiled);
        }

        public static string Replace(string inStr, string replacement)
        {
            return Regex.Replace(inStr, @"\p{So}|\p{Cs}\p{Cs}(\p{Cf}\p{Cs}\p{Cs})*", replacement, RegexOptions.Compiled);
        }
    }
}