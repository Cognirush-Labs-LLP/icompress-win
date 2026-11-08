using ImageMagick;
using System;
using System.IO;
using System.Linq;

namespace miCompressor.core;

/// <summary>
/// The part of an image to keep, as fractions of the image after rotation and before zoom. Outside 0 to 1 where it reaches past the image, which adds blank space.
/// Fractions rather than pixels, as decoders can disagree slightly on the size of a camera raw image.
/// </summary>
public readonly record struct CropRect(double X, double Y, double Width, double Height);

/// <summary>
/// The color of blank space added by a crop that reaches past the image. Its default is transparent.
/// </summary>
public readonly record struct CropFill(byte R, byte G, byte B, byte A)
{
    public static readonly CropFill Transparent = default;
    public static readonly CropFill White = new(255, 255, 255, 255);
    public static readonly CropFill Black = new(0, 0, 0, 255);
}

/// <summary>
/// Output size of a crop, and the rectangle it takes from the image before zoom.
/// </summary>
/// <param name="SourceX">Left edge in image pixels, negative if the crop starts before the image.</param>
/// <param name="SourceY">Top edge in image pixels, negative if the crop starts above the image.</param>
/// <param name="HasSpill">True if part of the output is outside the image, so it's blank space.</param>
public readonly record struct CropOutput(uint Width, uint Height, int SourceX, int SourceY, uint SourceWidth, uint SourceHeight, bool HasSpill);

/// <summary>
/// Everything the user chose to change in one image. A new kind of edit belongs here, with a default that changes nothing, and in <see cref="ImageEditor.Apply"/>.
/// </summary>
/// <param name="QuarterTurns">Clockwise quarter turns, 0 to 3. Applied first.</param>
/// <param name="Crop">Null if the image is not cropped. Applied after rotation.</param>
/// <param name="Zoom">Output pixels per image pixel, which is how an edited image is resized. Only applies with a crop, as the crop is what sets the output's size.</param>
/// <param name="Fill">The color of any blank space the crop adds.</param>
/// <param name="LockedRatio">The ratio the Edit screen was keeping, to keep it when reopened. Doesn't affect the output.</param>
public sealed record ImageEdit(int QuarterTurns, CropRect? Crop, double Zoom = 1, CropFill Fill = default, double? LockedRatio = null)
{
    /// <summary>
    /// True if the user chose this image's size themselves, which excludes it from batch resize. Rotating doesn't choose a size.
    /// </summary>
    public bool OverridesBatchResize => Crop != null;
}

/// <summary>
/// Applies an <see cref="ImageEdit"/> to decoded images.
/// </summary>
public static class ImageEditor
{
    /// <summary>
    /// True if the file is a still image that can be edited. Reads the file, so call it off the UI thread.
    /// Animated and multi-page files are excluded, along with GIF (its compression paths skip Magick) and SVG (icon output renders it again from the file).
    /// </summary>
    public static bool CanEdit(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is ".gif" or ".svg" or ".svgz")
            return false;
        if (!ImageCompressor.MultiFrameSupportedFormats.Contains(extension))
            return true;
        if (extension is ".png" or ".apng")
            return !MetadataHelper.IsAnimatedPng(filePath);

        try
        {
            return MagickImageInfo.ReadCollection(filePath).Count() == 1;
        }
        catch (MagickException)
        {
            return false; // unreadable, it would fail to compress as well.
        }
    }

    /// <summary>
    /// True if an image written to this path or extension can have transparent pixels.
    /// </summary>
    public static bool KeepsTransparency(string outputPathOrExtension) => OutputFormatHelper.GetOutputFormatFor(outputPathOrExtension) != OutputFormat.Jpg;

    /// <summary>
    /// Applies the edit to an image that already has its orientation corrected. Does nothing if there is no edit.
    /// </summary>
    /// <param name="outputPathOrExtension">Where the image will be written, which decides if blank space can be transparent.</param>
    public static void Apply(MagickImage image, ImageEdit? edit, string outputPathOrExtension)
    {
        if (edit == null)
            return;

        if (edit.QuarterTurns != 0)
            image.Rotate(edit.QuarterTurns * 90);

        // A position on a canvas, which some PNG and TIFF files store, would otherwise shift the crop. Formats such as GIF also write it out.
        image.ResetPage();

        if (edit.Crop is CropRect crop)
        {
            CropOutput output = ResolveCropOutput(crop, image.Width, image.Height, edit.Zoom);
            if (output.HasSpill)
                ApplyCropWithBlankSpace(image, output, edit.Zoom, UsableFill(edit.Fill, outputPathOrExtension));
            else
            {
                CropPixels(image, output.SourceX, output.SourceY, output.SourceWidth, output.SourceHeight);
                ResizeTo(image, output.Width, output.Height);
            }
        }
    }

    /// <summary>
    /// The color blank space really gets in this output: white if the color is transparent and the output can't be.
    /// </summary>
    public static CropFill UsableFill(CropFill fill, string outputPathOrExtension) => KeepsTransparency(outputPathOrExtension) || fill.A == 255 ? fill : CropFill.White;

    /// <summary>
    /// Size of an upright image after the edit.
    /// </summary>
    public static (uint width, uint height) ResolveOutputSize(uint width, uint height, ImageEdit? edit)
    {
        if (edit == null)
            return (width, height);

        if (edit.QuarterTurns % 2 != 0)
            (width, height) = (height, width);

        if (edit.Crop is CropRect crop)
        {
            CropOutput output = ResolveCropOutput(crop, width, height, edit.Zoom);
            return (output.Width, output.Height);
        }
        return (width, height);
    }

    /// <summary>
    /// The only place a crop's output is worked out, so the size the user is shown is the size that gets written.
    /// </summary>
    public static CropOutput ResolveCropOutput(CropRect crop, uint imageWidth, uint imageHeight, double zoom = 1)
    {
        (int sourceX, uint sourceWidth) = ResolveSourcePixels(crop.X, crop.Width, imageWidth);
        (int sourceY, uint sourceHeight) = ResolveSourcePixels(crop.Y, crop.Height, imageHeight);
        bool hasSpill = sourceX < 0 || sourceY < 0 || sourceX + (long)sourceWidth > imageWidth || sourceY + (long)sourceHeight > imageHeight;

        // At 100% the image's pixels are kept as they are, so the output is exactly what's taken from the image.
        uint width = zoom == 1 ? sourceWidth : (uint)Math.Max(1, RoundPixels(crop.Width * imageWidth * zoom));
        uint height = zoom == 1 ? sourceHeight : (uint)Math.Max(1, RoundPixels(crop.Height * imageHeight * zoom));
        return new CropOutput(width, height, sourceX, sourceY, sourceWidth, sourceHeight, hasSpill);
    }

    /// <summary>
    /// The whole pixels a crop takes along one axis. Its start and length are rounded separately, which alone could carry a crop that's inside the image one pixel past the edge, and that would add a false line of blank space. So a crop within half a pixel of the image stays inside it.
    /// </summary>
    /// <param name="start">Fraction of the image's length.</param>
    /// <param name="length">Fraction of the image's length.</param>
    private static (int start, uint length) ResolveSourcePixels(double start, double length, uint imageLength)
    {
        const double FloatingPointNoise = 1e-6;
        double exactStart = start * imageLength;
        double exactEnd = (start + length) * imageLength;
        long pixels = Math.Max(1, (long)RoundPixels(length * imageLength));
        long first = (long)RoundPixels(exactStart);

        if (exactStart > -0.5 + FloatingPointNoise && exactEnd < imageLength + 0.5 - FloatingPointNoise)
        {
            pixels = Math.Min(pixels, imageLength);
            first = Math.Clamp(first, 0, imageLength - pixels);
        }
        return ((int)first, (uint)pixels);
    }

    private static double RoundPixels(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Crops and forgets the position on the old canvas, which later steps would otherwise measure from.
    /// </summary>
    private static void CropPixels(MagickImage image, int x, int y, uint width, uint height)
    {
        image.Crop(new MagickGeometry(x, y, width, height));
        image.ResetPage();
    }

    /// <summary>
    /// Resizes only the part of the image that shows, then adds the blank space around it. The other way around, a far zoomed-out crop would need an enormous canvas, and a zoomed-in one an enormous image.
    /// </summary>
    private static void ApplyCropWithBlankSpace(MagickImage image, CropOutput output, double zoom, CropFill fill)
    {
        int left = Math.Max(0, output.SourceX);
        int top = Math.Max(0, output.SourceY);
        long right = Math.Min(image.Width, output.SourceX + (long)output.SourceWidth);
        long bottom = Math.Min(image.Height, output.SourceY + (long)output.SourceHeight);

        // A crop wholly beside the image starts the canvas after the image's end, which leaves only blank space.
        int canvasX = (int)image.Width;
        int canvasY = 0;
        if (right > left && bottom > top)
        {
            CropPixels(image, left, top, (uint)(right - left), (uint)(bottom - top));
            ResizeTo(image, (uint)Math.Max(1, RoundPixels((right - left) * zoom)), (uint)Math.Max(1, RoundPixels((bottom - top) * zoom)));
            canvasX = (int)RoundPixels((output.SourceX - left) * zoom);
            canvasY = (int)RoundPixels((output.SourceY - top) * zoom);
        }

        if (fill.A < 255)
            image.Alpha(AlphaOption.Set);
        image.BackgroundColor = new MagickColor(fill.R, fill.G, fill.B, fill.A);
        image.Extent(new MagickGeometry(canvasX, canvasY, output.Width, output.Height));
        image.ResetPage();
    }

    private static void ResizeTo(MagickImage image, uint width, uint height)
    {
        if (image.Width != width || image.Height != height)
            image.Resize(new MagickGeometry(width, height) { IgnoreAspectRatio = true });
    }
}
