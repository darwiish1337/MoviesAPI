using System.Text.RegularExpressions;

namespace Movies.Application.Helpers;

public static class SanitizationHelper
{
    // Detects tags like <script>, <img onerror=...>, <iframe>, etc.
    private static readonly Regex XssPattern = new(
        @"<script.*?>.*?</script>|<.*?on\w+\s*=.*?>|<iframe.*?>.*?</iframe>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Detects raw HTML tags
    private static readonly Regex HtmlTagPattern = new(
        @"<[^>]+>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Detects basic SQL injection patterns
    private static readonly Regex SqlInjectionPattern = new(
        @"(--|\b(OR|AND)\b\s+[\w'\s]*=|;|\bSELECT\b|\bINSERT\b|\bDELETE\b|\bUPDATE\b|\bDROP\b|\bUNION\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Whitespace normalizer
    private static readonly Regex ExcessiveWhitespace = new(
        @"\s{2,}", RegexOptions.Compiled);

    public static bool ContainsDangerousContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        return XssPattern.IsMatch(input) ||
               HtmlTagPattern.IsMatch(input) ||
               SqlInjectionPattern.IsMatch(input);
    }

    public static string SanitizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string sanitized = XssPattern.Replace(input, string.Empty);
        sanitized = HtmlTagPattern.Replace(sanitized, string.Empty);
        sanitized = SqlInjectionPattern.Replace(sanitized, string.Empty);
        sanitized = ExcessiveWhitespace.Replace(sanitized, " ");

        return sanitized.Trim();
    }

    public static string RemoveHtml(string input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : HtmlTagPattern.Replace(input, string.Empty);
    }

    public static bool IsSafeText(string input) => !ContainsDangerousContent(input);
}