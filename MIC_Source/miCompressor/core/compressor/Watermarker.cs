using ImageMagick;
using System;
using System.IO;
using System.Linq;

namespace miCompressor.core
{
    /// <summary>
    /// Provides utility for applying watermark to an image using Magick.NET.
    /// </summary>
    public static class WatermarkHelper
    {
        /// <summary>
        /// Checks if watermark is configured.
        /// </summary>
        /// <returns></returns>
        public static bool IsWatermarkConfigured()
        {
            string watermarkPath = WatermarkSettings.GetWatermarkPath();
            return !string.IsNullOrWhiteSpace(watermarkPath) && File.Exists(watermarkPath);
        }

        /// <summary>
        /// Applies a watermark to the provided image. Should be called after resize operation. 
        /// Checks settings if watermark needs to be applied.
        /// </summary>
        /// <param name="baseImage">The base image to apply the watermark to.</param>
        /// <param name="settings">Settings that can help with watermarking (or not)</param>
        public static void ApplyWatermark(
            MagickImage baseImage,
            OutputSettings settings)
        {

            if (!settings.ApplyWatermark)
                return;

            string watermarkPath = WatermarkSettings.GetWatermarkPath();
            if (string.IsNullOrWhiteSpace(watermarkPath) || !File.Exists(watermarkPath))
                return;

            int minDimension = WatermarkSettings.GetMinDimension();
            int maxHeightPercentage = WatermarkSettings.GetMaxHeightPercentage();
            int opacityPercentage = WatermarkSettings.GetOpacityPercentage();

            // Skip watermarking if image is smaller than threshold
            if (baseImage.Width < minDimension || baseImage.Height < minDimension)
                return;

            using var watermark = new MagickImage(watermarkPath);

            // Calculate target height from base image height
            int targetHeight = (int)(baseImage.Height * (maxHeightPercentage / 100.0));

            // Enforce minimum height of 10 pixels
            targetHeight = Math.Max(10, targetHeight);

            // If watermark is smaller than 10px originally, do not upscale
            if (watermark.Height < 10)
                targetHeight = (int)watermark.Height;

            // Resize watermark maintaining aspect ratio (i.e. provide width = 0)
            watermark.Resize(0, (uint)targetHeight);

            // Apply opacity (alpha blending)
            watermark.Alpha(AlphaOption.Set);
            watermark.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, opacityPercentage / 100.0);

            // Position: bottom-right with margin
            uint x = baseImage.Width - watermark.Width - 10;
            uint y = baseImage.Height - watermark.Height - 10;

            // Apply watermark
            baseImage.Composite(watermark, (int)x, (int)y, CompositeOperator.Over);
        }

        /// <summary>
        /// Resize all frames in a MagickImageCollection according to the provided settings.
        /// Ensures all frames maintain consistent dimensions after resizing.
        /// </summary>
        public static void WatermarkAllFrames(MagickImageCollection frames, OutputSettings settings)
        {
            if (!settings.ApplyWatermark)
                return;

            var isDiffSizedFrames = frames.Any(f => f.Height != frames[0].Height || f.Height != frames[0].Width);

            if (isDiffSizedFrames)
                frames.Coalesce();

            if (frames.Count == 0) return;

            foreach (MagickImage frame in frames)
                ApplyWatermark(frame, settings);
        }
    }

    public static class WatermarkSettingsKeys
    {
        public const string WatermarkPath = "Watermark.Path";
        public const string MinDimension = "Watermark.MinDimension";
        public const string MaxHeightPercentage = "Watermark.MaxHeightPercentage";
        public const string OpacityPercentage = "Watermark.OpacityPercentage";
    }

    public static class WatermarkSettings
    {
        public static string GetWatermarkPath() =>
            AppSettingsManager.Get<string>(WatermarkSettingsKeys.WatermarkPath) ?? string.Empty;

        public static void SetWatermarkPath(string path) =>
            AppSettingsManager.Set(WatermarkSettingsKeys.WatermarkPath, path);

        public static int GetMinDimension() =>
            AppSettingsManager.Get<int>(WatermarkSettingsKeys.MinDimension, 300); // default: 300px

        public static void SetMinDimension(int value) =>
            AppSettingsManager.Set(WatermarkSettingsKeys.MinDimension, value);

        public static int GetMaxHeightPercentage() =>
            AppSettingsManager.Get<int>(WatermarkSettingsKeys.MaxHeightPercentage, 10); // default: 10%

        public static void SetMaxHeightPercentage(int value) =>
            AppSettingsManager.Set(WatermarkSettingsKeys.MaxHeightPercentage, value);

        public static int GetOpacityPercentage() =>
            AppSettingsManager.Get<int>(WatermarkSettingsKeys.OpacityPercentage, 40); // default: 40%

        public static void SetOpacityPercentage(int value) =>
            AppSettingsManager.Set(WatermarkSettingsKeys.OpacityPercentage, value);
    }

}
