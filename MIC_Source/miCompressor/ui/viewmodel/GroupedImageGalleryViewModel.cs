using miCompressor.core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace miCompressor.ui.viewmodel
{
    public class GroupedImageGalleryViewModel
    {
        public bool IsTreeView = true;

        public ObservableCollection<ImageGroup> ImageGroups { get; } = new();
        private object _itemgroupLock = new object();

        public ObservableCollection<ImageTreeNode> ImageTree { get; } = new();
        private readonly object _treeLock = new();

        private SelectedPath _currentSelectedPath;

        public void LoadData(SelectedPath selectedPath)
        {
            if (_currentSelectedPath != null)
                _currentSelectedPath.PropertyChanged -= SelectedPath_PropertyChanged;

            if (selectedPath == null)
                return;

            _currentSelectedPath = selectedPath;
            _currentSelectedPath.PropertyChanged += SelectedPath_PropertyChanged;

            RefreshImagesWithThrottled();
        }

        private Timer _throttleTimer;
        private readonly object _throttleLock = new object();
        private bool _isThrottlingActive = false;

        private void RefreshImagesWithThrottled(int throttleTimeInMs = 100)
        {
            lock (_throttleLock)
            {
                if (_isThrottlingActive) return; // Ignore if already scheduled

                _isThrottlingActive = true;

                if (_throttleTimer == null)
                {
                    _throttleTimer = new Timer(_ =>
                    {
                        UIThreadHelper.RunOnUIThread(() =>
                        {
                            if (IsTreeView) RefreshTreeView();
                            else RefreshImageGroups();

                            lock (_throttleLock) { _isThrottlingActive = false; } // Allow new scheduling after execution
                        });
                    }, null, throttleTimeInMs, Timeout.Infinite);
                }
                else
                {
                    _throttleTimer.Change(throttleTimeInMs, Timeout.Infinite);
                }
            }
        }


        private void RefreshImageGroups()
        {
            if (_currentSelectedPath?.Files == null)
                return;

            var groupedFiles = _currentSelectedPath.Files
                .GroupBy(file => GetParentDirectory(_currentSelectedPath.Path, file.FileToCompress.Directory?.FullName ?? string.Empty))
                .Select(group => new ImageGroup(group.Key, group.ToList()))
                .Where(g => g.Images.Any())
                .OrderBy(g => g.FolderPath);

            lock (_itemgroupLock)
                UpdateImageGroups(groupedFiles);
        }

        private void UpdateImageGroups(IEnumerable<ImageGroup> newGroups)
        {
            // Efficiently update the ImageGroups collection. Hope is to reduce the UI refresh calls. 
            // Remove groups that no longer exist and update/add new ones.

            // Remove groups that no longer exist.
            var groupsToRemove = ImageGroups.Where(existingGroup =>
                !newGroups.Any(newGroup => newGroup.FolderPath == existingGroup.FolderPath)).ToList();

            foreach (var group in groupsToRemove)
            {
                ImageGroups.Remove(group);
            }

            // Add or update existing groups.
            foreach (var newGroup in newGroups)
            {
                var existingGroup = ImageGroups.FirstOrDefault(g => g.FolderPath == newGroup.FolderPath);
                if (existingGroup == null)
                {
                    ImageGroups.Add(newGroup);
                }
                else
                {
                    // Update the image list for the existing group.
                    existingGroup.UpdateImages(newGroup.Images);
                }
            }
        }

        private void RefreshTreeView()
        {
            if (_currentSelectedPath?.Files == null || !_currentSelectedPath.Files.Any())
            {
                ImageTree.Clear();
                return;
            }

            var rootNode = new ImageTreeNode(_currentSelectedPath.Path, true);
            Dictionary<string, ImageTreeNode> nodeLookup = new()
            {
                { _currentSelectedPath.Path, rootNode }
            };

            var sortedFiles = _currentSelectedPath.Files
                .OrderBy(file => file.FileToCompress.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in sortedFiles)
            {
                AddToTree(rootNode, file, nodeLookup);
            }

            ShortenTree(rootNode);
            rootNode.FreezeAndSubscribeToChangeInChildStatusImageTreeNode();
            lock (_treeLock)
            {
                ImageTree.Clear();
                ImageTree.Add(rootNode);
                //rootNode.UpdateFolderCounts(); // Make sure folder counts are updated
            }
        }

        private void AddToTree(ImageTreeNode root, MediaFileInfo file, Dictionary<string, ImageTreeNode> nodeLookup)
        {
            string directoryPath = file.FileToCompress.DirectoryName ?? root.Name;

            if (!nodeLookup.TryGetValue(directoryPath, out var parentNode))
            {
                parentNode = CreateFolderNodes(root, directoryPath, nodeLookup);
            }

            var fileNode = new ImageTreeNode(file.FileToCompress.FullName, false)
            {
                FileInfo = file
            };

            parentNode.Children.Add(fileNode);
        }

        private ImageTreeNode CreateFolderNodes(ImageTreeNode root, string fullPath, Dictionary<string, ImageTreeNode> nodeLookup)
        {
            if (nodeLookup.TryGetValue(fullPath, out var existingNode))
                return existingNode; // Folder already exists

            string selectedRoot = root.Name; // Selected Path (e.g., "C:\abc\def")
            string currentPath = fullPath;
            Stack<string> missingFolders = new();

            // Walk upwards, but stop at the selected root
            while (!nodeLookup.ContainsKey(currentPath) && currentPath.StartsWith(selectedRoot, StringComparison.OrdinalIgnoreCase))
            {
                missingFolders.Push(Path.GetFileName(currentPath)); // Store missing folder names
                string parentPath = Path.GetDirectoryName(currentPath);

                if (string.IsNullOrEmpty(parentPath) || parentPath == currentPath || parentPath.Length < selectedRoot.Length)
                    break; // Prevents going above selected root

                currentPath = parentPath; // Move up one level
            }

            // Get the nearest existing parent node inside the selected path
            nodeLookup.TryGetValue(currentPath, out ImageTreeNode parentNode);
            if (parentNode == null)
                parentNode = root; // If no parent found, assign root as parent

            // create the missing folders from the stack
            while (missingFolders.Count > 0)
            {
                string folderName = missingFolders.Pop();
                var newFolderNode = new ImageTreeNode(folderName, true);
                parentNode.Children.Add(newFolderNode);
                parentNode = newFolderNode;
                currentPath = Path.Combine(currentPath, folderName);
                nodeLookup[currentPath] = newFolderNode;
            }

            return parentNode;
        }

        public static void ShortenTree(ImageTreeNode nodeToShorten)
        {
            // Only process folders.
            if (!nodeToShorten.IsFolder)
                return;

            // Merge nodes: while the node has exactly one child and that child is a folder,
            // merge the child’s ShortName into the parent and splice in its children.
            while (nodeToShorten.Children.Count == 1 && nodeToShorten.Children[0].IsFolder)
            {
                var child = nodeToShorten.Children[0];
                // Combine the ShortNames using the system’s directory separator.
                nodeToShorten.ShortName = $"{nodeToShorten.ShortName}{Path.DirectorySeparatorChar}{child.ShortName}";

                // Replace the current node's children with the merged child's children.
                nodeToShorten.Children.Clear();
                foreach (var grandchild in child.Children)
                {
                    nodeToShorten.Children.Add(grandchild);
                }
            }

            // Sort children: first folders (sorted by ShortName) then files (sorted by ShortName)
            var sortedChildren = nodeToShorten.Children
                .OrderBy(child => child.IsFolder ? 0 : 1)
                .ThenBy(child => child.ShortName)
                .ToList();
            nodeToShorten.Children.Clear();
            foreach (var child in sortedChildren)
            {
                nodeToShorten.Children.Add(child);
            }

            // Recursively process each child.
            foreach (var child in nodeToShorten.Children)
            {
                ShortenTree(child);
            }
        }


        private string GetParentDirectory(string rootPath, string subDirPath)
        {
            return subDirPath.StartsWith(rootPath) ? subDirPath : rootPath;
        }

        private void SelectedPath_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedPath.Files))
            {
                if (_currentSelectedPath?.ScanningForFiles == true)
                    return; // Ignore changes while scanning
                // Refresh image groups when the Files collection changes.
                RefreshImagesWithThrottled();
            }
        }
    }

    public class ImageGroup
    {
        public string FolderPath { get; }
        public ObservableCollection<MediaFileInfo> Images { get; }

        public ImageGroup(string folderPath, List<MediaFileInfo> images)
        {
            FolderPath = folderPath;
            Images = new ObservableCollection<MediaFileInfo>(images);
        }

        public void UpdateImages(IList<MediaFileInfo> newImages)
        {
            // Update the Images collection instead of replacing it to reduce UI refresh calls (hopefully).
            var imagesToRemove = Images.Where(existingImage =>
                !newImages.Any(newImage => newImage.FileToCompress.FullName == existingImage.FileToCompress.FullName)).ToList();

            foreach (var image in imagesToRemove)
            {
                Images.Remove(image);
            }

            foreach (var newImage in newImages)
            {
                if (!Images.Any(image => image.FileToCompress.FullName == newImage.FileToCompress.FullName))
                {
                    Images.Add(newImage);
                }
            }
        }
    }

    public class ImageTreeNode : ObservableBase
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
                if (value != null)
                    ShortName = Path.GetFileName(_name.TrimEnd(Path.DirectorySeparatorChar));
                else
                    ShortName = "";
            }
        }

        public string ShortName { get; set; }

        public bool IsFolder { get; }
        public bool IsImage => !IsFolder;

        public ObservableCollection<ImageTreeNode> Children { get; } = new();

        private MediaFileInfo? _fileInfo;
        public MediaFileInfo? FileInfo
        {
            get => _fileInfo;
            set
            {
                if (_fileInfo != null)
                {
                    _fileInfo.PropertyChanged -= FileInfo_PropertyChanged; // Unsubscribe from old instance, just in case!!
                }

                _fileInfo = value;

                if (_fileInfo != null)
                {
                    _fileInfo.PropertyChanged += FileInfo_PropertyChanged; // Subscribe to new instance
                }
            }
        }

        public string Dimensions => FileInfo != null ? $"{FileInfo.Width}×{FileInfo.Height}" : string.Empty;

        private ulong FileSizeInBytes = 0;
        private ulong SelectedFileSizeInBytes = 0;

        public string FileSize
        {
            get
            {
                if (FileInfo != null)
                    return FileInfo.FileSizeToShow;
                if (FileSizeInBytes != SelectedFileSizeInBytes)
                    return $"{HumanReadable.FileSize(fileSize: SelectedFileSizeInBytes)} of {HumanReadable.FileSize(fileSize: FileSizeInBytes)}";
                else
                    return $"{HumanReadable.FileSize(fileSize: FileSizeInBytes)}";
            }
        }
        public string DisplayText
        {
            get
            {
                if (IsFolder)
                {
                    int fileCount = Children.Count(child => !child.IsFolder);
                    return fileCount > 0 ? $"{ShortName} ({fileCount})" : ShortName;
                }
                else if (FileInfo != null)
                {
                    return $"{ShortName}    {FileInfo.Width}x{FileInfo.Height}    {FileInfo.FileSizeToShow}";
                }
                return ShortName;
            }
        }

        private bool _isIncluded;
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                _isIncluded = value;
                UnsubscribeToChangeInChildStatusImageTreeNode();
                if (FileInfo != null)
                    FileInfo.ExcludeAndShow = !value;
                // Apply the same value to all children recursively
                foreach (var child in Children)
                {
                    child.IsIncluded = value;
                }

                OnPropertyChanged(nameof(IsIncluded));
                OnPropertyChanged(nameof(SelectionState)); // Ensure UI updates
                CalculateSelectedImageFileCount();
                CalculateSelectedImageSize();
                OnPropertyChanged(nameof(SelectedFileCountString));
                SubscribeToChangeInChildStatusImageTreeNode();

            }
        }

        /// <summary>
        /// Gets the selection state for the CheckBox.
        /// </summary>
        public bool? SelectionState
        {
            get
            {
                if (Children.Count == 0)
                    return IsIncluded; // No children, binary state

                bool allSelected = Children.All(c => c.SelectionState == true);
                bool noneSelected = Children.All(c => c.SelectionState == false);

                if (allSelected)
                    return true;
                if (noneSelected)
                    return false;

                return null; // Indeterminate state
            }
        }

        public int ImageFileCount;
        public int SelectedFileCount;
        public string SelectedFileCountString
        {
            get
            {
                if (ImageFileCount != SelectedFileCount)
                    return $"{SelectedFileCount} of {ImageFileCount}";
                return $"{ImageFileCount}";
            }
        }

        public ImageTreeNode(string name, bool isFolder)
        {
            Name = name;
            IsFolder = isFolder;
        }

        /// <summary>
        /// Call this when tree is frozen to finalize the tree state, count image files, and subscribe to child changes (include/exclude images).
        /// </summary>
        public void FreezeAndSubscribeToChangeInChildStatusImageTreeNode()
        {
            IsIncluded = true;
            //SubscribeToChangeInChildStatusImageTreeNode();
            CalculateImageFileCount();
            CalculateSelectedImageFileCount();
            CalculateImageSize();
            CalculateSelectedImageSize();
        }

        /// <summary>
        /// Call this on Freeze to avoid any event leaks.
        /// </summary>
        private void SubscribeToChangeInChildStatusImageTreeNode()
        {
            // Subscribe to changes in children
            foreach (var child in Children)
            {
                child.PropertyChanged += Child_PropertyChanged;
            }
        }

        /// <summary>
        /// Call this on Freeze to avoid any event leaks.
        /// </summary>
        private void UnsubscribeToChangeInChildStatusImageTreeNode()
        {
            // Subscribe to changes in children
            foreach (var child in Children)
            {
                child.PropertyChanged -= Child_PropertyChanged;
            }
        }

        /// <summary>
        /// Handles child property changes to update the parent's SelectionState.
        /// </summary>
        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsIncluded))
            {
                // If any child's selection changes, update parent's selection state
                OnPropertyChanged(nameof(SelectionState));
                //System.Diagnostics.Debug.WriteLine($"{Name} was updated");
                OnPropertyChanged(nameof(IsIncluded));
                CalculateSelectedImageFileCount();
                OnPropertyChanged(nameof(SelectedFileCountString));
                CalculateSelectedImageSize();
            }
            if (e.PropertyName == nameof(FileSize))
            {
                ThrottleTask.Add(100, this.GetHashCode().ToString() + "CalculateImageSize", () =>
                {
                    CalculateImageSize();
                    CalculateSelectedImageSize();
                }, shouldRunInUI: true);

            }
        }


        /// <summary>
        /// Call this method when the tree is frozen to compute image count efficiently.
        /// </summary>
        private void CalculateImageFileCount()
        {
            ImageFileCount = GetImageFileCount(this);
        }

        private int GetImageFileCount(ImageTreeNode node)
        {
            if (node.IsImage)
                return 1;

            int count = 0;
            foreach (var child in node.Children)
            {
                count += GetImageFileCount(child);
            }
            node.ImageFileCount = count;
            return count;
        }

        private void CalculateSelectedImageFileCount()
        {
            SelectedFileCount = GetImageSelectedFileCount(this);
        }

        private int GetImageSelectedFileCount(ImageTreeNode node)
        {
            if (node.IsImage)
                return node.FileInfo == null ? 0 : ((node.FileInfo.ExcludeAndShow || node.FileInfo.ExcludeAndHide) ? 0 : 1);

            int count = 0;
            foreach (var child in node.Children)
            {
                count += GetImageSelectedFileCount(child);
            }
            node.SelectedFileCount = count;
            return count;
        }

        private void CalculateImageSize()
        {
            var oldFileSize = FileSizeInBytes;
            FileSizeInBytes = GetImageFileSizeOfAllChild(this);
            if (oldFileSize != FileSizeInBytes)
                OnPropertyChanged(nameof(FileSize));
        }

        /// <summary>
        /// Recursively counts image files (non-folder nodes).
        /// </summary>
        private ulong GetImageFileSizeOfAllChild(ImageTreeNode node)
        {
            if (node.IsImage)
                return node.FileInfo?.FileSize ?? 0;

            ulong count = 0;
            foreach (var child in node.Children)
            {
                count += GetImageFileSizeOfAllChild(child);
            }
            node.FileSizeInBytes = count;
            return count;
        }

        private void CalculateSelectedImageSize()
        {
            SelectedFileSizeInBytes = GetSelectedImageFileSizeOfAllChild(this);
            OnPropertyChanged(nameof(FileSize));
        }

        /// <summary>
        /// Recursively counts image files (non-folder nodes).
        /// </summary>
        private ulong GetSelectedImageFileSizeOfAllChild(ImageTreeNode node)
        {
            if (node.IsImage)
                return node.FileInfo == null ? 0 : ((node.FileInfo.ExcludeAndShow || node.FileInfo.ExcludeAndHide) ? 0 : node.FileInfo.FileSize);

            ulong count = 0;
            foreach (var child in node.Children)
            {
                count += GetSelectedImageFileSizeOfAllChild(child);
            }
            node.SelectedFileSizeInBytes = count;
            return count;
        }


        private void FileInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaFileInfo.Width) || e.PropertyName == nameof(MediaFileInfo.Height))
            {
                OnPropertyChanged(nameof(Dimensions));
            }
            else if (e.PropertyName == nameof(MediaFileInfo.FileSize))
            {
                OnPropertyChanged(nameof(FileSize));
            }
        }
    }
}
