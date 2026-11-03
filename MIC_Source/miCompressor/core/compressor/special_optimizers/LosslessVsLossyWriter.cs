using ImageMagick;
using ImageMagick.Formats;
using System;
using System.IO;
using System.Linq;

namespace miCompressor.core
{
    /// <summary>
    /// Encapsulates the "try lossy vs lossless and pick the smaller" write flow.
    /// Applies to WebP and JXL only. JPEG is excluded; PNG remains single-pass.
    /// </summary>
    internal static class LosslessVsLossyWriter
    {
        // Lossless only beats lossy on low color content (flat graphics, UI, logos). On real images, both WebP and JXL
        // lossless won up to ~2000 colors and lost from ~2600 up (photos and screenshots have many thousands).
        private const uint MaxColorsForLossless = 2048;
        // Larger images are judged by their center section only, to keep the color count cheap.
        private const uint ColorSampleSize = 1000;

        // ---------------------------- Public API ----------------------------

        /// <summary>
        /// Decide whether a lossy-vs-lossless comparison should be attempted for the given settings.
        /// Thread-safety: does not mutate <paramref name="firstFrame"/>; safe for concurrent use on different images.
        /// </summary>
        public static bool ShouldTryLosslessVsLossy(OutputSettings settings, string outputPath, IMagickImage<byte> firstFrame)
        {
            if (settings.quality >= 100) // already lossless
                return false;

            // Only formats where a true lossless encode exists and can be smaller than a lossy encode.
            // Exclude JPEG; PNG stays single-pass (quantization already handled downstream).
            var format = OutputFormatHelper.GetOutputFormatFor(outputPath);
            return format is OutputFormat.Webp or OutputFormat.Jxl
                && CountSampleColors(firstFrame) <= MaxColorsForLossless;
        }

        private static uint CountSampleColors(IMagickImage<byte> image)
        {
            if (image.Width <= ColorSampleSize && image.Height <= ColorSampleSize)
                return image.TotalColors;

            using var sample = image.Clone();
            sample.Crop(new MagickGeometry(ColorSampleSize, ColorSampleSize), Gravity.Center);
            return sample.TotalColors;
        }

        // JXL has no lossless define; quality 100 is what selects its lossless mode.
        private static void PrepareLossless(IMagickImage<byte> image, string outputPath)
        {
            if (OutputFormatHelper.GetOutputFormatFor(outputPath) == OutputFormat.Jxl)
                image.Quality = 100;
        }

        /// <summary>
        /// Two-pass write for single-frame images: encode once with caller's lossy settings and once lossless; write the smaller to disk.
        /// If the format is not supported, returns false without writing (caller should fall back to normal path).
        /// Thread-safety: do not share the same <see cref="IMagickImage"/> across threads. Clones are created for isolation.
        /// Throws on Magick errors; caller should handle.
        /// </summary>
        /// <param name="src">Prepared image (resized/watermarked). Not mutated.</param>
        /// <param name="settings">Output settings; only <c>format</c>/<c>quality</c> are used here.</param>
        /// <param name="mediaInfo">Current file’s metadata for your define helper.</param>
        /// <param name="outputPath">Destination file path; this method writes exactly once.</param>
        /// <returns>True if this method handled the write; false if format not applicable.</returns>
        public static bool TryWrite(MagickImage? src, OutputSettings settings, MediaFileInfo mediaInfo, string outputPath)
        {
            if (src == null || !ShouldTryLosslessVsLossy(settings, outputPath, src))
                return false;

            // Build lossy defines using the app’s existing helper (keeps your global policy centralized).

            var lossyDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, false, settings.quality, mediaInfo);
            var losslessDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, false, 100, mediaInfo); //100% translates to lossless in the helper method. 

            using var lossyImg = src.Clone();
            using var losslessImg = src.Clone();

            // Explicit, consistent metadata state across both encodes.
            lossyImg.Strip();
            losslessImg.Strip();
            PrepareLossless(losslessImg, outputPath);

            // Encode to memory and compare — no double disk I/O.
            using var lossyMs = new MemoryStream();
            using var losslessMs = new MemoryStream();

            lossyImg.Write(lossyMs, lossyDefines);
            losslessImg.Write(losslessMs, losslessDefines);

            // Pick winner and write once to disk
            var winner = (losslessMs.Length < lossyMs.Length) ? losslessMs : lossyMs;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, winner.ToArray());

            return true;
        }

        /// <summary>
        /// Two-pass write for multi-frame collections (animated WebP/AVIF).
        /// Same semantics as single-frame overload.
        /// Thread-safety: do not share the same collection across threads.
        /// </summary>
        public static bool TryWrite(MagickImageCollection collection, OutputSettings settings, MediaFileInfo mediaInfo, string outputPath)
        {
            if (collection.Count == 0 || !ShouldTryLosslessVsLossy(settings, outputPath, collection[0]))
                return false;

            var lossyDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, true, settings.quality, mediaInfo);
            var losslessDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, true, 100, mediaInfo);

            using var lossyCol = new MagickImageCollection(collection.Select(f => (MagickImage)f.Clone()));
            using var lossCol = new MagickImageCollection(collection.Select(f => (MagickImage)f.Clone()));

            // Strip metadata for consistency
            foreach (var f in lossyCol) f.Strip();
            foreach (var f in lossCol) { f.Strip(); PrepareLossless(f, outputPath); }

            using var lossyMs = new MemoryStream();
            using var losslessMs = new MemoryStream();

            lossyCol.Write(lossyMs, lossyDefines);
            lossCol.Write(losslessMs, losslessDefines);

            var winner = (losslessMs.Length < lossyMs.Length) ? losslessMs : lossyMs;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, winner.ToArray());

            return true;
        }
    }
}
