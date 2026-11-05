using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miCompressor.core;

/// <summary>
/// Writes an image as a Windows icon holding the standard icon sizes.
/// Every size is stored as an RGBA PNG. Windows Imaging Component (used by WPF, among others) skips palette PNGs inside an ICO, so pngquant can't be used here.
/// </summary>
internal static class IcoWriter
{
    private static readonly uint[] Sizes = [256, 128, 96, 64, 48, 40, 32, 24, 20, 16];
    private const uint SvgRenderEdge = 512; // twice the largest icon size
    private const double SvgDefaultDensity = 96;

    /// <param name="image">Decoded image with orientation already corrected.</param>
    /// <param name="sourcePath">File the image was decoded from.</param>
    /// <param name="outputPath">Path of the .ico file to write.</param>
    /// <exception cref="NotSupportedException">The image is smaller than the smallest icon size.</exception>
    public static void Write(MagickImage image, string sourcePath, string outputPath)
    {
        using MagickImage? svg = IsSvg(sourcePath) ? RenderSvg(sourcePath, image) : null;
        MagickImage source = svg ?? image;

        uint longEdge = Math.Max(source.Width, source.Height);
        if (longEdge < Sizes[^1])
            throw new NotSupportedException($"Image is too small for an icon, needs at least {Sizes[^1]}px: {sourcePath}");

        // An .ico path can't carry a profile, so this converts to sRGB: icon loaders ignore embedded profiles.
        ColorProfileHelper.StripMetadata(source, outputPath);
        source.BackgroundColor = MagickColors.None;
        source.Alpha(AlphaOption.Set);

        // The full set is prepared first, so a failure on any size never leaves a partial icon.
        var entries = Sizes.Where(size => size <= longEdge).Select(size => (size, png: EncodeSize(source, size))).ToList();
        WriteContainer(outputPath, entries);
    }

    private static bool IsSvg(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".svg" or ".svgz";

    /// <summary>
    /// Renders large enough for every icon size, even if the SVG's own size is small, and with a transparent background.
    /// </summary>
    private static MagickImage RenderSvg(string path, MagickImage defaultRender)
    {
        uint longEdge = Math.Max(defaultRender.Width, defaultRender.Height);
        double scale = Math.Max(1, (double)SvgRenderEdge / longEdge);
        return new MagickImage(path, new MagickReadSettings
        {
            BackgroundColor = MagickColors.None,
            Density = new Density(SvgDefaultDensity * scale),
        });
    }

    private static byte[] EncodeSize(MagickImage source, uint size)
    {
        string pngPath = Path.Combine(Path.GetTempPath(), $"mic-ico-{Guid.NewGuid():N}.png");
        try
        {
            using var frame = source.Clone();
            frame.FilterType = FilterType.Lanczos;
            frame.Resize(size, size); // fits inside, keeping aspect ratio
            frame.Extent(size, size, Gravity.Center);
            frame.Write(pngPath, MagickFormat.Png32);
            new PNGOptimizer().Optimize(pngPath, isPreview: false, keepColorType: true);
            return File.ReadAllBytes(pngPath);
        }
        finally
        {
            File.Delete(pngPath);
        }
    }

    private static void WriteContainer(string outputPath, List<(uint size, byte[] png)> entries)
    {
        const int HeaderBytes = 6, EntryBytes = 16;
        using var writer = new BinaryWriter(File.Create(outputPath));

        writer.Write((ushort)0); // reserved
        writer.Write((ushort)1); // type: icon
        writer.Write((ushort)entries.Count);

        uint offset = (uint)(HeaderBytes + EntryBytes * entries.Count);
        foreach (var (size, png) in entries)
        {
            byte edge = size == 256 ? (byte)0 : (byte)size; // 256 is stored as 0
            writer.Write(edge);
            writer.Write(edge);
            writer.Write((byte)0); // palette colours: none
            writer.Write((byte)0); // reserved
            writer.Write((ushort)1); // colour planes
            writer.Write((ushort)32); // bits per pixel
            writer.Write((uint)png.Length);
            writer.Write(offset);
            offset += (uint)png.Length;
        }
        foreach (var (_, png) in entries)
            writer.Write(png);
    }
}
