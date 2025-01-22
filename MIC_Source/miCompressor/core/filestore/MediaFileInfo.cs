using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;

namespace miCompressor.core
{
    /// <summary>
    /// Represents an image file, storing metadata and relative path for compression.
    /// Uses async loading to prevent UI blocking and supports automatic UI updates.
    /// </summary>
    public partial class MediaFileInfo : ObservableBase
    {
        /// <summary>
        /// The root directory selected by the user.
        /// </summary>
        public string SelectedPath { get; }

        /// <summary>
        /// The full path to the image file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The relative path of the image within the selected directory.
        /// </summary>
        public string RelativePath => Path.GetRelativePath(SelectedPath, FilePath);

        [AutoNotify]
        private int width;

        [AutoNotify]
        private int height;

        [AutoNotify]
        private ulong fileSize;

        [AutoNotify]
        private string? cameraModel;

        [AutoNotify]
        private DateTimeOffset? dateTaken;

        /// <summary>
        /// Exclude the media from processing and hide the file in gallery, used for files are not eligible due to filter settings.
        /// </summary>
        [AutoNotify]
        private bool excludeAndHide = false;

        /// <summary>
        /// Exclude the media from processing but do not hide the file in gallery, may be show it greyed out
        /// </summary>
        [AutoNotify]
        private bool excludeAndShow = false;

        /// <summary>
        /// returns excludeAndHide && excludeAndShow
        /// </summary>
        public bool ShouldProcess
        {
            get { return excludeAndHide && excludeAndShow; }
        }

        public string FileSizeToShow
        {
            get { return HumanReadable.FileSize(fileSize: fileSize); }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MediaFileInfo"/>.
        /// </summary>
        /// <param name="selectedPath">The root directory selected by the user.</param>
        /// <param name="filePath">The full file path of the image.</param>
        public MediaFileInfo(string selectedPath, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            SelectedPath = selectedPath;
            FilePath = filePath;

            // Load metadata asynchronously
            _ = LoadImageMetadataAsync();
        }

        /// <summary>
        /// Loads image metadata asynchronously without blocking the UI.
        /// </summary>
        private async Task LoadImageMetadataAsync()
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
                ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
               
                Width = (int)properties.Width;
                Height = (int)properties.Height;
                FileSize = (await file.GetBasicPropertiesAsync()).Size;
                raisePropertyChanged(nameof(FileSizeToShow));
                CameraModel = properties.CameraModel;
                DateTaken = properties.DateTaken;

                Console.WriteLine(width);
                Console.WriteLine(Height);
                Console.WriteLine(FileSize);
                Console.WriteLine(DateTaken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading metadata: {ex.Message}");
            }
        }
    }
}
