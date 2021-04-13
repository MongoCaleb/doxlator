using System;
using System.Collections.Generic;
using System.Text;

namespace xlator
{
    public class IgnoreList
    {
        private const string marker = "NOXLATE";
        private static List<string> wordList =
            new List<string>()
            {
               "Realm",
               "MongoDB"
            };

        public static string ReplaceIgnoreWords(string input)
        {
            var output = new List<string>();
            var words = input.Split(' ');
            for (int x = 0; x < words.Length; x++)
            {
                var word = words[x];
                if (wordList.Contains(word))
                {
                    output.Add($"{marker}{word}{marker}");
                }
                else
                {
                    output.Add(word);
                }
            }

            return string.Join(" ", output);
        }

        public static string ReAaddIgnoreWords(string input)
        {
            return input.Replace(marker, "");

        }
    }
}
