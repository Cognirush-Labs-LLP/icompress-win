using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// Returns the file name if the path is a file, otherwise returns the directory name.
        /// </summary>
        public string DisplayName => IsDirectory ? new DirectoryInfo(Path).Name : System.IO.Path.GetFileName(Path);

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
        public bool IncludeSubDirectories
        {
            get => GetProperty<bool>();
            set
            {
                SetProperty(value, (_, _) =>
                {
                    ChangeToIncludeSubDirectories(value);
                });
            }
        }

        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// A read-only list of media files found in the selected path.
        /// </summary>
        public IReadOnlyList<MediaFileInfo> Files
        {
            get
            {
                using (_lockForFiles.ReadLock())
                    return _files.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Useful in UI when selected path is a file and not a directory.
        /// </summary>
        public MediaFileInfo? FirstFile => Files.FirstOrDefault();

        private List<MediaFileInfo> _files = new List<MediaFileInfo>();
        private readonly ReaderWriterLockSlim _lockForFiles = new ReaderWriterLockSlim();
        private readonly object _lockForScannerThread = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedPath"/> class.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="includeSubDirectories">Specifies whether to scan subdirectories.</param>
        public SelectedPath(string path, bool includeSubDirectories)
        {
            if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
                throw new ArgumentException("Invalid path.", nameof(path));

            Path = path;
            IsDirectory = Directory.Exists(path);
            this.IncludeSubDirectories = includeSubDirectories;

            //ScanForMediaFiles().ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the settings to include or exclude sub-directories. Make sure UI looks for `ScanningForFiles` and disable the toggle control to avoid inconsistency. 
        /// </summary>
        /// <param name="includeSubDirectories"></param>
        public void ChangeToIncludeSubDirectories(bool includeSubDirectories)
        {
            this.IncludeSubDirectories = includeSubDirectories;
            _ = Task.Run(async () =>
            {
                try
                {
                    await ScanForMediaFiles().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Cancels the ongoing file scanning operation, it may take some time to cancel the operation.
        /// Should be called before changing the include subdirectories setting and before removing the path from the store.
        /// </summary>
        public void CancelScanning()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Cleanup()
        {
            for (int i = 0; i < 1000 && ScanningForFiles; i++) // wait for 1 second max if still scanning
                Thread.Sleep(1);

            _cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Scans the directory asynchronously and updates the file list.
        /// Only files with supported extensions are included.
        /// </summary>
        private async Task ScanForMediaFiles()
        {
            CancelScanning();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            ScanningForFiles = true;
            try
            {
                string basePath = Path;
                if (File.Exists(basePath) &&
                CodeConsts.SupportedInputExtensions.Contains(System.IO.Path.GetExtension(basePath).TrimStart('.').ToLower())) // if file, just at it as supported input file. 
                {
                    using (_lockForFiles.WriteLock())
                    {
                        _files.Clear();
                        _files.Add(new MediaFileInfo(basePath, new FileInfo(basePath)));
                    }
                    OnPropertyChanged(nameof(Files));
                    OnPropertyChanged(nameof(FirstFile));
                }
                else
                {
                    await Task.Run(() =>
                    {
                        lock (_lockForScannerThread)
                        {
                            using (_lockForFiles.WriteLock())
                            {
                                _files.Clear();
                            }
                            OnPropertyChanged(nameof(Files));
                            PopulateAllFilesForSupportedExtension(CodeConsts.SupportedInputExtensionsWithDot, Path, Path, IncludeSubDirectories, cancellationToken);
                        }
                    }, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("File scanning was canceled.");
            }
            finally
            {
                ScanningForFiles = false;
                OnPropertyChanged(nameof(Files));
            }
        }

        /// <summary>
        /// Recursively scans the given `rootFolderPath` and populates files in batches.
        /// Supports cancellation through `CancellationToken`.
        /// </summary>
        /// <param name="SupportedInputExtensions">Small cased extensions without preceding "." e.g., ["jpg", "jpeg", "png"]</param>
        /// <param name="rootFolderPath">Selected Path by user to search the images within</param>
        /// <param name="includeSubDir">Should search directories within `rootFolderPath` or not</param>
        /// <param name="cancellationToken">Token to cancel the ongoing operation</param>
        private void PopulateAllFilesForSupportedExtension(HashSet<string> supportedInputExtensions, string directoryToScan, string originalSelectedDirectory, bool includeSubDir, CancellationToken cancellationToken)
        {
            DirectoryInfo di = new DirectoryInfo(directoryToScan);


            try
            {
                cancellationToken.ThrowIfCancellationRequested();  // Check for cancellation

                var fiArr = di.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                             .Where(file => supportedInputExtensions.Contains(file.Extension.ToLower()));

                var newFiles = fiArr.Select(file =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new MediaFileInfo(originalSelectedDirectory, file);
                }).ToList();

                if (newFiles.Any())
                {
                    using (_lockForFiles.WriteLock())
                    {
                        _files.AddRange(newFiles);
                    }
                    OnPropertyChanged(nameof(Files));
                }

                if (includeSubDir)
                {
                    /*foreach (DirectoryInfo subDir in di.EnumerateDirectories())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Recursively yield results from subdirectories
                        PopulateAllFilesForSupportedExtension(SupportedInputExtensions, subDir.FullName, includeSubDir, cancellationToken);
                    }*/
                    Parallel.ForEach(
                        di.EnumerateDirectories(),
                        new ParallelOptions { CancellationToken = cancellationToken },
                        subDir =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            PopulateAllFilesForSupportedExtension(supportedInputExtensions, subDir.FullName, originalSelectedDirectory, includeSubDir, cancellationToken);
                        });
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Cancellation requested while scanning directory: {di.FullName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while scanning directory: {di.FullName}. Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Manages a thread-safe collection of selected file paths.
    /// </summary>
    public class FileStore : ObservableBase
    {
        private readonly List<SelectedPath> _store = new();
        private readonly ObservableCollection<SelectedPath> _uiStore = new(); // UI-friendly ObservableCollection

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly object _uiStoreLock = new(); //plain simle lock for observable collection for UI. Optimizaiton not required.


        /// <summary>
        /// Retrieves a thread-safe read-only collection of selected paths.
        /// </summary>
        /*public IReadOnlyCollection<SelectedPath> SelectedPaths
        {
            get
            {
                using (_lock.ReadLock())
                    return _store.AsReadOnly();
            }
        }*/
        public ObservableCollection<SelectedPath> SelectedPaths => _uiStore;

        /// <summary>
        /// Retrieves all unique media files across selected paths.
        /// </summary>
        /// <remarks>This is dynamically created list, be mindful and cache for multiple access. </remarks>
        public IReadOnlyCollection<MediaFileInfo> GetAllFiles
        {
            get
            {
                int totalWaitMs = 0;

                IReadOnlyCollection<SelectedPath> tempSelectedPathList;
                using (_lock.ReadLock())
                    tempSelectedPathList = _store.AsReadOnly();

                while (tempSelectedPathList.Any(selection => selection.ScanningForFiles) && totalWaitMs < 10 * 1000) //wait for scanning to finish, max 10 seconds
                {
                    Thread.Sleep(10);
                    totalWaitMs += 10;
                }

                using (_lock.ReadLock())
                    return _store.SelectMany(sp => sp.Files).DistinctBy(media => media.FileToCompress.FullName).ToList().AsReadOnly();
            }
        }


        /// <summary>
        /// Adds a new path to the store. This method returns the result just after making entry in selected path but all files to process will populate only after 
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <param name="scanSubDirectories">Indicates whether subdirectories should be scanned.</param>
        /// <returns>The result of the add operation.</returns>
        public PathAddedResult Enqueue(string path, bool scanSubDirectories = false)
        {
            path = Path.GetFullPath(path);
            using (_lock.WriteLock())
            {
                if (_store.Any(sp => sp.Path.TrimEnd('\\').Equals(path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                    return PathAddedResult.AlreadyExists;

                try
                {
                    var selectedPath = new SelectedPath(path, scanSubDirectories);
                    _store.Add(selectedPath);
                    UIThreadHelper.RunOnUIThread(() =>
                    {
                        lock (_uiStoreLock)
                            if (!_uiStore.Contains(selectedPath)) _uiStore.Add(selectedPath);
                        OnPropertyChanged(nameof(SelectedPaths));
                    });

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
                UIThreadHelper.RunOnUIThread(() =>
                {
                    lock (_uiStoreLock)
                        _uiStore.Remove(selectedPath);
                    OnPropertyChanged(nameof(SelectedPaths));
                });
                selectedPath.CancelScanning();
                //OnPropertyChanged(nameof(SelectedPaths));
                return true;
            }
        }

        /// <summary>
        /// Change setting of scanning the added path to include Subdirectories or not. No change if settings are not changed. 
        /// </summary>
        /// <param name="path">The path to remove.</param>
        /// <param name="includeSubDirectories">Should Include Sub-Directories or not</param>
        /// <returns>True if the path was found as selected path and will be taken up for making setting change if aplicable.</returns>
        public bool ChangeIncludeSubDirectoriesSetting(string path, bool includeSubDirectories)
        {
            using (_lock.ReadLock())
            {
                var selectedPath = _store.FirstOrDefault(sp => sp.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (selectedPath == null) return false;

                selectedPath.ChangeToIncludeSubDirectories(includeSubDirectories);

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
                _store.ForEach(selectedPath => selectedPath.CancelScanning());
                _store.Clear();
                UIThreadHelper.RunOnUIThread(() =>
                {
                    lock (_uiStoreLock)
                        _uiStore.Clear();
                    OnPropertyChanged(nameof(SelectedPaths));
                });
                OnPropertyChanged(nameof(SelectedPaths));
            }
        }
    }
}