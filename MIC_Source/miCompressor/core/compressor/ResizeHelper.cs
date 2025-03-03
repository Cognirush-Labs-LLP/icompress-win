using System;
using ImageMagick;

namespace miCompressor.core
{
    /// <summary>
    /// Helper for resizing images using Magick.NET based on specified output settings.
    /// </summary>
    public static class ResizeHelper
    {
        /// <summary>
        /// Resize a single MagickImage according to the provided settings.
        /// </summary>
        public static void ResizeImage(MagickImage image, OutputSettings settings)
        {
            if (settings.dimensionStrategy == DimensionReductionStrategy.KeepSame)
                return; // no resizing needed

            (uint targetWidth, uint targetHeight) = DimensionHelper.GetOutputDimensions(settings, image.Width, image.Height);
            if (targetWidth != image.Width || targetHeight != image.Height)
            {
                image.Resize(targetWidth, targetHeight);
            }
        }

        /// <summary>
        /// Resize all frames in a MagickImageCollection according to the provided settings.
        /// Ensures all frames maintain consistent dimensions after resizing.
        /// </summary>
        public static void ResizeFrames(MagickImageCollection frames, OutputSettings settings)
        {
            if (settings.dimensionStrategy == DimensionReductionStrategy.KeepSame)
                return;
            if (frames.Count == 0) return;

            // Calculate target dimensions based on the first frame
            (uint targetW, uint targetH) = DimensionHelper.GetOutputDimensions(settings, frames[0].Width, frames[0].Height);
            // If any frame is larger than target, we will downscale; if smaller and FixedInFrame, upscale.
            // Apply resizing to each frame to the same dimensions for consistency.
            foreach (MagickImage frame in frames)
            {
                if (frame.Width != targetW || frame.Height != targetH)
                {
                    frame.Resize(targetW, targetH);
                }
            }
        }
    }
}
