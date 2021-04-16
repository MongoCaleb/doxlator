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
                "note",
                "tip",
                "warning"
            };

        public static bool ShouldBeTranslated(Ast node)
        {
            if (Helpers.DoNotTranslateTypes.Contains(node.type))
            {
                return false;
            }
            if (node.type == "directive" && Helpers.SpecialTranslateTypes.Contains(node.name))
            {
                return true;
            }
            return true;
        }
    }


}
