using ImageMagick;
using miCompressor.core;
using System.Diagnostics;

namespace miCompressor.IntegrationTests;

[Collection("Image compression")]
public class JpegXlCompressionTests : IDisposable
{
    private const int SourceWidth = 128;
    private const int SourceHeight = 96;
    private readonly string _testRoot;
    private readonly List<string> _previewOutputs = [];

    public JpegXlCompressionTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-jxl-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [Theory]
    [InlineData(MagickFormat.Png, ".png")]
    [InlineData(MagickFormat.Jpeg, ".jpg")]
    public async Task PngAndJpeg_ConvertToDecodableJpegXl(MagickFormat inputFormat, string inputExtension)
    {
        string inputPath = CreateSourceImage("source" + inputExtension, inputFormat);
        string outputPath = await ConvertForPreviewAsync(inputPath, CreateSettings(OutputFormat.Jxl, 85));

        Assert.Equal(".jxl", Path.GetExtension(outputPath), ignoreCase: true);
        using var output = new MagickImage(outputPath);
        Assert.Equal(MagickFormat.Jxl, output.Format);
        Assert.Equal((uint)SourceWidth, output.Width);
        Assert.Equal((uint)SourceHeight, output.Height);
    }

    [Fact]
    public async Task JpegXl_ResizesThroughExistingPipeline()
    {
        string inputPath = CreateSourceImage("resize.png", MagickFormat.Png);
        OutputSettings settings = CreateSettings(OutputFormat.Jxl, 80);
        settings.dimensionStrategy = DimensionReductionStrategy.Percentage;
        settings.percentageOfLongEdge = 50;

        string outputPath = await ConvertForPreviewAsync(inputPath, settings);

        using var output = new MagickImage(outputPath);
        Assert.Equal((uint)(SourceWidth / 2), output.Width);
        Assert.Equal((uint)(SourceHeight / 2), output.Height);
    }

    [Fact]
    public async Task JpegXlQuality100_IsPixelLossless_AndLowerQualityIsSmaller()
    {
        string inputPath = CreateSourceImage("quality.png", MagickFormat.Png);

        string lowQualityPath = await ConvertForPreviewAsync(inputPath, CreateSettings(OutputFormat.Jxl, 40));
        string losslessPath = await ConvertForPreviewAsync(inputPath, CreateSettings(OutputFormat.Jxl, 100));

        Assert.True(new FileInfo(lowQualityPath).Length < new FileInfo(losslessPath).Length);

        using var original = new MagickImage(inputPath);
        using var lossless = new MagickImage(losslessPath);
        Assert.Equal(
            original.GetPixels().ToByteArray(PixelMapping.RGB),
            lossless.GetPixels().ToByteArray(PixelMapping.RGB));
    }

    [Theory]
    [InlineData(OutputFormat.Jpg, MagickFormat.Jpeg, ".jpg")]
    [InlineData(OutputFormat.Webp, MagickFormat.WebP, ".webp")]
    [InlineData(OutputFormat.avif, MagickFormat.Avif, ".avif")]
    public async Task ExistingLossyFormats_StillEncodeAndDecode(
        OutputFormat outputFormat, MagickFormat expectedMagickFormat, string expectedExtension)
    {
        string inputPath = CreateSourceImage("regression-" + outputFormat + ".png", MagickFormat.Png);
        string outputPath = await ConvertForPreviewAsync(inputPath, CreateSettings(outputFormat, 80));

        Assert.Equal(expectedExtension, Path.GetExtension(outputPath), ignoreCase: true);
        using var output = new MagickImage(outputPath);
        Assert.Equal(expectedMagickFormat, output.Format);
        Assert.Equal((uint)SourceWidth, output.Width);
        Assert.Equal((uint)SourceHeight, output.Height);
    }

    [Fact]
    public async Task JpegMetadata_IsCopiedToJpegXl()
    {
        string inputPath = CreateSourceImage("metadata.jpg", MagickFormat.Jpeg, addArtistMetadata: true);
        Assert.Equal("JPEG XL integration test", ReadMetadataTag(inputPath, "Artist"));
        string outputDirectory = Path.Combine(_testRoot, "metadata-output");
        OutputSettings settings = CreateSettings(OutputFormat.Jxl, 85);
        settings.OutputLocationSettings = OutputLocationSetting.UserSpecificFolder;
        settings.OutputFolder = outputDirectory;
        settings.copyMetadata = true;
        settings.copyIPTC = false;
        settings.copyXMP = false;
        settings.retainDateTime = false;

        var media = await CreateMediaFileInfoAsync(inputPath);
        string outputPath = await new ImageCompressor().ProcessSingleFileAsync(
            media, multipleSelectPaths: false, settings, forPreview: false);

        Assert.True(File.Exists(outputPath));
        Assert.Equal("JPEG XL integration test", ReadMetadataTag(outputPath, "Artist"));
    }

    [Fact]
    public async Task UnwritableOutputLocation_IsReportedAsAccessDenied()
    {
        string inputPath = CreateSourceImage("access-denied.png", MagickFormat.Png);
        string fileUsedAsOutputDirectory = Path.Combine(_testRoot, "not-a-directory");
        File.WriteAllText(fileUsedAsOutputDirectory, "block directory creation");
        OutputSettings settings = CreateSettings(OutputFormat.Jxl, 80);
        settings.OutputLocationSettings = OutputLocationSetting.UserSpecificFolder;
        settings.OutputFolder = fileUsedAsOutputDirectory;

        var media = await CreateMediaFileInfoAsync(inputPath);
        ImageCompressedEventArgs? result = null;
        var compressor = new ImageCompressor();
        compressor.ImageCompressed += (_, args) => result = args;

        await compressor.CompressImagesAsync([media], fromMultipleSelectPath: false, settings, forPreview: false);

        Assert.NotNull(result);
        Assert.Equal(CompressionErrorType.AccessDenied, result.Error);
        Assert.Contains(
            media,
            WarningHelper.Instance.CompressionErrors[CompressionErrorType.AccessDenied]);
    }

    private async Task<string> ConvertForPreviewAsync(string inputPath, OutputSettings settings)
    {
        var media = await CreateMediaFileInfoAsync(inputPath);
        string outputPath = await new ImageCompressor().ProcessSingleFileAsync(
            media, multipleSelectPaths: false, settings, forPreview: true);
        _previewOutputs.Add(outputPath);
        Assert.True(File.Exists(outputPath));
        return outputPath;
    }

    private static async Task<MediaFileInfo> CreateMediaFileInfoAsync(string inputPath)
    {
        var media = new MediaFileInfo(Path.GetDirectoryName(inputPath)!, new FileInfo(inputPath));
        await TestFileHelper.WaitUntil(
            () => media.Width == SourceWidth && media.Height == SourceHeight,
            TimeSpan.FromSeconds(5));
        return media;
    }

    private static OutputSettings CreateSettings(OutputFormat format, uint quality)
    {
        return new OutputSettings
        {
            format = format,
            quality = quality,
            copyMetadata = false,
            retainDateTime = false,
            dimensionStrategy = DimensionReductionStrategy.KeepSame
        };
    }

    private string CreateSourceImage(string fileName, MagickFormat format, bool addArtistMetadata = false)
    {
        string path = Path.Combine(_testRoot, fileName);
        byte[] pixels = CreateDeterministicPixels(SourceWidth, SourceHeight);
        var readSettings = new MagickReadSettings
        {
            Width = SourceWidth,
            Height = SourceHeight,
            Format = MagickFormat.Rgb
        };

        using var image = new MagickImage(pixels, readSettings);
        image.Quality = 94;

        if (addArtistMetadata)
        {
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Artist, "JPEG XL integration test");
            image.SetProfile(exif);
        }

        image.Write(path, format);
        return path;
    }

    private static byte[] CreateDeterministicPixels(int width, int height)
    {
        var pixels = new byte[width * height * 3];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * 3;
                pixels[offset] = (byte)((x * 37 + y * 17 + x * y) & 0xff);
                pixels[offset + 1] = (byte)((x * 11 + y * 43 + x * y * 3) & 0xff);
                pixels[offset + 2] = (byte)((x * 29 + y * 7 + x * y * 5) & 0xff);
            }
        }

        return pixels;
    }

    private static string ReadMetadataTag(string imagePath, string tag)
    {
        string executable = Path.Combine(ProcessExecutor.ThirdPartyBasePath, "exiftool", "exiftool.exe");
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-" + tag);
        startInfo.ArgumentList.Add("-s3");
        startInfo.ArgumentList.Add(imagePath);

        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
        return output.Trim();
    }

    public void Dispose()
    {
        foreach (string outputPath in _previewOutputs)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }

        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }
}

[CollectionDefinition("Image compression", DisableParallelization = true)]
public class ImageCompressionCollection
{
}
