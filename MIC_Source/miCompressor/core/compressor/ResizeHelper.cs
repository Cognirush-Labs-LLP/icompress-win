using System;
using System.Diagnostics;
using System.Linq;
using ImageMagick;
using ImageMagick.Factories;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

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

            (uint targetHeight, uint targetWidth) = DimensionHelper.GetOutputDimensions(settings, image.Height, image.Width);
            if (targetWidth != image.Width || targetHeight != image.Height)
            {
                image.Resize(targetWidth, targetHeight);
            }
        }

        private static void ResizeFrameCanvas(IMagickImage frame)
        {
            var geometry = frame.Page;
            // Set the frame size without scaling the image content
            frame.Extent(geometry);
        }

        /// <summary>
        /// Resize all frames in a MagickImageCollection according to the provided settings.
        /// Ensures all frames maintain consistent dimensions after resizing.
        /// </summary>
        public static void ResizeFrames(MagickImageCollection frames, OutputSettings settings)
        {
            var isDiffSizedFrames = frames.Any(f => f.Height != frames[0].Height || f.Height != frames[0].Width);

            if (isDiffSizedFrames)
            {
                frames.Coalesce();
            }

            if (settings.dimensionStrategy == DimensionReductionStrategy.KeepSame)
                return;
            if (frames.Count == 0) return;

            // Calculate target dimensions based on the first frame
            (uint targetH, uint targetW) = DimensionHelper.GetOutputDimensions(settings, frames[0].Height, frames[0].Width);
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

        /// <summary>
        /// Checks if image will be resized based on provided output settings.
        /// We expect first frame to be largest in case other framews are smaller (in GIF).
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool NeedsResize(MagickImageCollection frames, OutputSettings settings)
        {
           if (frames.Count == 0) return false;
            return NeedsResize(frames[0], settings);
        }

        /// <summary>
        /// Checks if image will be resized based on provided output settings. 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool NeedsResize(IMagickImage img, OutputSettings settings)
        {
            return NeedsResize(img.Height, img.Width, settings);
        }

        /// <summary>
        /// Checks if image will be resized based on provided output settings. 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool NeedsResize(uint origHeight, uint origWidth, OutputSettings settings)
        {
            if (settings.dimensionStrategy == DimensionReductionStrategy.KeepSame)
                return false;

            (uint targetH, uint targetW) = DimensionHelper.GetOutputDimensions(settings, origHeight, origWidth);

            return (origWidth != targetW || origHeight != targetH);
        }
    }
}
