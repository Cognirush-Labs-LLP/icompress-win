using System;
using System.Text.RegularExpressions;

namespace miCompressor.core;

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

    [AutoNotify] public string nameGlob = "";
    [AutoNotify] public string nameRegex = "";


    [AutoNotify] public bool applyNameContainsFilter = false;
    [AutoNotify] public bool applyNameStartsWithFilter = false;
    [AutoNotify] public bool applyNameEndsWithFilter = false;
    [AutoNotify] public bool applyNameGlobFilter = false;
    [AutoNotify] public bool applyNameRegexFilter = false;



    [AutoNotify] public bool applySizeFilter = false;

    /// <summary>
    /// Create text representation (For UI purpose) of filters.
    /// </summary>
    /// <returns></returns>
    public string BuildFilterSummary()
    {
        // Name part (only one apply* flag will be true, or all false)
        string? namePart = null;

        if (applyNameContainsFilter && !string.IsNullOrWhiteSpace(nameContains))
            namePart = $"File Name Contains {nameContains}";
        else if (applyNameStartsWithFilter && !string.IsNullOrWhiteSpace(nameStartsWith))
            namePart = $"File Name Starts With {nameStartsWith}";
        else if (applyNameEndsWithFilter && !string.IsNullOrWhiteSpace(nameEndsWith))
            namePart = $"File Name Ends With {nameEndsWith}";
        else if (applyNameGlobFilter && !string.IsNullOrWhiteSpace(nameGlob))
            namePart = $"File Name matches Glob \"{nameGlob}\"";
        else if (applyNameRegexFilter && !string.IsNullOrWhiteSpace(nameRegex))
            namePart = $"File Name matches Regex \"{nameRegex}\"";

        // Size part (optional)
        string? sizePart = null;
        if (applySizeFilter)
        {
            string cmp = sizeComparator switch
            {
                FileSizeComparator.GreaterThan => FileSizeComparator.GreaterThan.GetDescription(),
                FileSizeComparator.LessThan => FileSizeComparator.LessThan.GetDescription(),
                _ => sizeComparator.ToString()
            };

            string unit = FileSizeUnit switch
            {
                FileSizeUnit.B => FileSizeUnit.B.GetDescription(),
                FileSizeUnit.KB => FileSizeUnit.KB.GetDescription(),
                FileSizeUnit.MB => FileSizeUnit.MB.GetDescription(),
                _ => FileSizeUnit.ToString()
            };

            string valueText = FileSizeValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            sizePart = $"File Size {cmp} {valueText} {unit}";
        }

        if (string.IsNullOrWhiteSpace(namePart) && string.IsNullOrWhiteSpace(sizePart))
            return "No filters applied.";

        if (!string.IsNullOrWhiteSpace(namePart) && !string.IsNullOrWhiteSpace(sizePart))
            return $"Selected only (see Filters): {namePart} and {sizePart}.";

        return "Selected only (see Filters): " + (namePart ?? sizePart) + ".";
    }


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
    private double _fileSizeValue;
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
    private FileSizeUnit _fileSizeUnit;
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

    public Filter()
    {
        FileSizeValue = 1;
        FileSizeUnit = FileSizeUnit.MB; //This should always be MB as it is hardcoded in UI to set as MB first. 

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

        // Name Glob Filter
        if (applyNameGlobFilter && !string.IsNullOrWhiteSpace(nameGlob))
        {
            if (!MatchesFilter(fileName, nameGlob, MatchType.Glob))
                return true;
        }

        // Name Regex Filter
        if (applyNameRegexFilter && !string.IsNullOrWhiteSpace(nameRegex))
        {
            if (!MatchesFilter(fileName, nameRegex, MatchType.Regex))
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
                    break;
            }
        }

        // If all enabled filters pass, do not exclude the file.
        return false;
    }

    // Enum to differentiate match types
    private enum MatchType { Contains, StartsWith, EndsWith, Glob, Regex }

    /// <summary>
    /// Determines whether <paramref name="fileName"/> matches <paramref name="filter"/>
    /// according to <paramref name="matchType"/>.
    /// 
    /// Modes:
    /// - Contains / StartsWith / EndsWith: plain, case-insensitive string checks (no tokenization).
    /// - Glob: filename-only matcher supporting '*', '?', and brace expansion '{a,b}' with nesting. Multiple patterns via '|' or ';'.
    /// - Regex: raw user regex (we do not add anchors).
    /// 
    /// Thread-safety: Stateless; safe for concurrent use.
    /// </summary>
    /// <param name="fileName">A file name (not required to be a path). Null/empty -> false.</param>
    /// <param name="filter">
    ///   For Glob, supports: '*', '?', and brace sets '{jpg,png}', including nesting (e.g., "{a,{b,c}}").
    ///   For Regex, provide a .NET regex pattern (invalid regex -> false).
    ///   For literal modes, used as-is. Empty/whitespace filter -> true (no filter).
    /// </param>
    /// <param name="matchType">Contains | StartsWith | EndsWith | Glob | Regex.</param>
    /// <returns>True if matched; otherwise false.</returns>
    private static bool MatchesFilter(string fileName, string filter, MatchType matchType)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        if (string.IsNullOrWhiteSpace(filter))
            return true; // No filter => accept all.

        switch (matchType)
        {
            case MatchType.Contains:
                return fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

            case MatchType.StartsWith:
                return fileName.StartsWith(filter, StringComparison.OrdinalIgnoreCase);

            case MatchType.EndsWith:
                return fileName.EndsWith(filter, StringComparison.OrdinalIgnoreCase);

            case MatchType.Glob:
                return CustomGlobFilter.MatchesAnyGlob(fileName, filter);

            case MatchType.Regex:
                try { return Regex.IsMatch(fileName, filter, RegexOptions.IgnoreCase); }
                catch (ArgumentException) { return false; }

            default:
                return false;
        }
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
            FileSizeComparator.GreaterThan => "Bigger Than",
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
