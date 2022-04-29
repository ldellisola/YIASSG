using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YIASSG.Utils;

public static class StringExtensions
{
    public static string RemoveDiacritics(this string text) 
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    public static string RemoveConsecutiveDuplicatedCharacter(this string text, params char[] chars)
    {
        StringBuilder bld = new();
        char? prev = null;
        
        foreach (var ch in text)
        {
            if (prev is not null && prev == ch && chars.Contains(ch))
                continue;
            bld.Append(ch);
            prev = ch;
        }

        return bld.ToString();
    }

    private static readonly Regex CharacterRemover = new (@"[^a-zA-Z0-9\u00C0-\u00FF\-_\. ]", RegexOptions.Compiled);
    
    public static string PrepareFragment(this string text)
     => CharacterRemover.Replace(text, "")
         .Replace(' ', '-')
         .RemoveDiacritics()
         .RemoveConsecutiveDuplicatedCharacter('-','.','_')
         .TrimEnd('-','.','_')
         .ToLowerInvariant();
}