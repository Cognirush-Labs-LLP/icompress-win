using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    public static class HumanReadable
    {
        /// <summary>
        /// Converts a file size in bytes to a human-readable string format.
        /// Supports B, KB, MB, GB, TB, PB up to exabytes.
        /// </summary>
        /// <param name="fileSize">The file size in bytes (ulong).</param>
        /// <returns>Formatted string with appropriate unit (B, KB, MB, GB, etc.).</returns>
        public static string FileSize(ulong fileSize)
        {
            // Define size units in increasing order
            string[] sizeUnits = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double size = fileSize;
            int unitIndex = 0;

            // Convert size to appropriate unit
            while (size >= 1024 && unitIndex < sizeUnits.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            // Format size with at most 2 decimal places, ensuring clipping behavior
            string formattedSize = size.ToString("0.##", CultureInfo.InvariantCulture);

            return $"{formattedSize} {sizeUnits[unitIndex]}";
        }

        /// <summary>
        /// Converts a file size in bytes to a human-readable string format.
        /// Supports B, KB, MB, GB, TB, PB up to exabytes.
        /// </summary>
        /// <param name="fileSize">The file size in bytes (ulong).</param>
        /// <returns>Formatted string with appropriate unit (B, KB, MB, GB, etc.).</returns>
        public static (decimal, FileSizeUnit)  FileSizeAndUnit(ulong fileSize)
        {
            // Define size units in increasing order
            FileSizeUnit[] sizeUnits = { FileSizeUnit.B, FileSizeUnit.KB, FileSizeUnit.MB };
            decimal size = fileSize;
            int unitIndex = 0;

            // Convert size to appropriate unit
            while (size >= 1024 && unitIndex < sizeUnits.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            // Format size with at most 2 decimal places, ensuring clipping behavior
            string formattedSize = size.ToString("0.##", CultureInfo.InvariantCulture);

            return (size, sizeUnits[unitIndex]);
        }
    }
}
