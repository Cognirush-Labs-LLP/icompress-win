using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace miCompressor.core;

public static class CustomGlobFilter
{

    /// <summary>
    /// Evaluates one or more glob patterns (separated by '|' or ';') against the file name.
    /// Each glob supports '*', '?', and brace expansion '{a,b}' with nesting.
    /// The compiled regex is anchored to match the entire file name.
    /// </summary>
    public static bool MatchesAnyGlob(string fileName, string globExpression)
    {
        var patterns = globExpression.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in patterns)
        {
            var glob = raw.Trim();
            if (glob.Length == 0) continue;

            if (TryConvertGlobToAnchoredRegex(glob, out var regexPattern))
            {
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                    return true;
            }
            else
            {
                // If brace parsing failed (unbalanced), treat braces literally.
                var literalPattern = ConvertLiteralToAnchoredRegex(glob);
                if (Regex.IsMatch(fileName, literalPattern, RegexOptions.IgnoreCase))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Converts a glob with '*', '?', and brace expansion to an anchored regex (^...$).
    /// Returns false if the brace structure is syntactically invalid (unbalanced).
    /// Escapes all regex metacharacters except the ones we intentionally inject.
    /// Supports backslash escaping to force literals (e.g., '\{', '\}', '\*', '\?').
    /// </summary>
    private static bool TryConvertGlobToAnchoredRegex(string glob, out string regex)
    {
        try
        {
            var sb = new StringBuilder(glob.Length + 8);
            sb.Append('^');
            AppendGlobAsRegex(sb, glob.AsSpan(), 0, out var consumed, allowTopLevel: true);
            if (consumed != glob.Length)
            {
                regex = string.Empty;
                return false; // leftover means unbalanced braces at top level
            }
            sb.Append('$');
            regex = sb.ToString();
            return true;
        }
        catch (FormatException)
        {
            regex = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Recursively appends the regex for the provided glob span.
    /// Handles:
    ///  - '*' => ".*"
    ///  - '?' => '.'
    ///  - '{alt1,alt2,...}' => '(?:<alt1>|<alt2>|...)' with full recursion
    ///  - '\' escapes next char literally
    ///  - all other regex metacharacters are escaped
    /// Throws FormatException for unbalanced braces at the current recursion level.
    /// </summary>
    private static void AppendGlobAsRegex(StringBuilder sb, ReadOnlySpan<char> span, int startIndex, out int consumed, bool allowTopLevel)
    {
        int i = startIndex;

        while (i < span.Length)
        {
            char ch = span[i];

            if (ch == '\\')
            {
                // Escape sequence: include next char literally if present.
                i++;
                if (i < span.Length)
                {
                    EscapeRegexChar(sb, span[i]);
                    i++;
                }
                else
                {
                    // trailing backslash -> treat as literal backslash
                    EscapeRegexChar(sb, '\\');
                }
                continue;
            }

            if (ch == '*')
            {
                sb.Append(".*");
                i++;
                continue;
            }

            if (ch == '?')
            {
                sb.Append('.');
                i++;
                continue;
            }

            if (ch == '{')
            {
                // Parse a brace group -> alternation
                i++; // consume '{'
                AppendBraceAlternation(sb, span, ref i);
                continue;
            }

            if (ch == '}')
            {
                // '}' here means the caller's group has ended.
                if (!allowTopLevel)
                {
                    consumed = i + 1;
                    return;
                }
                // At top level, an unmatched '}' is invalid.
                throw new FormatException("Unbalanced '}' in glob.");
            }

            // Normal char -> escape if regex meta
            EscapeRegexChar(sb, ch);
            i++;
        }

        if (!allowTopLevel)
            throw new FormatException("Unbalanced '{' in glob.");

        consumed = i;
    }

    /// <summary>
    /// Parses a '{...}' group at the current position (span[i] points to first char after '{').
    /// Builds a non-capturing group with '|' separated alternatives, each recursively parsed as glob.
    /// Supports nested braces and backslash escaping.
    /// Throws FormatException for unbalanced or empty alternatives.
    /// </summary>
    private static void AppendBraceAlternation(StringBuilder sb, ReadOnlySpan<char> span, ref int i)
    {
        var altBuilder = new StringBuilder();
        var alts = new System.Collections.Generic.List<string>();

        while (i < span.Length)
        {
            char ch = span[i];

            if (ch == '\\')
            {
                // Preserve literal escaped char within current alt
                i++;
                if (i < span.Length)
                {
                    altBuilder.Append(span[i]);
                    i++;
                }
                else
                {
                    altBuilder.Append('\\');
                }
                continue;
            }

            if (ch == '{')
            {
                // Recurse for nested group
                var nested = new StringBuilder();
                // Build nested by reusing our glob-to-regex on the nested content.
                var temp = new StringBuilder();
                AppendGlobAsRegex(temp, span, i, out var consumed, allowTopLevel: false);
                // temp now contains regex for nested (without anchors).
                nested.Append(temp);
                altBuilder.Append(nested);
                i = consumed; // position after '}'
                continue;
            }

            if (ch == ',')
            {
                // finalize current alt
                alts.Add(GlobAltToRegex(altBuilder.ToString()));
                altBuilder.Clear();
                i++;
                continue;
            }

            if (ch == '}')
            {
                // finalize last alt
                alts.Add(GlobAltToRegex(altBuilder.ToString()));
                altBuilder.Clear();
                i++; // consume '}'

                if (alts.Count == 0)
                    throw new FormatException("Empty brace group in glob.");

                // Build non-capturing group
                sb.Append("(?:");
                for (int k = 0; k < alts.Count; k++)
                {
                    if (k > 0) sb.Append('|');
                    sb.Append(alts[k]);
                }
                sb.Append(')');
                return;
            }

            // regular char inside alt
            altBuilder.Append(ch);
            i++;
        }

        // Ran out without closing '}'
        throw new FormatException("Unbalanced '{' in glob.");
    }

    /// <summary>
    /// Converts a single brace alternative's glob text into a regex fragment by
    /// running it through the same rules (*, ?, escape, braces) without anchors.
    /// </summary>
    private static string GlobAltToRegex(string altGlob)
    {
        var sb = new StringBuilder(altGlob.Length + 8);
        AppendGlobAsRegex(sb, altGlob.AsSpan(), 0, out var consumed, allowTopLevel: true);
        if (consumed != altGlob.Length)
            throw new FormatException("Invalid alternative in brace group.");
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a single character if it is a regex metacharacter.
    /// </summary>
    private static void EscapeRegexChar(StringBuilder sb, char ch)
    {
        // Regex metacharacters that need escaping when used literally
        switch (ch)
        {
            case '.':
            case '^':
            case '$':
            case '+':
            case '(':
            case ')':
            case '{':
            case '}':
            case '[':
            case ']':
            case '|':
            case '\\':
                sb.Append('\\').Append(ch);
                break;
            default:
                sb.Append(ch);
                break;
        }
    }

    /// <summary>
    /// Produces a fully anchored regex that treats the whole input literally
    /// (used as a fallback when brace parsing fails).
    /// Also honors '*' and '?' literally, not as wildcards.
    /// </summary>
    private static string ConvertLiteralToAnchoredRegex(string text)
    {
        var sb = new StringBuilder(text.Length + 8);
        sb.Append('^');
        foreach (var ch in text)
            EscapeRegexChar(sb, ch);
        sb.Append('$');
        return sb.ToString();
    }
}

