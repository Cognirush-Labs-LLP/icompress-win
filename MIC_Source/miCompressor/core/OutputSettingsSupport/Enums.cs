using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    #region OutputFormat
    /// <summary>
    /// Represents the supported output formats for image compression.
    /// Stores both format name and file extension.
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// Keep the original format when possible. If output format is not supported, default format will be used (Advanced Settings).
        /// </summary>
        KeepSame = 0,
        /// <summary>
        /// JPEG format (.jpg, .jpeg, .JPG, .JPEG)
        /// </summary>
        Jpg = 1,
        /// <summary>
        /// PNG format (.png, .PNG)
        /// </summary>
        Png,

        /// <summary>
        /// WebP format (.webp, .WEBP)
        /// </summary>
        Webp,        // WebP format (.webp, .WEBP)

        /// <summary>
        /// AVID (very slow, experimental)
        /// </summary>
        avif,

        /// <summary>
        /// Gif 
        /// </summary>
        Gif
    }

    /// <summary>
    /// Provides extension methods for OutputFormat enumeration.
    /// </summary>
    public static class OutputFormatHelper
    {
        /// <summary>
        /// Retrieves the corresponding file extension for a given output format,
        /// preserving the original file's extension case if it matches.
        /// </summary>
        /// <param name="format">The desired output format.</param>
        /// <param name="originalFilePath">The original file's full path.</param>
        /// <returns>The correct file extension preserving case and format.</returns>
        public static string GetOutputExtension(this OutputFormat format, string originalFilePath)
        {
            if (string.IsNullOrWhiteSpace(originalFilePath))
                throw new ArgumentException("Invalid file path provided.", nameof(originalFilePath));

            string originalExtension = Path.GetExtension(originalFilePath);

            return format switch
            {
                OutputFormat.Jpg => IsJpeg(originalExtension) ? originalExtension : ".jpg",
                OutputFormat.Png => IsPng(originalExtension) ? originalExtension : ".png",
                //OutputFormat.Tiff => IsTiff(originalExtension) ? originalExtension : ".tiff",
                OutputFormat.Webp => IsWebp(originalExtension) ? originalExtension : ".webp",
                //OutputFormat.heic => IsHeic(originalExtension) ? originalExtension : ".heic",
                OutputFormat.avif => IsAvif(originalExtension) ? originalExtension : ".avif",
                OutputFormat.Gif => IsGif(originalExtension) ? originalExtension : ".gif",
                OutputFormat.KeepSame => isSupportedAsOutput(originalExtension) ? originalExtension : AdvancedSettings.Instance.defaultImageExtension.GetOutputExtension(originalFilePath),
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported format.")
            };
        }

        private static bool isSupportedAsOutput(string originalExtension)
        {
            return IsJpeg(originalExtension) || IsPng(originalExtension) || IsTiff(originalExtension) || IsWebp(originalExtension) || IsHeic(originalExtension) || IsAvif(originalExtension);
        }

        private static bool IsJpeg(string ext) => ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        private static bool IsPng(string ext) => ext.Equals(".png", StringComparison.OrdinalIgnoreCase);
        private static bool IsTiff(string ext) => ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase) || ext.Equals(".tif", StringComparison.OrdinalIgnoreCase);
        private static bool IsWebp(string ext) => ext.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        private static bool IsHeic(string ext) => ext.Equals(".heic", StringComparison.OrdinalIgnoreCase);
        private static bool IsAvif(string ext) => ext.Equals(".avif", StringComparison.OrdinalIgnoreCase);
        private static bool IsGif(string ext) => ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);


        /// <summary>
        /// Get text to show to user for the image format
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetDescription(this OutputFormat format)
        {
            return format switch
            {
                OutputFormat.KeepSame => "Keep Same",
                OutputFormat.Jpg => "JPEG",
                OutputFormat.Png => "PNG",
                //OutputFormat.Tiff => "TIFF", // Not a good format anymore
                OutputFormat.Webp => "WebP",
                //OutputFormat.heic => "HEIC", //Not supported by Magick.NET
                OutputFormat.avif => "AVIF",
                OutputFormat.Gif => "GIF"
            };
        }

        /// <summary>
        /// Returns format that will be actually used. Basically, 'Keep Same' will be converted to one of the actual output format. Others, will be returned as it is.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="outputPath">Output file path, full or short path, we need this to extract extension</param>
        public static OutputFormat GetOutputFormatFor(string outputPath)
        {
            string requiredExtension = Path.GetExtension(outputPath);

            if (IsJpeg(requiredExtension)) return OutputFormat.Jpg;
            if (IsPng(requiredExtension)) return OutputFormat.Png;
            //if (IsTiff(requiredExtension)) return OutputFormat.Tiff;
            if (IsWebp(requiredExtension)) return OutputFormat.Webp;
            //if (IsHeic(requiredExtension)) return OutputFormat.heic;
            if (IsAvif(requiredExtension)) return OutputFormat.avif;
            if (IsGif(requiredExtension)) return OutputFormat.Gif;
            return OutputFormat.Jpg;
        }



    }
    #endregion

    #region Dimension Reduction Strategy
    /// <summary>
    /// Common Strategy to reduce the image dimension. We always maintain the aspect ratio of the image.
    /// </summary>
    public enum DimensionReductionStrategy
    {
        /// <summary>
        /// No change in dimension
        /// </summary>
        KeepSame,
        /// <summary>
        /// Reduce dimension by percentage 
        /// </summary>
        Percentage,
        /// <summary>
        /// Longest Edge of the image will be considered and will be reduced to specified length. If the long edge is already smaller than specified length, no change is performed.
        /// </summary>
        LongEdge,
        /// <summary>
        /// Resize to specified height. Image may only decrease in dimension with this settings as if the height is already smaller than specified length, no change is performed.  
        /// </summary>
        MaxHeight,
        /// <summary>
        /// Resize to specified width. Image may only decrease in dimension with this settings as if the width is already smaller than specified length, no change is performed.  
        /// </summary>
        MaxWidth,
        /// <summary>
        /// Fix the height of each image to specified length. Image size may increase or decrease. 
        /// </summary>
        FixedHeight,
        /// <summary>
        /// Fix the width of each image to specified length. Image size may increase or decrease. 
        /// </summary>
        FixedWidth,

        /// <summary>
        /// Used for resizing images for print size. 
        /// Fit the image in a frame of specified size. Image size will not increase as smaller image will be keep as same. Frame is made of two edges, primary and secondary. Primary edge is the one which is fixed and secondary edge is the one which is flexible. Primary edge is always the longest edge of the image.
        /// </summary>
        FitInFrame,

        /// <summary>
        /// Used for resizing images for print size. 
        /// Fix the image in a frame of specified size. Image size may increase or decrease. Frame is made of two edges, primary and secondary. Primary edge is the one which is fixed and secondary edge is the one which is flexible. Primary edge is always the longest edge of the image.
        /// </summary>
        FixedInFrame
    }

    /// <summary>
    /// Extension methods for DimensionReductionStrategy to provide user-friendly names.
    /// </summary>
    public static class DimensionReductionStrategyExtensions
    {
        /// <summary>
        /// Retrieves a user-friendly description for a given DimensionReductionStrategy.
        /// </summary>
        /// <param name="strategy">The strategy whose name needs to be retrieved.</param>
        /// <returns>Formatted user-friendly name.</returns>
        public static string GetDescription(this DimensionReductionStrategy strategy)
        {
            return strategy switch
            {
                DimensionReductionStrategy.KeepSame => "Keep Same",
                DimensionReductionStrategy.Percentage => "Reduce by Percentage",
                DimensionReductionStrategy.LongEdge => "Max Longest Edge",
                DimensionReductionStrategy.MaxHeight => "Max Height",
                DimensionReductionStrategy.MaxWidth => "Max Width",
                DimensionReductionStrategy.FixedHeight => "Fixed Height",
                DimensionReductionStrategy.FixedWidth => "Fixed Width",
                DimensionReductionStrategy.FitInFrame => "Fit in Frame (Print)",
                DimensionReductionStrategy.FixedInFrame => "Fixed in Frame (Print)",
                _ => strategy.ToString() // Fallback (should never happen)
            };
        }
    }

    #endregion

    #region Output File Location Settings
    /// <summary>
    /// Strategy to save compressed images to.
    /// </summary>
    public enum OutputLocationSetting
    {
        /// <summary>
        /// A 'Compressed' folder is created inside the selected directory or beside the selected file. 
        /// </summary>
        InCompressedFolder,
        /// <summary>
        /// New file is created beside the original file with a suffix in the name. i.e. if the original file was file.jpg, this strategy make another file in the same directy called file@3x.jpg if suffix specified was '@3x'.
        /// </summary>
        SameFolderWithFileNameSuffix,
        /// <summary>
        /// Replace original file, results in data loss as orignal files will be replaced with compressed files.
        /// </summary>
        ReplaceOriginal,
        /// <summary>
        /// All selected items will be compressed and stored in user specified directory while maintaining the folder structure of original files (i.e. relative to the selected path). e.g. if selected path "def" is "C:/abc/def" folder and output folder is "C:/output" then "C:/abc/def/xyz/lmn.jpg" will be stored as C:/output/def/xyz/lmn.jpg".
        /// </summary>
        UserSpecificFolder,
    }

    public static class OutputLocationSettingsExtensions
    {
        //Get Description (User Friendly name) of OutputLocationSetting
        public static string GetDescription(this OutputLocationSetting outputLocationSettings)
        {
            return outputLocationSettings switch
            {
                OutputLocationSetting.InCompressedFolder => "'Compressed' Folder In Selected Folder",
                OutputLocationSetting.SameFolderWithFileNameSuffix => "Create Next To Original (with Suffix/Prefix)",
                OutputLocationSetting.ReplaceOriginal => "Replace Original (Lose Original)",
                OutputLocationSetting.UserSpecificFolder => "Specify Output Folder"
            };
        }
    }
    #endregion
}
