using System;

namespace miCompressor.core;
using System.Text.RegularExpressions;

/// <summary>
/// Allows user to apply filter. This selection filter. Means, if conditions match, we include the file.
/// All filters are applied with 'AND' condition. i.e. in order for file to get selected, the file must match ALL applied conditions.
/// </summary>
public partial class Filter
{
    /// <summary>
    /// Can give single value such 'plains' (without quotes) or 'plains|landscape'. We should accept even if user enters regular expression. 
    /// Regular users may just enter one value or value separated by '|'.
    /// Advanced users may enter regular expression.
    /// We support both - intuitively. 
    /// We detect 'regular user' mode and convert it to regular expression while comparing.
    /// Ignored if left empty or <![CDATA[applyNameContainsFilter]]> is false.
    /// </summary>
    [AutoNotify] public string nameContains = "";

    /// <summary>
    /// Similar to <![CDATA[nameContains]]> for advanced user but regular user may be able to utilize this in simplistic manner. 
    /// Ignored if left empty or <![CDATA[applyNameStartsWithFilter]]> is false.
    /// </summary>
    [AutoNotify] public string nameStartsWith = "";
    /// <summary>
    /// Similar to <![CDATA[nameStartsWith]]> but checks for file name ends with for regular user.. 
    /// Ignored if left empty or <![CDATA[applyNameEndsWithFilter]]> is false.
    /// </summary>
    [AutoNotify] public string nameEndsWith = "";

    [AutoNotify] public bool applyNameContainsFilter = false;
    [AutoNotify] public bool applyNameStartsWithFilter = false;
    [AutoNotify] public bool applyNameEndsWithFilter = false;
    [AutoNotify] public bool applySizeFilter = false;

    /// <summary>
    /// Options to complete file selection filter. 
    /// i.e. FileSize GreaterThan 1 MB
    ///  GreaterThan = sizeComparator
    ///  1 = FileSizeValue
    ///  MB = FileSizeUnit
    /// </summary>
    [AutoNotify] public FileSizeComparator sizeComparator = FileSizeComparator.GreaterThan;

    /// <summary>
    /// Value of file size in byte. To be used for comparing with value. This value will be calculated based on FileSizeValue and FileSizeUnit
    /// </summary>
    private ulong _fileSizeInBytes;

    /// <summary>
    /// User enters this value along with FileSizeUnit.
    /// </summary>
    private double _fileSizeValue  = 1;
    public double FileSizeValue
    {
        get { return _fileSizeValue; }
        set
        {
            if (_fileSizeValue == value) return;

            _fileSizeValue = value;
            _fileSizeInBytes = (ulong) Math.Round(value * (ulong)_fileSizeUnit);
            OnPropertyChanged(nameof(FileSizeValue));
        }
    }

    /// <summary>
    /// User enters this value along with FileSizeValue.
    /// </summary>
    private FileSizeUnit _fileSizeUnit = FileSizeUnit.MB;
    public FileSizeUnit FileSizeUnit
    {
        get { return _fileSizeUnit;  }
        set 
        {
            if (_fileSizeUnit == value) return;
            _fileSizeUnit = value;
            _fileSizeInBytes = (ulong)Math.Round(_fileSizeValue * (ulong)value);
            OnPropertyChanged(nameof(FileSizeUnit));
        }
    }

    /// <summary>
    /// All selected filters are applied with AND conditions. If ALL conditions match, the file is set to be included. 
    /// </summary>
    /// <param name="file"></param>
    public void Apply(MediaFileInfo file)
    {
        file.ExcludeAndHide = DoesQualifyToBeExcluded(file.ShortName, file.FileSize);
    }

    /// <summary>
    /// Checks if file fails any of the enabled filters. 
    /// Returns true if any condition is not met (i.e. file should be excluded); 
    /// returns false only if ALL enabled conditions match.
    /// </summary>
    /// <param name="fileName">File name (without path) e.g. abc.jpg</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>True if file does NOT meet one or more enabled filter criteria.</returns>
    public bool DoesQualifyToBeExcluded(string fileName, ulong fileSize)
    {
        // Name Contains Filter
        if (applyNameContainsFilter && !string.IsNullOrWhiteSpace(nameContains))
        {
            if (!MatchesFilter(fileName, nameContains, MatchType.Contains))
                return true;
        }

        // Name StartsWith Filter
        if (applyNameStartsWithFilter && !string.IsNullOrWhiteSpace(nameStartsWith))
        {
            if (!MatchesFilter(fileName, nameStartsWith, MatchType.StartsWith))
                return true;
        }

        // Name EndsWith Filter
        if (applyNameEndsWithFilter && !string.IsNullOrWhiteSpace(nameEndsWith))
        {
            if (!MatchesFilter(fileName, nameEndsWith, MatchType.EndsWith))
                return true;
        }

        // File Size Filter
        if (applySizeFilter)
        {
            switch (sizeComparator)
            {
                case FileSizeComparator.GreaterThan:
                    if (!(fileSize > _fileSizeInBytes))
                        return true;
                    break;
                case FileSizeComparator.LessThan:
                    if (!(fileSize < _fileSizeInBytes))
                        return true;
                    break;
                default:
                    // equal to? HAHAHA
                    break;
            }
        }

        // If all enabled filters pass, do not exclude the file.
        return false;
    }

    // Enum to differentiate match types
    private enum MatchType { Contains, StartsWith, EndsWith }

    /// <summary>
    /// Determines whether the fileName matches the filter criteria.
    /// Depending on the content of the filter string, this method either performs a literal match (with optional token splitting on '|')
    /// or uses a regex-based match.
    /// </summary>
    /// <param name="fileName">The file name to test.</param>
    /// <param name="filter">The filter string (which may be a literal or regex).</param>
    /// <param name="matchType">The type of match: Contains, StartsWith, or EndsWith.</param>
    /// <returns>True if a match is found, otherwise false.</returns>
    private bool MatchesFilter(string fileName, string filter, MatchType matchType)
    {
        // If the filter contains a pipe and does NOT have other regex meta characters, assume literal token mode.
        if (filter.Contains("|") && !ContainsRegexMeta(filter, ignorePipe: true))
        {
            var tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string trimmed = token.Trim();
                if (IsLiteralMatch(fileName, trimmed, matchType))
                    return true;
            }
            return false;
        }

        // If no regex meta characters are found, use literal matching.
        if (!ContainsRegexMeta(filter, ignorePipe: false))
        {
            return IsLiteralMatch(fileName, filter, matchType);
        }
        else
        {
            // Advanced regex mode.
            // For startsWith/endsWith, add anchor if not already present.
            string pattern = filter;
            if (matchType == MatchType.StartsWith && !pattern.StartsWith("^"))
            {
                pattern = "^" + pattern;
            }
            else if (matchType == MatchType.EndsWith && !pattern.EndsWith("$"))
            {
                pattern = pattern + "$";
            }

            try
            {
                return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                // If regex compilation fails, fallback to literal matching.
                return IsLiteralMatch(fileName, filter, matchType);
            }
        }
    }

    /// <summary>
    /// Performs literal matching on the fileName.
    /// </summary>
    /// <param name="fileName">The file name to test.</param>
    /// <param name="text">The literal text to compare.</param>
    /// <param name="matchType">The type of match: Contains, StartsWith, or EndsWith.</param>
    /// <returns>True if the literal condition is satisfied.</returns>
    private bool IsLiteralMatch(string fileName, string text, MatchType matchType)
    {
        switch (matchType)
        {
            case MatchType.Contains:
                return fileName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
            case MatchType.StartsWith:
                return fileName.StartsWith(text, StringComparison.OrdinalIgnoreCase);
            case MatchType.EndsWith:
                return fileName.EndsWith(text, StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the input string contains regex meta characters.
    /// If ignorePipe is true, the '|' character is not considered a meta character.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="ignorePipe">Whether to ignore the pipe character.</param>
    /// <returns>True if any meta character is found; otherwise, false.</returns>
    private bool ContainsRegexMeta(string input, bool ignorePipe)
    {
        // List of common regex meta characters.
        char[] metaChars = ignorePipe
            ? new char[] { '.', '*', '?', '+', '[', ']', '(', ')', '{', '}', '^', '$', '\\' }
            : new char[] { '.', '*', '?', '+', '[', ']', '(', ')', '{', '}', '^', '$', '\\', '|' };

        foreach (char c in metaChars)
        {
            if (input.IndexOf(c) >= 0)
                return true;
        }
        return false;
    }
}

/// <summary>
/// File Size Units, used where UI needs dropdown choice (such as for filters)
/// </summary>
public enum FileSizeUnit
{
     B =         1,
    KB =      1024,
    MB = 1024*1024
}

/// <summary>
/// Extension methods for FileSizeUnit to provide user-friendly names.
/// </summary>
public static class FileSizeUnitExtensions
{
    /// <summary>
    /// Retrieves a user-friendly description for a given FileSizeUnit.
    /// </summary>
    /// <param name="unit">The unit whose name needs to be retrieved.</param>
    /// <returns>Formatted user-friendly name.</returns>
    public static string GetDescription(this FileSizeUnit unit)
    {
        return unit switch
        {
            FileSizeUnit.B => "Bytes",
            FileSizeUnit.KB => "KB",
            FileSizeUnit.MB => "MB",
            _ => unit.ToString() // Fallback (should never happen)
        };
    }
}

/// <summary>
/// For comparing file size. Used in filter. 
/// </summary>
public enum FileSizeComparator
{
    GreaterThan,
    LessThan,
}

/// <summary>
/// Extension methods for FileSizeUnit to provide user-friendly names.
/// </summary>
public static class FileSizeComparatorExtensions
{
    /// <summary>
    /// Retrieves a user-friendly description for a given FileSizeComparator.
    /// </summary>
    /// <param name="comparator">The comparator whose name needs to be retrieved.</param>
    /// <returns>Formatted user-friendly name.</returns>
    public static string GetDescription(this FileSizeComparator comparator)
    {
        return comparator switch
        {
            FileSizeComparator.GreaterThan => "Larger Than",
            FileSizeComparator.LessThan => "Smaller Than",
            _ => comparator.ToString() // Fallback (should never happen)
        };
    }

    /// <summary>
    /// Get mathematical symbol for greater than or less than. 
    /// </summary>
    /// <param name="comparator"></param>
    /// <returns> &gt; or &lt; </returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToSymbol(this FileSizeComparator comparator)
    {
        return comparator switch
        {
            FileSizeComparator.GreaterThan => ">",
            FileSizeComparator.LessThan => "<",
            _ => throw new ArgumentOutOfRangeException(nameof(comparator), "Invalid comparator value.")
        };
    }
}
