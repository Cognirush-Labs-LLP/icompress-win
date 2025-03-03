using System;
using System.Collections.Generic;
using ImageMagick;

namespace miCompressor.core
{

    public enum MetadataCopyMode { None, All, AllExceptSensitive }

    /// <summary>
    /// Provides helper methods for filtering and copying image metadata (EXIF, IPTC, XMP).
    /// </summary>
    public static class MetadataHelper
    {
        /// <summary>
        /// Defines modes for metadata copying.
        /// </summary>

        // Predefined sensitive Exif tags to exclude (personal or sensitive information).
        private static readonly HashSet<ExifTag> SensitiveExifTags = new()
        {
            ExifTag.MakerNote,
            ExifTag.UserComment,
            ExifTag.OwnerName,
            ExifTag.SerialNumber,
            ExifTag.LensSerialNumber,
            ExifTag.GPSLatitude, ExifTag.GPSLongitude, ExifTag.GPSAltitude,
            ExifTag.GPSLatitudeRef, ExifTag.GPSLongitudeRef, ExifTag.GPSAltitudeRef,
            ExifTag.GPSProcessingMethod, ExifTag.GPSAreaInformation, ExifTag.GPSDateStamp,
            // We exclude all GPS by removing known GPS tags; also remove orientation if image was transformed.
            
        };

        private static readonly HashSet<ExifTag> AlwaysSkipExifTags = new()
        {
            ExifTag.Orientation //We should orient image image in code while compressing instead of adding an extra information in EXIF (also, we need to support stripping metadata)
        };

        /// <summary>
        /// Copies metadata from a source image to a destination image, filtering out unwanted fields.
        /// </summary>
        /// <param name="source">The source image containing original metadata.</param>
        /// <param name="dest">The destination image to apply metadata to.</param>
        /// <param name="mode">Metadata copy mode (All, None, AllExceptSensitive).</param>
        public static void FilterAndCopyMetadata(IMagickImage<byte> source, IMagickImage<byte> dest, MetadataCopyMode mode)
        {
            if (mode == MetadataCopyMode.None)
            {
                // Remove all metadata from destination
                dest.Strip();
                return;
            }

            // Copy EXIF profile if present

            IExifProfile? exif = source.GetExifProfile();
            if (exif != null)
            {
                RemoveUnnecessaryExifTags(exif);

                if (mode == MetadataCopyMode.AllExceptSensitive)
                {
                    RemoveSensitiveExifTags(exif);
                }
                // Add/replace the EXIF profile in destination
                dest.SetProfile(exif);
            }


            // Copy IPTC profile if present
            IIptcProfile? iptc = source.GetIptcProfile();
            if (iptc != null)
            {
                if (mode == MetadataCopyMode.AllExceptSensitive)
                {
                    // Remove any sensitive IPTC data (e.g., contact info) if needed.
                    // Implementation can remove specific IPTC tags like Byline, Credit, etc., if deemed sensitive.
                    // For now, we assume IPTC data is not sensitive. Our assumption can be horribly wrong as IPTC is nothing but source information.
                    // We expect user adding IPTC data to image is not considering it sensitive. If they do, they should strip all metadata.
                    //TODO: Warn user about this. 
                }
                dest.SetProfile(iptc);
            }

            // Copy XMP profile if present
            IXmpProfile? xmp = source.GetXmpProfile();
            if (xmp != null)
            {
                if (mode == MetadataCopyMode.AllExceptSensitive)
                {
                    // XMP may contain duplicates of EXIF/IPTC data, including sensitive info.
                    // Easiest approach: do NOT copy XMP in sensitive mode to avoid leaking GPS or personal info.
                    // (Optionally, parse and remove sensitive fields from XMP if needed.)
                }
                if (mode == MetadataCopyMode.All)
                {
                    dest.SetProfile(xmp);
                }
            }
        }

        /// <summary>
        /// Remove sensitive tags from an Exif profile in-place.
        /// </summary>
        private static void RemoveSensitiveExifTags(IExifProfile exif)
        {
            // Remove each sensitive tag if present
            foreach (ExifTag tag in SensitiveExifTags)
            {
                exif.RemoveValue(tag);
            }
        }

        /// <summary>
        /// Remove some tags which we have anyway taken care (like correcting the orientation).
        /// </summary>
        private static void RemoveUnnecessaryExifTags(IExifProfile exif)
        {
            // Remove each sensitive tag if present
            foreach (ExifTag tag in AlwaysSkipExifTags)
            {
                exif.RemoveValue(tag);
            }
        }
    }
}
