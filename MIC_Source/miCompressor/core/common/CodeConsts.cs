using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    public static class CodeConsts
    {
        /// <summary>
        /// Size of Thumbnail in pixels to show image list in UI
        /// </summary>
        public const int ThumbSize = 100;

        /// <summary>
        /// Supported lower cased input file extensions without dot. i.e. "jpg", "jpeg", "png", "webp". We will add all supported image formats later, this is just for testing. 
        /// </summary>
        public static string[] SupportedInputExtensions = ["jpg", "jpeg", "png", "webp","gif","tif", "tiff", "avif", "heic", "jp2", "bmp", "arw", "dng", "nef", "cr2", "cr3"];


        /// <summary>
        /// Supported lower cased output file extensions without dot. i.e. "jpg", "png", "webp"
        /// </summary>
        public static string[] SupportedOutputExtensions = ["jpg", "png", "webp", "tiff", "avif"];

/// <summary>
        /// Generated from <![CDATA[SupportedInputExtensions]]> but file extensions with dot. i.e. ".jpg", ".jpeg", ".png", ".webp"
        /// </summary>
        public static HashSet<string> SupportedInputExtensionsWithDot
            => new HashSet<string>(SupportedInputExtensions.Select(ext => $".{ext}"));

        public static string compressedDirName = "Compressed";
    }
}
