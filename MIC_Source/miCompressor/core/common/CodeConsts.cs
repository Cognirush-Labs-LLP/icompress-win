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
        public static int ThumbSize = 100;

        /// <summary>
        /// Supported lower cased input file extensions without dot. i.e. "jpg", "jpeg", "png", "webp".
        /// </summary>
        public static string[] SupportedInputExtensions = ["jpg", "jpeg", "png", "webp"];


        /// <summary>
        /// Supported lower cased output file extensions without dot. i.e. "jpg", "png", "webp"
        /// </summary>
        public static string[] SupportedOutputExtensions = ["jpg", "png", "webp"];

        /// <summary>
        /// Generated from <![CDATA[SupportedInputExtensions]]> but file extensions with dot. i.e. ".jpg", ".jpeg", ".png", ".webp"
        /// </summary>
        public static HashSet<string> SupportedInputExtensionsWithDot
            => new HashSet<string>(SupportedInputExtensions.Select(ext => $".{ext}"));

        public static string compressedDirName = "Compressed";
    }
}
