using System.Text.RegularExpressions;

public static class StringExtensions
{
    /// <summary>
    /// Thanks to https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string PascalToString(this string str)
    {
        return Regex.Replace(str,
            @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))",
            " $0"
            );
    }
}