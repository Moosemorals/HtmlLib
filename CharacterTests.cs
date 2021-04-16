using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib {
    static class CharacterTests {
        public static bool IsAlpha(int c) => c >= 'a' && c <= 'z'  ||  c >= 'A' && c <= 'Z';

        public static bool IsAlphanumeric(int c) => IsAlpha(c) || IsDigit(c);

        public static bool IsDigit(int c) => c >= '0' && c <= '9';

        public static bool IsHexDigit(int c) => c >= 'A' && c <= 'F'  ||  c >= 'a' && c <= 'f'  ||  c >= '0' && c <= '9';

        public static bool IsLowerAlpha(int c) => c >= 'a' && c <= 'z';

        public static bool IsMatchFor(int c, params int[] chars) => chars.Contains(c);

        public static Predicate<int> BuildMatch(params int[] chars) => c => IsMatchFor(c, chars);

        public static bool IsUpperAlpha(int c) => c >= 'A' && c <= 'Z';

        public static bool IsWhitespace(int c) => c == '\u0009' || c == '\u000A' || c == '\u000C' || c == '\u000D' || c == '\u0020';


    }
}
