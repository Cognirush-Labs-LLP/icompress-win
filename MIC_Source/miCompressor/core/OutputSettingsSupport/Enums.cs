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
}
