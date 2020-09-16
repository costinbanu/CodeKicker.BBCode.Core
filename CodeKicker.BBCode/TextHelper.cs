namespace CodeKicker.BBCode.Core
{
    public static class TextHelper
    {
        public static (string replaceResult, int offset) ReplaceAtIndex(string haystack, string needle, string replacement, int index)
        {
            if (index + needle.Length > haystack.Length || haystack.Substring(index, needle.Length) != needle)
            {
                return (haystack, 0);
            }
            return (haystack.Insert(index, replacement).Remove(index + replacement.Length, needle.Length), replacement.Length - needle.Length);
        }
    }
}
