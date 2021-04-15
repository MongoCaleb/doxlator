using System;
using System.Collections.Generic;

namespace xlator
{
    public class Helpers
    {
        public static List<string> DoNotTranslateTypes =
            new List<string>()
            {
                "role",
                "guilabel",
                "ref_role",
                "literal",
                "reference"
            };

        public static List<string> SpecialTranslateTypes =
            new List<string>()
            {
                "directive",
                "note",
                "tip",
                "warning"
            };
    }
}
