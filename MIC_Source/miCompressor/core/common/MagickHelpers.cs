using ImageMagick;
using ImageMagick.Formats;
using ImageMagick.ImageOptimizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    /// <summary>
    /// Helps with Image Magick functions
    /// </summary>
    public static class MagickHelper
    {
        public static IWriteDefines GetWriteDefinesFor(OutputFormat setFormat, string outputPath, bool isInputMultiframed, uint targetQuality, MediaFileInfo fileinfo)
        {
            var format = OutputFormatHelper.GetOutputFormatFor(outputPath);

            switch (format)
            {
                case OutputFormat.Jpg:
                    return new JpegWriteDefines
                    {
                        DctMethod = JpegDctMethod.Float,
                        OptimizeCoding = true,
                    };
                    break;
                case OutputFormat.Png:
                    return !isInputMultiframed ? null : new PngWriteDefines { PreserveiCCP = false, CompressionLevel = 5 };
                    break;
                case OutputFormat.Webp:
                    int method = 2;
                    uint pixels = fileinfo.Width * fileinfo.Height;

                    if (pixels <= 50 * 50) method = 6;
                    else if (pixels <= 0100 * 0100) method = 5;
                    else if (pixels <= 1200 * 0800) method = 4;
                    else if (pixels <= 1920 * 1080) method = 3;

                    return new WebPWriteDefines
                    {
                        Method = method,
                        Lossless = (targetQuality > 95)
                    };
                    break;
                /*case OutputFormat.Tiff:
                    return null;
                    return new TiffWriteDefines
                    {
                        PreserveCompression = true
                    };*/
            }
            return null;
        }

        public static MagickFormat GetMagickFormat(OutputFormat setFormat, string outputPath, bool isMultiframed)
        {
            var format = OutputFormatHelper.GetOutputFormatFor(outputPath);

            return format switch
            {
                OutputFormat.Jpg => MagickFormat.Jpeg,
                OutputFormat.Png => isMultiframed ? MagickFormat.APng : MagickFormat.Png,
                OutputFormat.Webp => MagickFormat.WebP,
                //OutputFormat.Tiff => MagickFormat.Tiff,
                //OutputFormat.heic => MagickFormat.Heic,
                OutputFormat.avif => MagickFormat.Avif,
                OutputFormat.Gif => MagickFormat.Gif
            };
        }

        public static bool CanSetQuality(OutputFormat setFormat, string outputPath)
        {
            var format = OutputFormatHelper.GetOutputFormatFor(outputPath);
            return format switch
            {
                OutputFormat.Jpg => true,
                OutputFormat.Png => false,
                OutputFormat.Webp => true,
                //OutputFormat.Tiff => false,
                //OutputFormat.heic => true,
                OutputFormat.avif => true,
                OutputFormat.Gif => false
            };
        }
    }
}
