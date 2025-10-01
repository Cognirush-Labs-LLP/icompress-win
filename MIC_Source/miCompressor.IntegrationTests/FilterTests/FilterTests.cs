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
        filter.ApplyNameGlobFilter = true;
        // Literal mode with token separator (pipe)
        filter.NameGlob = "*abc*|*xyz*";

        Assert.False(filter.DoesQualifyToBeExcluded("file_abc.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("file_xyz.png", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("file_nomatch.jpg", 0));
    }

    [Fact]
    public void TestFileNameContainsFilter_RegularMode_WithStar()
    {
        Filter filter = new();
        filter.ApplyNameGlobFilter = true;
        // Literal mode with token separator (pipe)
        filter.NameGlob = "*.jpg";

        Assert.False(filter.DoesQualifyToBeExcluded("file_abc.jpg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("file_jpg.png", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("file_xyz.png", 0));
    }

    [Fact]
    public void TestFileNameContainsFilter_AdvancedMode_RegexMatch()
    {
        Filter filter = new();
        filter.ApplyNameRegexFilter = true;
        // Contains regex meta character '.' so advanced regex mode is used.
        // Pattern "a.c" will match strings with 'a', any character, then 'c'
        filter.NameRegex = "a.c";

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
        filter.ApplyNameRegexFilter = true;
        // Regex "j.g" matches "j", any character, then "g". The advanced mode will automatically add the end anchor.
        filter.nameRegex = "j.g";

        Assert.False(filter.DoesQualifyToBeExcluded("image.jpg", 0));
        Assert.False(filter.DoesQualifyToBeExcluded("image.jogg", 0));
        Assert.True(filter.DoesQualifyToBeExcluded("image.josg", 0));

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
        filter.ApplyNameGlobFilter = true;
        filter.NameGlob = "*.jpg|*.jpeg"; // literal token mode

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
    public void TestFileNameGlobFilter_MultiplePatterns()
    {
        Filter filter = new();
        filter.ApplyNameGlobFilter = true;
        filter.NameGlob = "*.jpeg|*.png";

        Assert.False(filter.DoesQualifyToBeExcluded("photo.jpeg", 0));       // match
        Assert.False(filter.DoesQualifyToBeExcluded("image.PNG", 0));        // case-insensitive match
        Assert.True(filter.DoesQualifyToBeExcluded("document.txt", 0));      // non-match
    }

    [Fact]
    public void TestFileNameRegexFilter_RawPattern()
    {
        Filter filter = new();
        filter.ApplyNameRegexFilter = true;
        filter.NameRegex = "^a.g$"; // exactly 3 chars, starts with 'a', ends with 'g'

        Assert.False(filter.DoesQualifyToBeExcluded("abg", 0));              // match
        Assert.False(filter.DoesQualifyToBeExcluded("aXg", 0));              // match
        Assert.True(filter.DoesQualifyToBeExcluded("ag", 0));                // too short → non-match
        Assert.True(filter.DoesQualifyToBeExcluded("aZZg", 0));              // too long → non-match
    }


    [Fact]
    public void TestImageFilters_Combination_WebDeveloperAndArtist()
    {
        // Single active name filter (Glob) expressing:
        // - starts with "header"
        // - contains "design"
        // - ends with jpg or jpeg
        Filter filter = new();
        filter.ApplyNameGlobFilter = true;
        filter.NameGlob = "header*design*.{jpg,jpeg}";

        Assert.False(filter.DoesQualifyToBeExcluded("header_design_banner.jpg", 0));   // match
        Assert.True(filter.DoesQualifyToBeExcluded("header_banner.jpg", 0));           // missing "design"
        Assert.True(filter.DoesQualifyToBeExcluded("footer_design_banner.jpg", 0));    // doesn't start with "header"
        Assert.True(filter.DoesQualifyToBeExcluded("header_design_banner.png", 0));    // wrong extension
    }

    [Fact]
    public void TestImageFilters_DateBasedAdvancedRegex_MonthSelection()
    {
        // Single active name filter (Regex) for March 2023 (supports "-" or "_" before 03)
        Filter filter = new();
        filter.ApplyNameRegexFilter = true;
        filter.NameRegex = @"2023[-_]?03";

        Assert.False(filter.DoesQualifyToBeExcluded("DSC_2023-03-15_100000.jpg", 0));  // match
        Assert.False(filter.DoesQualifyToBeExcluded("DSC_20230315_100000.jpg", 0));    // match
        Assert.True(filter.DoesQualifyToBeExcluded("DSC_20230415_100000.jpg", 0));     // non-match
    }
}
