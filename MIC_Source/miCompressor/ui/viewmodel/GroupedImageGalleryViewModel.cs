using miCompressor.core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace miCompressor.ui.viewmodel
{
    public class GroupedImageGalleryViewModel
    {
        public ObservableCollection<ImageGroup> ImageGroups { get; } = new();
        private object _itemgroupLock = new object();

        private SelectedPath _currentSelectedPath;

        public void LoadData(SelectedPath selectedPath)
        {
            if (_currentSelectedPath != null)
                _currentSelectedPath.PropertyChanged -= SelectedPath_PropertyChanged;
            
            if (selectedPath == null)
                return;

            _currentSelectedPath = selectedPath;
            _currentSelectedPath.PropertyChanged += SelectedPath_PropertyChanged;

            RefreshImageGroupsThrottled();
        }

        private Timer _throttleTimer;
        private readonly object _throttleLock = new object();
        private bool _isThrottlingActive = false;

        private void RefreshImageGroupsThrottled(int throttleTimeInMs = 1000)
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
                            RefreshImageGroups();
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
                RefreshImageGroupsThrottled();
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
}
