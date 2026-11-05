using ImageMagick;
using miCompressor.core;
using System.Runtime.InteropServices;

namespace miCompressor.IntegrationTests;

[Collection("Image compression")]
public class IcoOutputTests : IDisposable
{
    private static readonly int[] AllSizes = [256, 128, 96, 64, 48, 40, 32, 24, 20, 16];
    private const byte PngColorTypeRgba = 6;
    private readonly string _testRoot;

    public IcoOutputTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-ico-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [Fact]
    public void Helpers_ReturnExpectedValues()
    {
        Assert.Equal(".ico", OutputFormat.Ico.GetOutputExtension("photo.png"));
        Assert.Equal("Icon", OutputFormat.Ico.GetDescription());
        Assert.Equal(OutputFormat.Ico, OutputFormatHelper.GetOutputFormatFor("photo.ICO"));
    }

    [Fact]
    public async Task LargeImage_GetsAllSizes_AsRgbaPng()
    {
        string input = CreateImage("large.png", 800, 800);

        var (_, output) = await CompressAsync(input);

        Assert.Equal(AllSizes, ReadEntries(output).Select(e => e.Size));
        Assert.All(ReadEntries(output), e => Assert.Equal(PngColorTypeRgba, e.Png[25]));
        Assert.All(AllSizes, size => Assert.True(LoadsInWindows(output, size), $"{size}px did not load"));
    }

    [Fact]
    public async Task Output_DecodesInWindowsImagingComponent()
    {
        string input = CreateImage("wic.png", 300, 300);

        var (_, output) = await CompressAsync(input);

        // StorageFile image properties go through WIC, which rejects or skips palette PNG entries.
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(output);
        var properties = await file.Properties.GetImagePropertiesAsync();
        Assert.True(properties.Width > 0);
    }

    [Fact]
    public async Task NonSquareImage_IsPaddedWithTransparency_WithoutDistortion()
    {
        string input = CreateImage("wide.png", 1000, 500);

        var (_, output) = await CompressAsync(input);

        using var entry = new MagickImage(ReadEntries(output).First(e => e.Size == 256).Png);
        Assert.Equal(256u, entry.Width);
        Assert.Equal(256u, entry.Height);
        using var pixels = entry.GetPixels();
        Assert.Equal(0, pixels.GetPixel(128, 10)!.ToColor()!.A); // above the image
        Assert.Equal(255, pixels.GetPixel(128, 128)!.ToColor()!.A); // inside it
        Assert.Equal(255, pixels.GetPixel(128, 66)!.ToColor()!.A); // 2:1 image spans rows 64 to 191
        Assert.Equal(0, pixels.GetPixel(128, 60)!.ToColor()!.A);
    }

    [Fact]
    public async Task SmallImage_IsNotUpscaled()
    {
        string input = CreateImage("small.png", 40, 30);

        var (_, output) = await CompressAsync(input);

        Assert.Equal([40, 32, 24, 20, 16], ReadEntries(output).Select(e => e.Size));
    }

    [Fact]
    public async Task ImageSmallerThanSmallestIcon_Fails()
    {
        string input = CreateImage("tiny.png", 10, 10);

        await Assert.ThrowsAsync<NotSupportedException>(() => CompressAsync(input));
    }

    [Theory]
    [InlineData(".gif", MagickFormat.Gif)]
    [InlineData(".png", MagickFormat.APng)]
    [InlineData(".webp", MagickFormat.WebP)]
    [InlineData(".tif", MagickFormat.Tiff)]
    public async Task AnimatedImage_Fails_WithoutOutput(string extension, MagickFormat format)
    {
        string input = Path.Combine(_testRoot, "animated" + extension);
        using (var frames = new MagickImageCollection())
        {
            foreach (var color in new[] { MagickColors.Red, MagickColors.Blue })
            {
                var frame = new MagickImage(color, 64, 64);
                frame.AnimationDelay = 10;
                frames.Add(frame);
            }
            frames.Write(input, format);
        }
        string outputFolder = Path.Combine(_testRoot, "output-animated");

        await Assert.ThrowsAsync<NotSupportedException>(() => CompressAsync(input, outputFolder));

        Assert.False(Directory.Exists(outputFolder) && Directory.EnumerateFiles(outputFolder).Any());
    }

    [Theory]
    [InlineData(24, 24)]
    [InlineData(2000, 1000)]
    public async Task Svg_GetsAllSizes_WithTransparentBackground(int width, int height)
    {
        string input = Path.Combine(_testRoot, $"logo{width}.svg");
        File.WriteAllText(input, $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
              <circle cx="{width / 2}" cy="{height / 2}" r="{Math.Min(width, height) / 4}" fill="#c02040"/>
            </svg>
            """);

        var (_, output) = await CompressAsync(input);

        Assert.Equal(AllSizes, ReadEntries(output).Select(e => e.Size));
        using var entry = new MagickImage(ReadEntries(output)[0].Png);
        using var pixels = entry.GetPixels();
        Assert.Equal(0, pixels.GetPixel(0, 0)!.ToColor()!.A);
        Assert.Equal(255, pixels.GetPixel(128, 128)!.ToColor()!.A);
    }

    [Fact]
    public async Task WideGamutImage_IsConvertedToSrgb()
    {
        string input = Path.Combine(_testRoot, "wide-gamut.png");
        using (var image = new MagickImage(new MagickColor("#4080C0"), 64, 64))
        {
            image.SetProfile(ColorProfiles.SRGB);
            image.TransformColorSpace(ColorProfiles.AppleRGB);
            image.Write(input);
        }

        var (_, output) = await CompressAsync(input);

        using var entry = new MagickImage(ReadEntries(output).First(e => e.Size == 64).Png);
        Assert.Null(entry.GetColorProfile());
        var color = entry.GetPixels().GetPixel(32, 32)!.ToColor()!;
        Assert.InRange((int)color.R, 0x40 - 4, 0x40 + 4);
        Assert.InRange((int)color.G, 0x80 - 4, 0x80 + 4);
        Assert.InRange((int)color.B, 0xC0 - 4, 0xC0 + 4);
    }

    [Fact]
    public async Task OutputLargerThanInput_DoesNotWarn()
    {
        string input = Path.Combine(_testRoot, "flat.png");
        using (var image = new MagickImage(MagickColors.Red, 300, 300))
            image.Write(input);
        WarningHelper.Instance.ClearAll();

        var (media, output) = await CompressAsync(input);

        Assert.All(ReadEntries(output), e => Assert.Equal(PngColorTypeRgba, e.Png[25])); // flat colours are what oxipng would turn into palette
        Assert.True(new FileInfo(output).Length > new FileInfo(input).Length, "test needs output larger than input");
        bool warned = WarningHelper.Instance.PostCompressionWarnings.TryGetValue(PostCompressionWarningType.FileSizeIncreased, out var files) && files.Contains(media);
        Assert.False(warned);
    }

    private async Task<(MediaFileInfo media, string output)> CompressAsync(string inputPath, string? outputFolder = null)
    {
        var settings = new OutputSettings
        {
            format = OutputFormat.Ico,
            OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
            OutputFolder = outputFolder ?? Path.Combine(_testRoot, "output-" + Guid.NewGuid().ToString("N")),
        };
        var media = new MediaFileInfo(Path.GetDirectoryName(inputPath)!, new FileInfo(inputPath));
        await TestFileHelper.WaitUntil(() => media.Width > 0 || !Path.GetExtension(inputPath).Equals(".png", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(5));

        string output = await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        Assert.True(File.Exists(output));
        return (media, output);
    }

    private string CreateImage(string fileName, int width, int height)
    {
        string path = Path.Combine(_testRoot, fileName);
        var pixels = new byte[width * height * 3];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)((i * 31) % 251);
        using var image = new MagickImage(pixels, new PixelReadSettings((uint)width, (uint)height, StorageType.Char, PixelMapping.RGB));
        image.Write(path);
        return path;
    }

    private static List<(int Size, byte[] Png)> ReadEntries(string icoPath)
    {
        byte[] raw = File.ReadAllBytes(icoPath);
        Assert.Equal(1, BitConverter.ToUInt16(raw, 2));
        var entries = new List<(int, byte[])>();
        for (int i = 0; i < BitConverter.ToUInt16(raw, 4); i++)
        {
            int entry = 6 + 16 * i;
            int size = raw[entry] == 0 ? 256 : raw[entry];
            Assert.Equal(raw[entry], raw[entry + 1]);
            int length = (int)BitConverter.ToUInt32(raw, entry + 8), offset = (int)BitConverter.ToUInt32(raw, entry + 12);
            byte[] png = raw[offset..(offset + length)];
            Assert.Equal(0x89, png[0]);
            entries.Add((size, png));
        }
        return entries;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImageW(IntPtr instance, string name, uint type, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    private static bool LoadsInWindows(string path, int size)
    {
        const uint ImageIcon = 1, LoadFromFile = 0x10;
        IntPtr icon = LoadImageW(IntPtr.Zero, path, ImageIcon, size, size, LoadFromFile);
        if (icon == IntPtr.Zero)
            return false;
        DestroyIcon(icon);
        return true;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testRoot, recursive: true);
        }
        catch (IOException) { }
    }
}
