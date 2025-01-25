using System;
using System.IO;

namespace miCompressor.core
{

    public static class PathHelper
    {
        /// <summary>
        /// Converts a standard Windows path to a long path format if needed.
        /// - Adds `\\?\` prefix only if:
        ///   1. The path is longer than 260 characters.
        ///   2. It is not already in long path format (`\\?\` or `\\?\UNC\`).
        /// - Works for both local and UNC paths.
        /// </summary>
        /// <param name="path">The original file or directory path.</param>
        /// <returns>The path converted to a long path format if required.</returns>
        public static string ConvertToLongPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize path separators
            path = Path.GetFullPath(path);

            // Check if path already has a long path prefix
            if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return path; // Already in long path format
            }

            // Convert to long path format only if path exceeds MAX_PATH
            if (path.Length > 260)
            {
                if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase)) // UNC path (network share)
                {
                    return @"\\?\UNC" + path.Substring(1); // Replace `\\` with `\\?\UNC`
                }
                else // Local path
                {
                    return @"\\?\" + path;
                }
            }

            return path; // Return original if it's short enough
        }
    }
}
