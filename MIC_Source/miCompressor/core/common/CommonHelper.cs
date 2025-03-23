using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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



        /// <summary>
        /// Given a collection of file/folder paths, returns the common parent folder 
        /// (i.e. the container folder for all the selected items). If a common parent cannot be found,
        /// or if any item is selected directly from a drive root, returns null.
        /// </summary>
        /// <param name="selectedPaths">A collection of file or folder paths.</param>
        /// <returns>The common parent folder or null if none exists.</returns>
        /// <remarks>We wrote this to get user near to their most probable file location, WinUI 3 doesn't allow setting initial location path in pickers (unless we use Widows Vista's picker). We may use this in future, or if we decide to dump all the possibilities of ever having a sandbox compliant app. </remarks>
        public static string GetCommonParent(IEnumerable<string> selectedPaths)
        {
            try
            {
                if (selectedPaths == null || selectedPaths.Count() == 0)
                    return null;

                /*if(selectedPaths.Count() == 1)
                {
                    var dirPath = selectedPaths.First();
                    if ( IsProbablyFile(dirPath) )
                        dirPath = Path.GetDirectoryName(dirPath);
                    return new DirectoryInfo(dirPath).Parent?.FullName;
                }*/

                // For each selected path, get the "container" folder:
                // - For files, we get Path.GetDirectoryName(path) then its parent.
                // - For folders, we get the parent folder.
                var containerPaths = new List<string>();

                foreach (var path in selectedPaths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    // Normalize the path (we already pass full path but this removes any trailing separator - just to make this method robust)
                    string fullPath = Path.GetFullPath(path);
                    string candidate = null;

                    if (IsProbablyFile(fullPath))
                    {
                        // For a file, get its containing folder and then the folder containing that folder.
                        string fileDir = Path.GetDirectoryName(fullPath);
                        if (string.IsNullOrEmpty(fileDir))
                            return null;

                        DirectoryInfo parentInfo = Directory.GetParent(fileDir);
                        candidate = parentInfo?.FullName;
                    }
                    else
                    {
                        // For a folder, we consider its container (i.e. its parent folder).
                        DirectoryInfo parentInfo = Directory.GetParent(fullPath);
                        candidate = parentInfo?.FullName;
                    }

                    // If candidate is null then either the item was in a drive root or invalid.
                    if (candidate == null)
                        return null;

                    containerPaths.Add(candidate);
                }

                if (containerPaths.Count == 0)
                    return null;

                // Compute the common path among all candidate container paths.
                string common = containerPaths[0];
                for (int i = 1; i < containerPaths.Count; i++)
                {
                    common = GetCommonPath(common, containerPaths[i]);
                    if (common == null)
                        return null;
                }
                return common;
            }
            catch
            {
                //This method is used just to suggest initial folder for file/folder selection dialogue, just nice to have. No need to crash the application if this logic fails for some reason. 
                return null;
            }
        }

        /// <summary>
        /// Determines whether the given path is “probably” a file by checking if it has an extension.
        /// (In many cases folders do not have extensions.)
        /// </summary>
        private static bool IsProbablyFile(string path)
        {
            return !string.IsNullOrEmpty(Path.GetExtension(path));
        }

        /// <summary>
        /// Computes the deepest common directory between two normalized paths.
        /// Returns null if the two paths do not share a common root.
        /// </summary>
        private static string GetCommonPath(string path1, string path2)
        {
            // Create DirectoryInfo objects for each path.
            var di1 = new DirectoryInfo(path1);
            var di2 = new DirectoryInfo(path2);

            // Build lists of ancestors starting from the root.
            var ancestors1 = new List<DirectoryInfo>();
            var ancestors2 = new List<DirectoryInfo>();

            while (di1 != null)
            {
                ancestors1.Insert(0, di1);
                di1 = di1.Parent;
            }
            while (di2 != null)
            {
                ancestors2.Insert(0, di2);
                di2 = di2.Parent;
            }

            // Find the common ancestor by comparing each level.
            int count = Math.Min(ancestors1.Count, ancestors2.Count);
            DirectoryInfo commonDir = null;
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(ancestors1[i].FullName, ancestors2[i].FullName, StringComparison.OrdinalIgnoreCase))
                    commonDir = ancestors1[i];
                else
                    break;
            }
            return commonDir?.FullName;
        }

        /// <summary>
        /// Checks if two paths refer to the same location, handling long paths and normalization.
        /// </summary>
        public static bool ArePathsEqual(string path1, string path2)
        {
            if (string.IsNullOrWhiteSpace(path1) || string.IsNullOrWhiteSpace(path2))
                return false;

            try
            {
                string fullPath1 = NormalizeLongPath(path1);
                string fullPath2 = NormalizeLongPath(path2);

                return string.Equals(fullPath1, fullPath2, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false; // Handle invalid paths gracefully
            }
        }

        /// <summary>
        /// Normalizes a path, handling long paths and ensuring a consistent format.
        /// </summary>
        private static string NormalizeLongPath(string path)
        {
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Handle long paths by adding \\?\ prefix if needed
            if (normalizedPath.Length >= 260 && !normalizedPath.StartsWith(@"\\?\"))
                normalizedPath = @"\\?\" + normalizedPath;
            
            return normalizedPath;
        }

        /// <summary>
        /// Gets folder path of the give path. Path may or may not exists.
        /// </summary>
        /// <param name="fileOrFolderPath">A valid path, not-null.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetFolderPath(string fileOrFolderPath)
        {
            // Check if the path is an existing directory or ends with a separator.
            if (Directory.Exists(fileOrFolderPath) ||
                fileOrFolderPath.EndsWith(Path.DirectorySeparatorChar) ||
                fileOrFolderPath.EndsWith(Path.AltDirectorySeparatorChar))
            {
                return fileOrFolderPath;
            }

            // Otherwise, treat it as a file path and get its parent folder.
            string? parentFolder = Path.GetDirectoryName(fileOrFolderPath);
            if (string.IsNullOrEmpty(parentFolder))
            {
                return fileOrFolderPath;
            }

            return parentFolder;
        }
    }
}

