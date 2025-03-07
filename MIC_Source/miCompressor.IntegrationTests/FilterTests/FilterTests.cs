using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using miCompressor.core;

namespace miCompressor.IntegrationTests.FilterTests;

public class FilterTests
{
    [Fact]
    public void TestNoFiltersApplied_ReturnsIncluded()
    {
        // When no filters are enabled, the file is always included.
        Filter filter = new();
        bool exclude = filter.DoesQualifyToBeExcluded("anything.jpg", 100);
        Assert.False(exclude);
    }

    // ================================
    // Name Contains Filter Tests
    // ================================
    [Fact]
    public void TestFileNameContainsFilter_RegularMode_Match()
    {
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "test";

        // "mytestfile.jpg" contains "test"
        bool exclude = filter.DoesQualifyToBeExcluded("mytestfile.jpg", 0);
        Assert.False(exclude);
    }

    [Fact]
    public void TestFileNameContainsFilter_RegularMode_NoMatch()
    {
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "test";

        // "nofilematch.jpg" does not contain "test"
        bool exclude = filter.DoesQualifyToBeExcluded("nofilematch.jpg", 0);
        Assert.True(exclude);
    }

    [Fact]
    public void TestFileNameContainsFilter_RegularMode_MultipleTokens()
    {
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        // Literal mode with token separator (pipe)
        filter.NameContains = "abc|xyz";

        Assert.False(filter.DoesQualifyToBeExcluded("file_abc.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("file_xyz.png", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("file_nomatch.jpg", 0));
    }

    [Fact]
    public void TestFileNameContainsFilter_AdvancedMode_RegexMatch()
    {
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        // Contains regex meta character '.' so advanced regex mode is used.
        // Pattern "a.c" will match strings with 'a', any character, then 'c'
        filter.NameContains = "a.c";

        Assert.False(filter.DoesQualifyToBeExcluded("abc.jpg", 0));   // 'b' between a and c
        Assert.False(filter.DoesQualifyToBeExcluded("a-cfile.txt", 0)); // '-' between a and c
        Assert.True(filter.DoesQualifyToBeExcluded("ac.jpg", 0));       // no character between a and c
    }

    // ================================
    // Name StartsWith Filter Tests
    // ================================
    [Fact]
    public void TestFileNameStartsWithFilter_RegularMode()
    {
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "abc";

        // Example given in the question.
        Assert.False(filter.DoesQualifyToBeExcluded("abcdef.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("zabc.jpg", 0));
    }

    [Fact]
    public void TestFileNameStartsWithFilter_AdvancedMode_RegexMatch()
    {
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        // Contains regex meta char, so advanced mode applies.
        // Regex "ab.+" will match any string starting with "ab" followed by at least one character.
        filter.NameStartsWith = "ab.+"; //'.+' is single instruction, it means 1 char.

        Assert.False(filter.DoesQualifyToBeExcluded("abcfile.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("ab", 0)); // fails because ".+" requires at least one char after "ab"
    }

    [Fact]
    public void TestFileNameStartsWithFilter_InvalidRegexFallback()
    {
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        // Intentionally invalid regex; should fall back to literal match.
        filter.NameStartsWith = "abc(";

        // Fallback to literal matching means file name must literally start with "abc("
        Assert.False(filter.DoesQualifyToBeExcluded("abc(.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("abcdef.jpg", 0));
    }

    // ================================
    // Name EndsWith Filter Tests
    // ================================
    [Fact]
    public void TestFileNameEndsWithFilter_RegularMode()
    {
        Filter filter = new();
        filter.ApplyNameEndsWithFilter = true;
        filter.NameEndsWith = "jpg";

        Assert.False(filter.DoesQualifyToBeExcluded("image.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("image.png", 0));
    }

    [Fact]
    public void TestFileNameEndsWithFilter_AdvancedMode_RegexMatch()
    {
        Filter filter = new();
        filter.ApplyNameEndsWithFilter = true;
        // Regex "j.g" matches "j", any character, then "g". The advanced mode will automatically add the end anchor.
        filter.NameEndsWith = "j.g";

        Assert.False(filter.DoesQualifyToBeExcluded("image.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("image.jogg", 0));
    }

    [Fact]
    public void TestFileNameEndsWithFilter_InvalidRegexFallback()
    {
        Filter filter = new();
        filter.ApplyNameEndsWithFilter = true;
        // Invalid regex that should fallback to literal matching.
        filter.NameEndsWith = "abc(";

        // Fallback: file name must literally end with "abc("
        Assert.False(filter.DoesQualifyToBeExcluded("fileabc(", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("fileabc", 0));
    }

    // ================================
    // File Size Filter Tests
    // ================================
    [Fact]
    public void TestFileSizeFilter_GreaterThan()
    {
        Filter filter = new();
        filter.ApplySizeFilter = true;
        filter.FileSizeValue = 1;
        filter.FileSizeUnit = FileSizeUnit.MB;
        // Default comparator is GreaterThan.
        // 1 MB = 1 * 1024*1024 = 1048576 bytes

        // File size equal to threshold should be excluded.
        Assert.True(filter.DoesQualifyToBeExcluded("any.jpg", 1048576));
        // File size greater than threshold should be included.
        Assert.False(filter.DoesQualifyToBeExcluded("any.jpg", 1048576 + 1));
    }

    [Fact]
    public void TestFileSizeFilter_LessThan()
    {
        Filter filter = new();
        filter.ApplySizeFilter = true;
        filter.FileSizeValue = 1;
        filter.FileSizeUnit = FileSizeUnit.MB;
        filter.SizeComparator = FileSizeComparator.LessThan;
        // 1 MB = 1048576 bytes

        // File size less than threshold should be included.
        Assert.False(filter.DoesQualifyToBeExcluded("any.jpg", 1048575));
        // File size equal or greater should be excluded.
        Assert.True(filter.DoesQualifyToBeExcluded("any.jpg", 1048576));
        Assert.True(filter.DoesQualifyToBeExcluded("any.jpg", 1048576 + 1));
    }

    // ================================
    // Combination of Filters Tests
    // ================================
    [Fact]
    public void TestCombination_AllFiltersMatch()
    {
        Filter filter = new();
        // Enable all name filters and a file size filter.
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "test";
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "file";
        filter.ApplyNameEndsWithFilter = true;
        filter.NameEndsWith = "jpg";
        filter.ApplySizeFilter = true;
        filter.FileSizeValue = 1;
        filter.FileSizeUnit = FileSizeUnit.MB;
        filter.SizeComparator = FileSizeComparator.GreaterThan;
        // 1 MB = 1048576 bytes

        // File meets all criteria: contains "test", starts with "file", ends with "jpg", and file size > 1MB.
        Assert.False(filter.DoesQualifyToBeExcluded("file_test_image.jpg", 1048576 + 1));

        // Fail Name Contains filter.
        Assert.True(filter.DoesQualifyToBeExcluded("file_image.jpg", 1048576 + 1));
        // Fail Name StartsWith filter.
        Assert.True(filter.DoesQualifyToBeExcluded("myfile_test_image.jpg", 1048576 + 1));
        // Fail Name EndsWith filter.
        Assert.True(filter.DoesQualifyToBeExcluded("file_test_image.png", 1048576 + 1));
        // Fail File Size filter.
        Assert.True(filter.DoesQualifyToBeExcluded("file_test_image.jpg", 1048576));
    }

    // ================================
    // Whitespace Filter String Tests
    // ================================
    [Fact]
    public void TestWhitespaceFilterStringIsIgnored()
    {
        Filter filter = new();
        // Even though filter flags are enabled, if the filter string is whitespace then the condition is ignored.
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "   ";
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "  ";
        filter.ApplyNameEndsWithFilter = true;
        filter.NameEndsWith = "   ";

        // With all filter strings effectively empty, the file should be included.
        Assert.False(filter.DoesQualifyToBeExcluded("anything.jpg", 0));
    }

    // ------------------------------------------------------------------
    // Literal Mode for Common Image Extensions and Mobile Prefixes
    // ------------------------------------------------------------------
    [Fact]
    public void TestImageEndsWithFilter_Shutterbug_LiteralMode_ExtensionList()
    {
        // A shutterbug typically wants to process only JPEG files.
        // Even as an advanced user, many will simply enter a pipe-separated list.
        Filter filter = new();
        filter.ApplyNameEndsWithFilter = true;
        filter.NameEndsWith = "jpg|jpeg"; // literal token mode

        Assert.False(filter.DoesQualifyToBeExcluded("holiday_photo.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("vacation.jpeg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("selfie.png", 0));
    }

    [Fact]
    public void TestImageStartsWithFilter_MobilePhotos_RegularMode()
    {
        // Most mobile devices name images with a common prefix like "IMG_".
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "IMG_";

        Assert.False(filter.DoesQualifyToBeExcluded("IMG_20230401_100000.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("DSC_20230401_100000.jpg", 0));
    }

    // ------------------------------------------------------------------
    // Regex Mode for Date-Based or Sequence-Based Selections
    // ------------------------------------------------------------------
    [Fact]
    public void TestImageNameContainsFilter_MobileDate_RegexMode()
    {
        // Some shutterbugs use regex to filter images captured on a particular day.
        // For example, many mobile files are named like "IMG_20230315_123456.jpg".
        // A user may wish to select images taken on the 15th day of any month.
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        // Regex: look for "15_" somewhere in the filename
        filter.NameContains = @"15_";

        Assert.False(filter.DoesQualifyToBeExcluded("IMG_20230315_123456.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("IMG_20211215_235959.jpeg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("IMG_20230414_100000.jpg", 0));
    }

    [Fact]
    public void TestImageNameContainsFilter_AdvancedMode_SequenceNumberRegex()
    {
        // A photographer might want to process files that include a sequential number in the name.
        // For example, many cameras output names like "DSC_0001.jpg".
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        // Regex to match a sequence of exactly four digits
        filter.NameContains = @"\d{4}";

        Assert.False(filter.DoesQualifyToBeExcluded("DSC_0001.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("DSC_1234.jpeg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("DSC_12.jpg", 0));
    }

    // ------------------------------------------------------------------
    // Combination Scenarios Reflecting User Workflows
    // ------------------------------------------------------------------
    [Fact]
    public void TestImageFilters_Combination_WebDeveloperAndArtist()
    {
        // A web developer/designer might select banner images that:
        // - Start with "header"
        // - Contain "design" (added by an artist)
        // - And have a common image extension (e.g., jpg or jpeg)
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "header";
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "design";
        filter.ApplyNameEndsWithFilter = true;
        // Users may simply write a literal token list.
        filter.NameEndsWith = "jpg|jpeg";

        Assert.False(filter.DoesQualifyToBeExcluded("header_design_banner.jpg", 0));
        // Each failure in one filter leads to exclusion:
        Assert.True(filter.DoesQualifyToBeExcluded("header_banner.jpg", 0));     // missing "design"
        Assert.True(filter.DoesQualifyToBeExcluded("footer_design_banner.jpg", 0)); // does not start with "header"
        Assert.True(filter.DoesQualifyToBeExcluded("header_design_banner.png", 0)); // wrong extension
    }

    [Fact]
    public void TestImageFilters_Combination_MobilePhotography()
    {
        // A mobile photography workflow:
        // - File names typically start with "IMG_"
        // - User wants files from 2023 (common year token)
        // - User wants only JPEG images.
        Filter filter = new();
        filter.ApplyNameStartsWithFilter = true;
        filter.NameStartsWith = "IMG_";
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = "2023"; // literal token search for year 2023
        filter.ApplyNameEndsWithFilter = true;
        filter.NameEndsWith = "jpg|jpeg"; // literal mode token list for extensions

        Assert.False(filter.DoesQualifyToBeExcluded("IMG_20230315_123456.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("IMG_20231231_235959.jpeg", 0));
        // File from another year should be excluded.
        Assert.True(filter.DoesQualifyToBeExcluded("IMG_20221231_235959.jpg", 0));
    }

    [Fact]
    public void TestImageFilters_DateBasedAdvancedRegex_MonthSelection()
    {
        // A photographer might want to filter images taken in March.
        // Mobile images often include the full date, e.g., "DSC_20230315_100000.jpg".
        // An advanced user might simply write a regex without anchors, like "2023[/-]?03".
        Filter filter = new();
        filter.ApplyNameContainsFilter = true;
        filter.NameContains = @"2023[-_]?03";  // matches 2023 followed by - or _ and then 03

        Assert.False(filter.DoesQualifyToBeExcluded("DSC_2023-03-15_100000.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("DSC_20230315_100000.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("DSC_20230415_100000.jpg", 0));
    }
}
