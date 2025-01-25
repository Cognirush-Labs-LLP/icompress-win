using miCompressor.core.common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;

namespace miCompressor.core
{
    /// <summary>
    /// Represents the result of attempting to add a path.
    /// </summary>
    public enum PathAddedResult
    {
        Success,
        AlreadyExists,
        InvalidPath
    }

    /// <summary>
    /// Represents a selected file or directory path that can be scanned for media files.
    /// </summary>
    public partial class SelectedPath : ObservableBase
    {
        /// <summary>
        /// The absolute path of the selected file or directory.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Indicates whether the selected path is a directory.
        /// </summary>
        public bool IsDirectory { get; }

        /// <summary>
        /// Indicates whether the directory is currently being scanned.
        /// Automatically notifies UI when changed.
        /// </summary>
        [AutoNotify]
        private bool scanningForFiles = false;

        /// <summary>
        /// Determines whether subdirectories should be included in the scan.
        /// Changing this property triggers a new scan.
        /// </summary>
        [AutoNotify]
        private bool includeSubDirectories;
        
        /// <summary>
        /// A read-only list of media files found in the selected path.
        /// </summary>
        public IReadOnlyList<MediaFileInfo> Files => (IReadOnlyList<MediaFileInfo>)_files;
        private IList<MediaFileInfo> _files = new List<MediaFileInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedPath"/> class.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="includeSubDirs">Specifies whether to scan subdirectories.</param>
        public SelectedPath(string path, bool includeSubDirs)
        {
            if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
                throw new ArgumentException("Invalid path.", nameof(path));

            Path = path;
            IsDirectory = Directory.Exists(path);
            includeSubDirectories = includeSubDirs;

            ScanForMediaFiles().ConfigureAwait(false);
        }

        /// <summary>
        /// Scans the directory asynchronously and updates the file list.
        /// Only files with supported extensions are included.
        /// </summary>
        private async Task ScanForMediaFiles()
        {
            ScanningForFiles = true;

            await Task.Run(() =>
            {
                var mediaFileInfos = new List<MediaFileInfo>();

                string basePath = Path;
                if (File.Exists(basePath) &&
                CodeConsts.SupportedInputExtensions.Contains(System.IO.Path.GetExtension(basePath).TrimStart('.').ToLower())) // if file, just at it as supported input file. 
                {
                    mediaFileInfos.Add(new MediaFileInfo(basePath, new FileInfo(basePath)));
                }
                else
                {
                    var supportedInputFiles = new List<FileInfo>();
                    PopulateAllFilesForSupportedExtension(CodeConsts.SupportedInputExtensions, basePath, includeSubDirectories, supportedInputFiles);

                    foreach (var supportedFile in supportedInputFiles)
                        mediaFileInfos.Add(new MediaFileInfo(basePath, supportedFile));
                }

                _files = mediaFileInfos;
                raisePropertyChanged(nameof(Files));

                ScanningForFiles = false;
            });
        }

        /// <summary>
        /// Recursively scans given `rootFolderPath` and populates `files`
        /// </summary>
        /// <param name="SupportedInputExtensions">Small cased extensions without preceding "." e.g. ["jpg", "jpeg", "png"]</param>
        /// <param name="rootFolderPath">Selected Path by user to search the images within</param>
        /// <param name="inlcudeSubDir">Should search directories within `rootFolderPath' or not.</param>
        /// <param name="files">Pass an empty list, this will be populated with supported files inside `rootFolderPath`</param>
        private void PopulateAllFilesForSupportedExtension(string[] SupportedInputExtensions, string rootFolderPath, bool inlcudeSubDir, List<FileInfo> files)
        {
            DirectoryInfo di = new DirectoryInfo(rootFolderPath);

            try
            {
                var fiArr = di.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => SupportedInputExtensions.Contains(file.Extension.TrimStart('.').ToLower()));

                files.AddRange(fiArr);

                if (inlcudeSubDir)
                    foreach (DirectoryInfo info in di.GetDirectories())
                        PopulateAllFilesForSupportedExtension(SupportedInputExtensions, info.FullName, inlcudeSubDir, files);
            }
            catch
            {
                //Most probable error: Unable to read the directory (access right issue). Ignore those directories.
                return;
            }
        }
    }

    /// <summary>
    /// Manages a thread-safe collection of selected file paths.
    /// </summary>
    public class FileStore : ObservableBase
    {
        private readonly List<SelectedPath> _store = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Retrieves a thread-safe read-only collection of selected paths.
        /// </summary>
        public IReadOnlyCollection<SelectedPath> SelectedPaths
        {
            get
            {
                using (_lock.ReadLock())
                    return _store.AsReadOnly();
            }
        }

        /// <summary>
        /// Retrieves all unique media files across selected paths.
        /// </summary>
        /// <remarks>This is dynamically created list, be mindful and cache for multiple access. </remarks>
        public IReadOnlyCollection<MediaFileInfo> GetAllFiles
        {
            get
            {
                using (_lock.ReadLock())
                    return _store.SelectMany(sp => sp.Files).DistinctBy(media => media.fileToProcess.FullName).ToList().AsReadOnly();
            }
        }


        /// <summary>
        /// Adds a new path to the store, ensuring thread safety.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="scanSubDirectories">Indicates whether subdirectories should be scanned.</param>
        /// <returns>The result of the add operation.</returns>
        public PathAddedResult Add(string path, bool scanSubDirectories = false)
        {
            using (_lock.WriteLock())
            {
                if (_store.Any(sp => sp.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    return PathAddedResult.AlreadyExists;

                try
                {
                    var selectedPath = new SelectedPath(path, scanSubDirectories);
                    _store.Add(selectedPath);
                    raisePropertyChanged(nameof(SelectedPaths));
                    return PathAddedResult.Success;
                }
                catch (ArgumentException)
                {
                    return PathAddedResult.InvalidPath;
                }
            }
        }

        /// <summary>
        /// Removes a specified path from the store in a thread-safe manner.
        /// </summary>
        /// <param name="path">The path to remove.</param>
        /// <returns>True if the path was removed, false if it was not found.</returns>
        public bool Remove(string path)
        {
            using (_lock.WriteLock())
            {
                var selectedPath = _store.FirstOrDefault(sp => sp.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (selectedPath == null) return false;

                _store.Remove(selectedPath);
                raisePropertyChanged(nameof(SelectedPaths));
                return true;
            }
        }

        /// <summary>
        /// Remove all stored paths in a thread-safe manner.
        /// </summary>
        public void RemoveAll()
        {
            using (_lock.WriteLock())
            {
                _store.Clear();
                raisePropertyChanged(nameof(SelectedPaths));
            }
        }
    }
}