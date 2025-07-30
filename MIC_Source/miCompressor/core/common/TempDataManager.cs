using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    /// <summary>
    /// Helper class for managing temporary data.
    /// </summary>
    public static class TempDataManager
    {
        /// <summary>
        /// Lock object for operations on temp directory.
        /// </summary>
        private static readonly object _tempDirOpLock = new object();

        /// <summary>
        /// Files older than this time are considered stale and are deleted when cleaning up temp directory.
        /// </summary>
        private static readonly double staleIfOlderThanHours = 2;


        /// <summary>
        /// Name of the directory where preview files are stored.
        /// </summary>
        private static readonly string preivewDirName = "preview";

        /// <summary>
        /// Name of the directory where compressed files are stored temporarily to be moved to designated location later if needed.
        /// </summary>
        private static readonly string cacheDirName = "cache";

        /// <summary>
        /// To store temporary data such as log file zip before sending/copying to email. 
        /// </summary>
        private static readonly string commonTempDirName = "temp_data";

        /// <summary>
        /// To store temporary data such as log file zip before sending/copying to email. 
        /// </summary>
        private static readonly string config = "user_config";

        /// <summary>
        /// Temporary directory of the application. All temporary files are stored here.
        /// This only returns the path, does not guarantee the directory exists.
        /// </summary>
        public static string tempAppDir
        {
            get
            {
                var dir = Path.Combine(Path.GetTempPath(), "miCompressor");
                return dir;
            }
        }

        /// <summary>
        /// Get the path of the compressed preview file. Caller is responsible for creating the directory if it doesn't exist.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string getTempPreviewFilePath(string fileName)
        {
            return Path.Combine(getTempPreviewDirPath(), fileName);
        }

        /// <summary>
        /// Get the path of the compressed preview directory. Caller is responsible for creating the directory if it doesn't exist.
        /// </summary>
        /// <returns>Preview directory path</returns>
        public static string getTempPreviewDirPath()
        {
            string dirPath = Path.Combine(tempAppDir, preivewDirName);
            Directory.CreateDirectory(dirPath); // Ensures the directory exists
            return dirPath;
        }

        /// <summary>
        /// Get the temp storage file path for storing compressed files temporarily to be moved to designated location later if applicable.
        /// Caller is responsible for creating the directory if it doesn't exist.
        /// </summary>
        /// <param name="dirPath">Relative directory from selected directory</param>
        /// <param name="fileName">Name of the output file</param>
        /// <returns></returns>
        public static string GetTempStorageFilePath(string dirPath, string fileName)
        {
            return Path.Combine(tempAppDir, cacheDirName, dirPath, fileName);
        }

        /// <summary>
        /// Get the temp storage path for any intermediate file such as log zip.
        /// </summary>
        /// <returns>Path of temp App dir.</returns>
        public static string GetCommonTempDir()
        {
            return Path.Combine(tempAppDir, commonTempDirName);
        }
        /// <summary>
        /// Get the temp storage directory path for storing compressed files temporarily to be moved to designated location later if applicable.
        /// </summary>
        /// <param name="dirPath">May provide dir or file path. If file path is provided, we still return dir, not file.</param>
        /// <returns></returns>
        public static string GetTempStorageDirPath(string dirPath)
        {
            string path = Path.Combine(tempAppDir, cacheDirName, dirPath);

            // Check if dirPath is a file and return its parent directory if it is
            if (File.Exists(path) || Path.HasExtension(dirPath))
            {
                return Path.GetDirectoryName(path) ?? tempAppDir;  // Fallback to tempAppDir if no directory found
            }

            return path;
        }

        /// <summary>
        /// Get the temp storage path for any user configuration data. Such as last used window size, compression settings etc.
        /// This is used for non-Store users. Store users use ApplicationData.Current.LocalSettings.
        /// </summary>
        /// <returns>
        /// Fully qualified path to the user config directory under %LOCALAPPDATA%\miCompressor\user_config.
        /// Ensures the directory exists before returning.
        /// </returns>
        public static string GetUserConfigDir()
        {
            string settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "miCompressor", config);
            Directory.CreateDirectory(settingsDir); 
            return settingsDir;
        }

        /// <summary>
        /// Get a unique temporary file path with the specified extension.
        /// </summary>
        /// <param name="extension">File extension including dot (e.g., ".jpg").</param>
        /// <returns>Path to a unique temporary file.</returns>
        public static string GetTempFile(string extension)
        {
            string tempFilePath;
            do
            {
                tempFilePath = Path.Combine(tempAppDir, cacheDirName, Path.GetRandomFileName() + extension);
            } while (File.Exists(tempFilePath));

            return tempFilePath;
        }

        /// <summary>
        /// Clean up temp directory, should be called when compression is done, or when application starts.
        /// Non-blocking operation.
        /// Ensure there is no other operation using temp directory before calling this.
        /// </summary>
        public static void CleanUpTempDir()
        {
            Task.Run(() =>
            {
                lock (_tempDirOpLock)
                {
                    try
                    {
                        if (Directory.Exists(Path.Combine(tempAppDir, preivewDirName)))
                            CleanDirectory(Path.Combine(tempAppDir, preivewDirName));
                        if (Directory.Exists(Path.Combine(tempAppDir, cacheDirName)))
                            CleanDirectory(Path.Combine(tempAppDir, cacheDirName));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($" * Error cleaning temp directory: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Clean up the directory by deleting stale files in the given directory and its subdirectories.
        /// </summary>
        /// <param name="dirPath"></param>
        private static void CleanDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return;

            // Delete files older than 2 hours
            foreach (var file in Directory.GetFiles(dirPath))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < DateTime.Now.AddHours(-1.0 * staleIfOlderThanHours))
                    {
                        fileInfo.Delete();
                        //System.Diagnostics.Debug.WriteLine($"Deleted file: {fileInfo.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($" * Failed to delete file {file}: {ex.Message}");
                }
            }

            // Recursively clean subdirectories
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                CleanDirectory(subDir);

                // After cleaning the subdirectory, delete it if it's empty
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(subDir).Any())
                        Directory.Delete(subDir);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($" * Failed to delete directory {subDir}: {ex.Message}");
                }
            }
        }
    }
}
