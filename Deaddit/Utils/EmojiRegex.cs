using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deaddit.Utils
{
    internal class EmojiRegex
    {
        public static readonly Regex EMOJI_REGEXP = new(EMOJI_SEQUENCE, RegexOptions.Compiled);

        public static readonly string EMOJI_SEQUENCE = $"(?:{EMOJI_CHARACTER}(?:{EMOJI_MODIFIER})?{EMOJI_VARIATION_SELECTOR}?{EMOJI_ZWJ}?)+";

        // https://www.unicode.org/Public/emoji/12.1/emoji-data.txt
        private static readonly string EMOJI_CHARACTER = @"[" +
        @"\u0023" +
        @"\u002A" +
        @"\u0030-\u0039" +
        @"\u00A9" +
        @"\u00AE" +
        @"\u203C" +
        @"\u2049" +
        @"\u2122" +
        @"\u2139" +
        @"\u2194-\u2199" +
        @"\u21A9-\u21AA" +
        @"\u231A-\u231B" +
        @"\u2328" +
        @"\u23CF" +
        @"\u23E9-\u23F3" +
        @"\u23F8-\u23FA" +
        @"\u24C2" +
        @"\u25AA-\u25AB" +
        @"\u25B6" +
        @"\u25C0" +
        @"\u25FB-\u25FE" +
        @"\u2600-\u2604" +
        @"\u260E" +
        @"\u2611" +
        @"\u2614-\u2615" +
        @"\u2618" +
        @"\u261D" +
        @"\u2620" +
        @"\u2622-\u2623" +
        @"\u2626" +
        @"\u262A" +
        @"\u262E-\u262F" +
        @"\u2638-\u263A" +
        @"\u2640" +
        @"\u2642" +
        @"\u2648-\u2653" +
        @"\u265F" +
        @"\u2660" +
        @"\u2663" +
        @"\u2665-\u2666" +
        @"\u2668" +
        @"\u267B" +
        @"\u267E" +
        @"\u267F" +
        @"\u2692-\u2694" +
        @"\u2695" +
        @"\u2696-\u2697" +
        @"\u2699" +
        @"\u269B-\u269C" +
        @"\u26A0-\u26A1" +
        @"\u26AA-\u26AB" +
        @"\u26B0-\u26B1" +
        @"\u26BD-\u26BE" +
        @"\u26C4-\u26C5" +
        @"\u26C8" +
        @"\u26CE-\u26CF" +
        @"\u26D1" +
        @"\u26D3-\u26D4" +
        @"\u26E9-\u26EA" +
        @"\u26F0-\u26F5" +
        @"\u26F7-\u26FA" +
        @"\u26FD" +
        @"\u2702" +
        @"\u2705" +
        @"\u2708-\u270D" +
        @"\u270F" +
        @"\u2712" +
        @"\u2714" +
        @"\u2716" +
        @"\u271D" +
        @"\u2721" +
        @"\u2728" +
        @"\u2733-\u2734" +
        @"\u2744" +
        @"\u2747" +
        @"\u274C" +
        @"\u274E" +
        @"\u2753-\u2755" +
        @"\u2757" +
        @"\u2763-\u2764" +
        @"\u2795-\u2797" +
        @"\u27A1" +
        @"\u27B0" +
        @"\u27BF" +
        @"\u2934-\u2935" +
        @"\u2B05-\u2B07" +
        @"\u2B1B-\u2B1C" +
        @"\u2B50" +
        @"\u2B55" +
        @"\u3030" +
        @"\u303D" +
        @"\u3297" +
        @"\u3299" +
        @"\U0001F004" +
        @"\U0001F0CF" +
        @"\U0001F170-\U0001F171" +
        @"\U0001F17E-\U0001F17F" +
        @"\U0001F18E" +
        @"\U0001F191-\U0001F19A" +
        @"\U0001F1E6-\U0001F1FF" +
        @"\U0001F201-\U0001F202" +
        @"\U0001F21A" +
        @"\U0001F22F" +
        @"\U0001F232-\U0001F23A" +
        @"\U0001F250-\U0001F251" +
        @"\U0001F300-\U0001F321" +
        @"\U0001F324-\U0001F393" +
        @"\U0001F396-\U0001F397" +
        @"\U0001F399-\U0001F39B" +
        @"\U0001F39E-\U0001F3F0" +
        @"\U0001F3F3-\U0001F3F5" +
        @"\U0001F3F7-\U0001F4FD" +
        @"\U0001F4FF-\U0001F53D" +
        @"\U0001F549-\U0001F54E" +
        @"\U0001F550-\U0001F567" +
        @"\U0001F56F-\U0001F570" +
        @"\U0001F573-\U0001F579" +
        @"\U0001F57A" +
        @"\U0001F587" +
        @"\U0001F58A-\U0001F58D" +
        @"\U0001F590" +
        @"\U0001F595-\U0001F596" +
        @"\U0001F5A4" +
        @"\U0001F5A5" +
        @"\U0001F5A8" +
        @"\U0001F5B1-\U0001F5B2" +
        @"\U0001F5BC" +
        @"\U0001F5C2-\U0001F5C4" +
        @"\U0001F5D1-\U0001F5D3" +
        @"\U0001F5DC-\U0001F5DE" +
        @"\U0001F5E1" +
        @"\U0001F5E3" +
        @"\U0001F5E8" +
        @"\U0001F5EF" +
        @"\U0001F5F3" +
        @"\U0001F5FA-\U0001F64F" +
        @"\U0001F680-\U0001F6C5" +
        @"\U0001F6CB-\U0001F6D0" +
        @"\U0001F6D1-\U0001F6D2" +
        @"\U0001F6D5" +
        @"\U0001F6E0-\U0001F6E5" +
        @"\U0001F6E9" +
        @"\U0001F6EB-\U0001F6EC" +
        @"\U0001F6F0" +
        @"\U0001F6F3" +
        @"\U0001F6F4-\U0001F6F6" +
        @"\U0001F6F7-\U0001F6F8" +
        @"\U0001F6F9" +
        @"\U0001F6FA" +
        @"\U0001F7E0-\U0001F7EB" +
        @"\U0001F90D-\U0001F90F" +
        @"\U0001F910-\U0001F918" +
        @"\U0001F919-\U0001F91E" +
        @"\U0001F91F" +
        @"\U0001F920-\U0001F927" +
        @"\U0001F928-\U0001F92F" +
        @"\U0001F930" +
        @"\U0001F931-\U0001F932" +
        @"\U0001F933-\U0001F93A" +
        @"\U0001F93C-\U0001F93E" +
        @"\U0001F93F" +
        @"\U0001F940-\U0001F945" +
        @"\U0001F947-\U0001F94B" +
        @"\U0001F94C" +
        @"\U0001F94D-\U0001F94F" +
        @"\U0001F950-\U0001F95E" +
        @"\U0001F95F-\U0001F96B" +
        @"\U0001F96C-\U0001F970" +
        @"\U0001F971" +
        @"\U0001F973-\U0001F976" +
        @"\U0001F97A" +
        @"\U0001F97C-\U0001F97F" +
        @"\U0001F980-\U0001F984" +
        @"\U0001F985-\U0001F991" +
        @"\U0001F992-\U0001F997" +
        @"\U0001F998-\U0001F9A2" +
        @"\U0001F9A5-\U0001F9AA" +
        @"\U0001F9AE-\U0001F9AF" +
        @"\U0001F9B0-\U0001F9B9" +
        @"\U0001F9BA-\U0001F9BF" +
        @"\U0001F9C0-\U0001F9C2" +
        @"\U0001F9C3-\U0001F9CA" +
        @"\U0001F9CD-\U0001F9CF" +
        @"\U0001F9D0-\U0001F9E6" +
        @"\U0001F9E7-\U0001F9FF" +
        @"\U0001FA70-\U0001FA73" +
        @"\U0001FA78-\U0001FA7A" +
        @"\U0001FA80-\U0001FA82" +
        @"\U0001FA90-\U0001FA95" +
        @"\U0001FA96-\U0001FAA8" +
        @"\U0001FAB0-\U0001FAB6" +
        @"\U0001FAC0-\U0001FAC2" +
        @"\U0001FAD0-\U0001FAD6" +
        @"]";

        private static readonly string EMOJI_KEYCAP = @"\u{20E3}";

        private static readonly string EMOJI_MODIFIER = @"[\u{1F3FB}-\u{1F3FF}]";

        private static readonly string EMOJI_MODIFIER_BASE = EMOJI_CHARACTER + @"|[\u{2600}-\u{26FF}]";

        private static readonly string EMOJI_VARIATION_SELECTOR = @"[\u{FE00}-\u{FE0F}]";

        private static readonly string EMOJI_ZWJ = @"\u{200D}";
    }
}