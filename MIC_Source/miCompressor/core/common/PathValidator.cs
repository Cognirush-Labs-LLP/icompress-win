using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace miCompressor.core.common
{
    /// <summary>
    /// Provides methods to validate file paths.
    /// </summary>
    public static class PathValidator
    {
        /// <summary>
        /// Windows Reserved Names (Cannot be used as folder names)
        /// </summary>
        private static readonly string[] ReservedNames =
        { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
      "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        /// <summary>
        /// Checks if the given path is a valid file path. Checks for invalid characters, reserved names, and path length.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool IsValidFolderPath(string folderPath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                errorMessage = "Path is empty or only contains whitespace.";
                return false;
            }

            // Check for invalid path characters
            if (folderPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                errorMessage = "Path contains invalid characters.";
                return false;
            }

            // Ensure it's a valid absolute path
            if (!Path.IsPathRooted(folderPath))
            {
                errorMessage = "Path must be an absolute path \n(e.g., C:\\ValidFolder).";
                return false;
            }

            // Extract directory name (last part)
            string directoryName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar));

            // Check for invalid characters in the folder name, this is to filter some characters that are not allowed in folder names but GetInvalidPathChars doesn't catch. We have to have both checks (unfortunately).
            if (directoryName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                errorMessage = "Folder name contains invalid characters.";
                return false;
            }

            // Check against reserved names (case insensitive)
            if (ReservedNames.Any(rn => rn.Equals(directoryName, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"'{directoryName}' is a reserved system name and cannot be used as a folder name.";
                return false;
            }

            /* We will check this for files only. 
            // Check if the path length exceeds Windows limit
            if (folderPath.Length >= 260) // Adjust if long path support is enabled
            {
                errorMessage = "Path is too long. It must be under 260 characters.";
                return false;
            }*/

            return true;
        }
    }

}
