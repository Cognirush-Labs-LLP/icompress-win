using ImageMagick;
using miCompressor.core;
using System.Diagnostics;

namespace miCompressor.IntegrationTests;

/// <summary>
/// Output must render with the same colours as the source, whether colour information is kept or pixels are converted to sRGB.
/// Apple RGB stands in for any non-sRGB profile (e.g. Display P3): its pixel values look clearly different when read as sRGB.
/// </summary>
[Collection("Image compression")]
public class ColorProfileTests : IDisposable
{
    private const int Width = 128;
    private const int Height = 96;
    // Average colour difference allowed for lossy encoding; reading Apple RGB values as sRGB is off by far more.
    private const double MaxMeanDifference = 3;
    private readonly string _testRoot;

    public ColorProfileTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-color-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [Theory]
    [InlineData(".png", OutputFormat.Png, 80, false)]
    [InlineData(".jpg", OutputFormat.Jpg, 80, false)]
    [InlineData(".png", OutputFormat.Webp, 80, false)]
    [InlineData(".png", OutputFormat.avif, 80, false)]
    [InlineData(".png", OutputFormat.Jxl, 100, false)]
    [InlineData(".jpg", OutputFormat.Jpg, 80, true)]
    [InlineData(".png", OutputFormat.Webp, 80, true)]
    public async Task NonSrgbProfile_IsKept_WhereOutputCanCarryIt(string inputExtension, OutputFormat format, uint quality, bool copyMetadata)
    {
        string inputPath = CreateImage("apple-rgb" + inputExtension, ColorProfiles.AppleRGB, detailed: true);

        string outputPath = await CompressAsync(inputPath, format, quality, copyMetadata);

        using var output = new MagickImage(outputPath);
        Assert.Equal(ColorProfiles.AppleRGB.Description, output.GetColorProfile()?.Description);
        AssertRendersSame(inputPath, outputPath);
    }

    [Theory]
    [InlineData(OutputFormat.Jxl, 80, true)] // lossy JPEG XL can't carry an ICC profile
    [InlineData(OutputFormat.Jxl, 80, false)] // few colours: lossless encode may win and keep the profile
    [InlineData(OutputFormat.Gif, 80, true)]
    [InlineData(OutputFormat.Png, 80, true)] // reducing PNG colours converts to sRGB
    public async Task NonSrgbProfile_RendersSame_WhenConvertedToSrgb(OutputFormat format, uint quality, bool detailed)
    {
        string inputPath = CreateImage("apple-rgb.png", ColorProfiles.AppleRGB, detailed);

        string outputPath = await CompressAsync(inputPath, format, quality, copyMetadata: true, allowLossyPNG: true);

        AssertRendersSame(inputPath, outputPath);
    }

    [Theory]
    [InlineData(MagickFormat.APng, ".png", OutputFormat.Png)]
    [InlineData(MagickFormat.APng, ".png", OutputFormat.Webp)]
    [InlineData(MagickFormat.APng, ".png", OutputFormat.Jxl)]
    [InlineData(MagickFormat.WebP, ".webp", OutputFormat.Webp)]
    [InlineData(MagickFormat.WebP, ".webp", OutputFormat.Gif)]
    [InlineData(MagickFormat.Jxl, ".jxl", OutputFormat.Png)]
    public async Task AnimationWithNonSrgbProfile_RendersSame(MagickFormat inputFormat, string inputExtension, OutputFormat format)
    {
        string inputPath = Path.Combine(_testRoot, "animated" + inputExtension);
        using (var collection = new MagickImageCollection())
        {
            for (int i = 0; i < 3; i++)
            {
                var frame = CreateMagickImage(ColorProfiles.AppleRGB, detailed: true);
                frame.Roll(i * 16, 0); // same colours on every frame, so the first frame's mean stands for all
                frame.AnimationDelay = 20;
                frame.Quality = 100;
                collection.Add(frame);
            }
            collection.Write(inputPath, inputFormat);
        }

        string outputPath = await CompressAsync(inputPath, format, 80, copyMetadata: false);

        using (var output = new MagickImageCollection(outputPath, format == OutputFormat.Png ? MagickFormat.APng : MagickFormat.Unknown))
            Assert.Equal(3, output.Count);
        AssertRendersSame(inputPath, outputPath);
    }

    [Fact]
    public async Task CmykJpeg_RendersSame_InRgbFormats()
    {
        string inputPath = Path.Combine(_testRoot, "cmyk.jpg");
        using (var image = CreateMagickImage(ColorProfiles.AppleRGB, detailed: true))
        {
            image.TransformColorSpace(ColorProfiles.USWebCoatedSWOP);
            image.Write(inputPath, MagickFormat.Jpeg);
        }

        string outputPath = await CompressAsync(inputPath, OutputFormat.Webp, 80, copyMetadata: false);

        AssertRendersSame(inputPath, outputPath);
    }

    [Fact]
    public async Task SrgbProfile_IsDropped_AsViewersAssumeSrgb()
    {
        string inputPath = CreateImage("srgb.png", ColorProfiles.SRGB, detailed: true);

        string outputPath = await CompressAsync(inputPath, OutputFormat.Webp, 80, copyMetadata: false);

        using var output = new MagickImage(outputPath);
        Assert.Null(output.GetColorProfile());
    }

    [Fact]
    public async Task PngGamma_IsKept()
    {
        string inputPath = Path.Combine(_testRoot, "gamma.png");
        using (var image = CreateMagickImage(profile: null, detailed: true))
        {
            image.SetAttribute("gamma", "0.5556"); // 1/1.8, as written by old Mac software
            image.Write(inputPath, MagickFormat.Png);
        }
        Assert.Contains("gAMA", PngChunkTypes(inputPath));

        string outputPath = await CompressAsync(inputPath, OutputFormat.Png, 80, copyMetadata: false);

        Assert.Contains("gAMA", PngChunkTypes(outputPath));
        using var output = new MagickImage(outputPath);
        Assert.Equal(0.5556, output.Gamma, 3);
    }

    private async Task<string> CompressAsync(string inputPath, OutputFormat format, uint quality, bool copyMetadata, bool allowLossyPNG = false)
    {
        var settings = new OutputSettings
        {
            format = format,
            quality = quality,
            allowLossyPNG = allowLossyPNG,
            copyMetadata = copyMetadata,
            retainDateTime = false,
            dimensionStrategy = DimensionReductionStrategy.KeepSame,
            OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
            OutputFolder = Path.Combine(_testRoot, "output-" + Guid.NewGuid().ToString("N")),
        };
        var media = new MediaFileInfo(Path.GetDirectoryName(inputPath)!, new FileInfo(inputPath));
        await TestFileHelper.WaitUntil(() => media.Width == Width && media.Height == Height, TimeSpan.FromSeconds(5));

        string outputPath = await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        Assert.True(File.Exists(outputPath));
        return outputPath;
    }

    private string CreateImage(string fileName, IColorProfile profile, bool detailed)
    {
        string path = Path.Combine(_testRoot, fileName);
        using var image = CreateMagickImage(profile, detailed);
        image.Quality = 95;
        image.Write(path);
        return path;
    }

    // Detailed images have thousands of colours, so lossy encoders are used; flat ones a few colours.
    // Colours stay inside sRGB: lossy JPEG XL shifts the average of colours clipped at the gamut edge.
    private static MagickImage CreateMagickImage(IColorProfile? profile, bool detailed)
    {
        var pixels = new byte[Width * Height * 3];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int i = (y * Width + x) * 3;
                int band = x * 4 / Width;
                pixels[i] = (byte)(120 + band * 25 + (detailed ? (x * y) % 23 : 0));
                pixels[i + 1] = (byte)(140 + (detailed ? (x * 7 + y * 3) % 31 : 0));
                pixels[i + 2] = (byte)(190 - band * 20 + (detailed ? (y * 5) % 19 : 0));
            }
        }

        var image = new MagickImage(pixels, new MagickReadSettings { Width = Width, Height = Height, Format = MagickFormat.Rgb });
        if (profile != null)
            image.SetProfile(profile);
        return image;
    }

    private static void AssertRendersSame(string expectedPath, string actualPath)
    {
        double[] expected = MeanSrgb(expectedPath, colorManaged: true);
        double[] actual = MeanSrgb(actualPath, colorManaged: true);
        for (int c = 0; c < 3; c++)
            Assert.True(Math.Abs(expected[c] - actual[c]) <= MaxMeanDifference,
                $"Channel {c}: expected {expected[c]:F1}, got {actual[c]:F1} ({Path.GetFileName(actualPath)})");

        // Guards the test itself: ignoring the source's colour information must change colours well beyond the tolerance.
        double[] ignoringProfile = MeanSrgb(expectedPath, colorManaged: false);
        Assert.Contains(Enumerable.Range(0, 3), c => Math.Abs(expected[c] - ignoringProfile[c]) > 3 * MaxMeanDifference);
    }

    // Mean colour of the first frame as a colour-managed viewer would show it on an sRGB screen.
    private static double[] MeanSrgb(string path, bool colorManaged)
    {
        using var image = Path.GetExtension(path) == ".png" ? new MagickImage(path, MagickFormat.Png) : new MagickImage(path);
        if (colorManaged)
        {
            // Magick's animated WebP reader drops the profile, so read it with ExifTool.
            IColorProfile? profile = image.GetColorProfile() ?? ReadIccProfile(path);
            if (profile != null)
            {
                image.SetProfile(profile);
                image.TransformColorSpace(ColorProfiles.SRGB);
            }
        }
        else if (image.ColorSpace == ColorSpace.CMYK)
        {
            image.ColorSpace = ColorSpace.sRGB;
        }
        image.Alpha(AlphaAction.Off);
        byte[] rgb = image.GetPixels().ToByteArray(PixelMapping.RGB)!;
        var mean = new double[3];
        for (int i = 0; i < rgb.Length; i++)
            mean[i % 3] += rgb[i];
        return mean.Select(sum => sum / (rgb.Length / 3)).ToArray();
    }

    private static IColorProfile? ReadIccProfile(string path)
    {
        var startInfo = new ProcessStartInfo(Path.Combine(ProcessExecutor.ThirdPartyBasePath, "exiftool", "exiftool.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        foreach (string arg in new[] { "-ICC_Profile", "-b", path })
            startInfo.ArgumentList.Add(arg);

        using Process process = Process.Start(startInfo)!;
        using var icc = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(icc);
        process.WaitForExit();
        return icc.Length > 0 ? new ColorProfile(icc.ToArray()) : null;
    }

    private static List<string> PngChunkTypes(string path)
    {
        byte[] png = File.ReadAllBytes(path);
        var types = new List<string>();
        for (int offset = 8; offset + 12 <= png.Length;)
        {
            int length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            types.Add(System.Text.Encoding.ASCII.GetString(png, offset + 4, 4));
            offset += 12 + length;
        }
        return types;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }
}
