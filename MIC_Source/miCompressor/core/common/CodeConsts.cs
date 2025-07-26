using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace miCompressor.core
{
    public static class CodeConsts
    {

        public const bool IsForStoreDistribution = true;

        /// <summary>
        /// Size of Thumbnail in pixels to show image list in UI
        /// </summary>
        public const int ThumbSize = 100;

        /// <summary>
        /// Supported lower cased input file extensions without dot. i.e. "jpg", "jpeg", "png", "webp". We will add all supported image formats later, this is just for testing. 
        /// </summary>
        public static string[] SupportedInputExtensions = ["jpg", "jpeg", "png", "webp","gif","tif", "tiff", "avif", "heic", "jp2", "bmp", "svg",
            "arw", "dng", "nef", "cr2", "cr3","crw","dcr","mrw", "orf","raf", "pef", "raw","rw2", "srw","erf","3fr","kdc", "dcr","mos", "mef"];

        /// <summary>
        /// Extension used by cameras to create RAW files, TIFF not included.
        /// </summary>
        public static HashSet<string> SupportedCameraExtensionsWithDot =>
            new HashSet<string>(new[]
            {
                "arw", "dng", "nef", "cr2", "cr3", "crw", "dcr", "mrw", "orf",
                "raf", "pef", "raw", "rw2", "srw", "erf", "3fr", "kdc", "dcr",
                "mos", "mef"
            }.Select(ext => $".{ext}"));

        //TESTED NOT SUPPORTED: X3F


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
