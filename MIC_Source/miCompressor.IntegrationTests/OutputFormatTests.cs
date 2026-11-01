using System.Text.Json;
using miCompressor.core;

namespace miCompressor.IntegrationTests;

public class OutputFormatTests
{
    [Fact]
    public void JpegXlHelpers_ReturnExpectedValues()
    {
        Assert.Equal(".jxl", OutputFormat.Jxl.GetOutputExtension("photo.png"));
        Assert.Equal(".JXL", OutputFormat.Jxl.GetOutputExtension("photo.JXL"));
        Assert.Equal("JPEG XL", OutputFormat.Jxl.GetDescription());
        Assert.Equal(OutputFormat.Jxl, OutputFormatHelper.GetOutputFormatFor("photo.JxL"));
        Assert.Contains("jxl", CodeConsts.SupportedOutputExtensions);
    }

    [Fact]
    public void OutputFormatNumericValues_RemainBackwardCompatible()
    {
        Assert.Equal(0, (int)OutputFormat.KeepSame);
        Assert.Equal(1, (int)OutputFormat.Jpg);
        Assert.Equal(2, (int)OutputFormat.Png);
        Assert.Equal(3, (int)OutputFormat.Webp);
        Assert.Equal(4, (int)OutputFormat.avif);
        Assert.Equal(5, (int)OutputFormat.Gif);
        Assert.Equal(6, (int)OutputFormat.Jxl);

        Assert.Equal(OutputFormat.Gif, JsonSerializer.Deserialize<OutputFormat>("5"));
        Assert.Equal(OutputFormat.Jxl, JsonSerializer.Deserialize<OutputFormat>("6"));
        Assert.Equal("6", JsonSerializer.Serialize(OutputFormat.Jxl));
    }

    [Fact]
    public void JpegXlMagickMapping_SupportsQualityAndBalancedEffort()
    {
        Assert.Equal(ImageMagick.MagickFormat.Jxl,
            MagickHelper.GetMagickFormat(OutputFormat.Jxl, "output.jxl", isMultiframed: false));
        Assert.True(MagickHelper.CanSetQuality(OutputFormat.Jxl, "output.jxl"));

        var defines = Assert.IsType<ImageMagick.Formats.JxlWriteDefines>(
            MagickHelper.GetWriteDefinesFor(OutputFormat.Jxl, "output.jxl", false, 80, null!));
        Assert.Equal((int?)7, defines.Effort);
    }

    [Theory]
    [InlineData(1920, 1080, 7)] // Full HD, 2,073,600 pixels
    [InlineData(1920, 1100, 6)] // 2,112,000 pixels
    [InlineData(2800, 1500, 6)] // 4,200,000 pixels
    [InlineData(2800, 1501, 5)] // 4,202,800 pixels
    [InlineData(6000, 4000, 5)] // 24 MP
    public async Task JpegXlEffort_AdaptsToImageSize(int width, int height, int expectedEffort)
    {
        string testDir = Path.Combine(Path.GetTempPath(), "miCompressor-jxl-effort-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        try
        {
            string inputPath = Path.Combine(testDir, $"{width}x{height}.png");
            using (var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.White, (uint)width, (uint)height))
                image.Write(inputPath, ImageMagick.MagickFormat.Png);

            var media = new MediaFileInfo(testDir, new FileInfo(inputPath));
            await TestFileHelper.WaitUntil(() => media.Width == width && media.Height == height, TimeSpan.FromSeconds(5));

            var defines = Assert.IsType<ImageMagick.Formats.JxlWriteDefines>(
                MagickHelper.GetWriteDefinesFor(OutputFormat.Jxl, "output.jxl", false, 80, media));
            Assert.Equal((int?)expectedEffort, defines.Effort);
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }
}
