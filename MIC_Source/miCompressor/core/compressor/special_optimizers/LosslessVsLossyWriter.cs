using ImageMagick;
using ImageMagick.Formats;
using System;
using System.IO;
using System.Linq;

namespace miCompressor.core
{
    /// <summary>
    /// Encapsulates the "try lossy vs lossless and pick the smaller" write flow.
    /// Applies to WebP and AVIF only. JPEG is excluded; PNG remains single-pass.
    /// </summary>
    internal static class LosslessVsLossyWriter
    {
        // ---------------------------- Public API ----------------------------

        /// <summary>
        /// Decide whether a lossy-vs-lossless comparison should be attempted for the given settings.
        /// Thread-safety: pure function; safe for concurrent use.
        /// </summary>
        public static bool ShouldTryLosslessVsLossy(MediaFileInfo mediaInfo, OutputSettings settings)
        {
            // Only formats where a true lossless encode exists and can be smaller than a lossy encode.
            // Exclude JPEG; PNG stays single-pass (quantization already handled downstream).
            OutputFormat[] allowedInputFormatForLossyVsLosslessCheck = [OutputFormat.Png, OutputFormat.Webp, OutputFormat.Gif];

            return (settings.format == OutputFormat.Webp)
                && allowedInputFormatForLossyVsLosslessCheck.Contains(OutputFormatHelper.GetOutputFormatFor(mediaInfo.FilePath))
                && settings.quality < 100; // user asked for lossy; we will probe if lossless is even smaller
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
            if (!ShouldTryLosslessVsLossy(mediaInfo, settings) || src == null)
                return false;

            // Build lossy defines using the app’s existing helper (keeps your global policy centralized).

            var lossyDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, false, settings.quality, mediaInfo);
            var losslessDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, false, 100, mediaInfo); //100% translates to lossless in the helper method. 

            using var lossyImg = src.Clone();
            using var losslessImg = src.Clone();

            // Explicit, consistent metadata state across both encodes.
            lossyImg.Strip();
            losslessImg.Strip();

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
            if (!ShouldTryLosslessVsLossy(mediaInfo, settings))
                return false;

            var lossyDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, true, settings.quality, mediaInfo);
            var losslessDefines = MagickHelper.GetWriteDefinesFor(settings.format, outputPath, true, 100, mediaInfo);

            using var lossyCol = new MagickImageCollection(collection.Select(f => (MagickImage)f.Clone()));
            using var lossCol = new MagickImageCollection(collection.Select(f => (MagickImage)f.Clone()));

            // Strip metadata for consistency
            foreach (var f in lossyCol) f.Strip();
            foreach (var f in lossCol) f.Strip();

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
