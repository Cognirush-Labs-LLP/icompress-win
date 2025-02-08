using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Jpg = 1,    // JPEG format (.jpg, .jpeg, .JPG, .JPEG)
        Png,        // PNG format (.png, .PNG)
        Tiff,       // TIFF format (.tiff, .tif, .TIFF, .TIF)
        Webp        // WebP format (.webp, .WEBP)
    }

    /// <summary>
    /// Provides extension methods for OutputFormat enumeration.
    /// </summary>
    public static class OutputFormatExtensions
    {
        /// <summary>
        /// Retrieves the corresponding file extension for a given output format,
        /// preserving the original file's extension case if it matches.
        /// </summary>
        /// <param name="format">The desired output format.</param>
        /// <param name="originalFilePath">The original file's full path.</param>
        /// <returns>The correct file extension preserving case and format.</returns>
        public static string GetExtension(this OutputFormat format, string originalFilePath)
        {
            if (string.IsNullOrWhiteSpace(originalFilePath) || !File.Exists(originalFilePath))
                throw new ArgumentException("Invalid file path provided.", nameof(originalFilePath));

            string originalExtension = Path.GetExtension(originalFilePath);

            return format switch
            {
                OutputFormat.Jpg => IsJpeg(originalExtension) ? originalExtension : ".jpg",
                OutputFormat.Png => IsPng(originalExtension) ? originalExtension : ".png",
                OutputFormat.Tiff => IsTiff(originalExtension) ? originalExtension : ".tiff",
                OutputFormat.Webp => IsWebp(originalExtension) ? originalExtension : ".webp",
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported format.")
            };
        }

        private static bool IsJpeg(string ext) => ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        private static bool IsPng(string ext) => ext.Equals(".png", StringComparison.OrdinalIgnoreCase);
        private static bool IsTiff(string ext) => ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase) || ext.Equals(".tif", StringComparison.OrdinalIgnoreCase);
        private static bool IsWebp(string ext) => ext.Equals(".webp", StringComparison.OrdinalIgnoreCase);
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

    #endregion

    #region Output File Location Settings
    /// <summary>
    /// Strategy to save compressed images to.
    /// </summary>
    public enum OutputLocationSettings
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
    #endregion
}
