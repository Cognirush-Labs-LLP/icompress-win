using ImageMagick;
using System;
using System.IO;
using System.Linq;

namespace miCompressor.core;

/// <summary>
/// Crude memory estimates used to keep big batches from running out of RAM on low-memory, many-core PCs; <see cref="MemoryGate"/> uses them.
/// Estimates are meant to be realistic, not padded: we only want to slow down when memory really can't hold the work.
/// </summary>
public static class BatchMemoryPlanner
{
    private const ulong MB = 1024 * 1024;

    /// <summary>
    /// Fixed cost of a job beyond its pixels (decoder/encoder state, .NET objects, external tools).
    /// </summary>
    public const ulong PerJobOverheadBytes = 32 * MB;

    /// <summary>
    /// Free memory kept aside for the OS and other apps when deciding whether an image can start alongside others.
    /// </summary>
    public const ulong PressureReserveBytes = 256 * MB;

    /// <summary>
    /// Share of free memory the batch may plan to use.
    /// </summary>
    private const double BudgetShareOfAvailable = 0.9;

    /// <summary>
    /// Assumed pixels per byte of file when dimensions are unknown, or when the file may hold many frames.
    /// </summary>
    private const ulong PixelsPerFileByte = 4;

    private static readonly string[] PossiblyAnimatedExtensions = [".gif", ".webp"];

    private static readonly Lazy<ulong> s_bytesPerPixel = new(() =>
    {
        bool hdri = MagickNET.Features.Contains("HDRI", StringComparison.OrdinalIgnoreCase);
        ulong bytesPerChannel = hdri ? 4UL : Math.Max(1UL, Quantum.Depth / 8UL);
        return 4 * bytesPerChannel; // RGBA
    });

    /// <summary>
    /// Estimated peak memory needed to compress one file.
    /// </summary>
    /// <returns>ulong.MaxValue if the estimate fails, so the image runs alone.</returns>
    public static ulong EstimateBytes(MediaFileInfo file, OutputSettings settings)
    {
        // Must not throw: it runs before per-file error handling, so an exception here would stop the whole batch.
        try
        {
            // Only the output extension is needed, not the full output path: that touches the disk and creates temp files for previews.
            OutputFormat outputFormat = OutputFormatHelper.GetOutputFormatFor(settings.format.GetOutputExtension(file.FilePath));
            // An edit can enlarge the image past its decoded size. An unknown size stays unknown, for which the estimate falls back on the file's size.
            (uint editedWidth, uint editedHeight) = ImageEditor.ResolveOutputSize(file.Width, file.Height, file.Edit);
            ulong pixels = (ulong)file.Width * file.Height;
            bool enlarged = pixels > 0 && editedWidth * (ulong)editedHeight > pixels;
            return EstimateBytes(enlarged ? editedWidth : file.Width, enlarged ? editedHeight : file.Height, file.FileSize, Path.GetExtension(file.FilePath), outputFormat, s_bytesPerPixel.Value);
        }
        catch
        {
            return ulong.MaxValue;
        }
    }

    /// <summary>
    /// Estimated peak memory needed to compress one image.
    /// </summary>
    /// <param name="width">Width in pixels, 0 if unknown.</param>
    /// <param name="height">Height in pixels, 0 if unknown.</param>
    /// <param name="fileSize">Input file size in bytes.</param>
    /// <param name="sourceExtension">Input file extension including the dot.</param>
    /// <param name="outputFormat">Format the image will be written as.</param>
    /// <param name="bytesPerPixel">Bytes Magick uses per decoded pixel.</param>
    public static ulong EstimateBytes(uint width, uint height, ulong fileSize, string sourceExtension, OutputFormat outputFormat, ulong bytesPerPixel)
    {
        ulong pixels = (ulong)width * height; // fits: uint x uint < 2^64
        bool possiblyAnimated = PossiblyAnimatedExtensions.Contains(sourceExtension?.ToLowerInvariant());
        if (pixels == 0 || possiblyAnimated)
            pixels = Math.Max(pixels, SaturatingMultiply(fileSize, PixelsPerFileByte)); // frames are held in memory together

        // Decoded image plus one working copy (resize, orientation, watermark), plus the encoder's own buffers.
        ulong copies = 2 + outputFormat switch
        {
            OutputFormat.Jxl => 3UL,
            OutputFormat.avif => 2UL,
            OutputFormat.Webp => 1UL,
            _ => 0UL
        };

        // Corrupt or malicious headers can claim billions of pixels: saturate instead of wrapping around to a small estimate.
        ulong bytes = SaturatingMultiply(SaturatingMultiply(pixels, bytesPerPixel), copies);
        return bytes > ulong.MaxValue - PerJobOverheadBytes ? ulong.MaxValue : bytes + PerJobOverheadBytes;
    }

    private static ulong SaturatingMultiply(ulong a, ulong b)
    {
        return b != 0 && a > ulong.MaxValue / b ? ulong.MaxValue : a * b;
    }

    /// <summary>
    /// Bytes a batch may plan to use, out of the free memory when it starts.
    /// </summary>
    public static ulong Budget(ulong availableBytes) => (ulong)(availableBytes * BudgetShareOfAvailable);

    /// <summary>
    /// True if free memory now can't hold this image and still leave <see cref="PressureReserveBytes"/> for everything else.
    /// </summary>
    public static bool IsUnderPressure(ulong estimateBytes, ulong? availableBytes)
    {
        // Written without adding to the estimate, which can be near ulong.MaxValue.
        return availableBytes is ulong available && (available < estimateBytes || available - estimateBytes < PressureReserveBytes);
    }

    /// <summary>
    /// True if the error means memory ran out, so the same file may succeed when it runs alone.
    /// </summary>
    public static bool IsOutOfMemory(Exception? ex)
    {
        for (; ex != null; ex = ex.InnerException)
        {
            // Magick reports an exhausted memory limit as a cache error ("cache resources exhausted"), failed allocations as resource limit errors.
            if (ex is OutOfMemoryException or MagickResourceLimitErrorException or MagickCacheErrorException)
                return true;
            if (ex is AggregateException aggregate && aggregate.InnerExceptions.Any(IsOutOfMemory))
                return true;
        }
        return false;
    }
}
