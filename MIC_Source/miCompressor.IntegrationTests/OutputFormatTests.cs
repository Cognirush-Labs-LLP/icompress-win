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
}
