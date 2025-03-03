using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Input.Inking;
using Windows.UI.StartScreen;

namespace miCompressor.core
{
    /// <summary>
    /// Represents an image file, storing metadata and relative path for compression.
    /// Uses async loading to prevent UI blocking and supports automatic UI updates.
    /// </summary>
    public partial class MediaFileInfo
    {
        /// <summary>
        /// The root directory selected by the user.
        /// </summary>
        public string SelectedRootPath { get; }

        /// <summary>
        /// The full path to the image file.
        /// </summary>
        public FileInfo FileToCompress { get; }

        /// <summary>
        /// The full path to the image file.
        /// </summary>
        public string FilePath => FileToCompress.FullName;

        /// <summary>
        /// Used only for logging purpose.
        /// </summary>
        public string ShortName;


        /// <summary>
        /// The relative path of the image within the selected directory.
        /// </summary>
        public string RelativePath => Path.GetRelativePath(SelectedRootPath, FileToCompress.FullName);

        /// <summary>
        /// The relative path of directory containing the image within the selected directory.
        /// </summary>
        public string RelativeImageDirPath => Path.GetDirectoryName(RelativePath) ?? string.Empty;

        private uint width;

        /// <summary>
        /// The width of the input image in pixels.
        /// </summary>
        public uint Width
        {
            get => width;
            private set
            {
                width = value;
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(DimensionsToShow));
            }
        }


        private uint height;
        /// <summary>
        /// The height of the input image in pixels.
        /// </summary>
        public uint Height
        {
            get => height;
            private set
            {
                height = value;
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(DimensionsToShow));
            }
        }

        private ulong _fileSize;
        /// <summary>
        /// The size of the input image file in bytes.
        /// </summary>
        public ulong FileSize
        {
            get => _fileSize;
            private set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
                OnPropertyChanged(nameof(FileSizeToShow));
            }
        }

        public string FileSizeToShow => HumanReadable.FileSize(fileSize: FileSize);

        private ulong _compressedFileSize;
        /// <summary>
        /// The size of the input image file in bytes.
        /// </summary>
        public ulong CompressedFileSize
        {
            get => _compressedFileSize;
            private set
            {
                _compressedFileSize = value;
                OnPropertyChanged(nameof(CompressedFileSize));
                OnPropertyChanged(nameof(CompressedFileSizeToShow));
            }
        }

        public string CompressedFileSizeToShow => HumanReadable.FileSize(fileSize: CompressedFileSize);

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

        public bool IsMetadataLoaded => Width != 0 && Height != 0 && FileSize != 0;

        /// <summary>
        /// Determines whether this operation is replacing the original file.
        /// Used when OutputLocationSetting is set to ReplaceOriginal.
        /// </summary>
        public bool IsReplaceOperation { get; private set; } = false;

        /// <summary>
        /// returns excludeAndHide AND excludeAndShow
        /// </summary>
        public bool ShouldProcess
        {
            get { return excludeAndHide && excludeAndShow; }
        }

        public string DimensionsToShow
        {
            get
            {
                return $"{width}x{height}";
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MediaFileInfo"/>.
        /// </summary>
        /// <param name="selectedPath">The root directory selected by the user.</param>
        /// <param name="mediaFile">FileInfo of the image/video</param>
        /// <exception cref="FileNotFoundException">Throws exception if file doesn't exist.</exception>
        public MediaFileInfo(string selectedPath, FileInfo mediaFile)
        {
            if (!mediaFile.Exists)
                throw new FileNotFoundException("File not found", mediaFile.FullName);

            SelectedRootPath = selectedPath;
            FileToCompress = mediaFile;
            ShortName = mediaFile.Name;
            FileSize = (ulong)mediaFile.Length;

            // Load metadata asynchronously
            //UIThreadHelper.RunOnUIThreadAsync(async () => LoadImageMetadataAsync());
            _ = LoadImageMetadataAsync();
        }

        /// <summary>
        /// Loads image metadata asynchronously without blocking the UI.
        /// </summary>
        private async Task LoadImageMetadataAsync(bool force = false)
        {
            if (!force && IsMetadataLoaded)
                return;

            ImageMetadata? outputMeta = await LoadImageMetadataAsync(FileToCompress.FullName, loadFileSize: false);
            Width = outputMeta?.Width ?? 0;
            Height = outputMeta?.Height ?? 0;

            OnPropertyChanged(nameof(DimensionsToShow));
            //CameraModel = outputMeta?.CameraModel;
            //DateTaken = outputMeta?.DateTaken;
        }

        /// <summary>
        /// Loads image metadata asynchronously without blocking the UI.
        /// If no file path is provided, it loads metadata for the original file.
        /// </summary>
        /// <param name="filePath">The full path of the file to load metadata from.</param>
        /// <param name="loadFileSize">True if file size is required. This is to save time</param>
        /// <returns>Returns an ImageMetadata object containing the extracted details.</returns>
        private async Task<ImageMetadata?> LoadImageMetadataAsync(string filePath, bool loadFileSize = true)
        {
            Debug.WriteLineIf(debugThisClass, $"Loading file metadata {Path.GetFileName(filePath)}");
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                ImageProperties properties = await file.Properties.GetImagePropertiesAsync();

                return new ImageMetadata
                {
                    Width = properties.Width,
                    Height = properties.Height,
                    FileSize = loadFileSize ? (ulong)new FileInfo(filePath).Length : 0,
                    //CameraModel = properties.CameraModel,
                    //DateTaken = properties.DateTaken
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading metadata for {filePath}: {ex.Message}");
                return null; // Return null if metadata loading fails
            }
        }


        /// <summary>
        /// Generates the output path based on the specified output settings.
        /// Determines the appropriate file path based on multiple settings such as
        /// location strategy, file naming modifications, and preview mode.
        /// There is no guarantee that the actual file creation path will be same as this method returns temporary path for replace operations. 
        /// Throws <see cref="InvalidOperationException"/> if required settings are missing.
        /// Throws <see cref="NotSupportedException"/> if an unsupported OutputLocationSetting is encountered.
        /// </summary>
        /// <param name="outputSettings">The output settings.</param>
        /// <param name="onlyPreview">If true, the output will be stored in a temporary directory.</param>
        /// <returns>The full path of the output file. Doesn't actually create file or directory.</returns>
        /// <remarks>
        /// The following variables in <paramref name="outputSettings"/> must be properly set before calling this method:
        /// <list type="bullet">
        /// <item><description><c>outputFolder</c> (required for UserSpecificFolder strategy)</description></item>
        /// <item><description><c>prefix</c></description></item>
        /// <item><description><c>suffix</c></description></item>
        /// <item><description><c>replaceFrom</c> (optional but affects filename transformation)</description></item>
        /// <item><description><c>replaceTo</c> (optional, used with replaceFrom)</description></item>
        /// </list>
        /// <para>This method may throw an exception if required output settings are not provided.</para>
        /// </remarks>
        public string GetOutputPath(OutputSettings outputSettings, bool multipleFolderSelected, bool onlyPreview)
        {
            string outputDirectory;
            string originalFileName = Path.GetFileNameWithoutExtension(FileToCompress.Name);
            string fileExtension = outputSettings.format.GetOutputExtension(FileToCompress.Name);

            // Apply ReplaceFrom/ReplaceTo if specified
            if (!string.IsNullOrEmpty(outputSettings.replaceFrom))
            {
                originalFileName = originalFileName.Replace(outputSettings.replaceFrom, outputSettings.replaceTo);
            }

            // Apply prefix and suffix
            string modifiedFileName = $"{outputSettings.prefix}{originalFileName}{outputSettings.suffix}{fileExtension}";

            if (onlyPreview || outputSettings.outputLocationSettings == OutputLocationSetting.ReplaceOriginal)
            {
                // Store in temp directory
                outputDirectory = TempDataManager.GetTempStorageDirPath(RelativeImageDirPath);
                IsReplaceOperation = outputSettings.outputLocationSettings == OutputLocationSetting.ReplaceOriginal;
            }
            else
            {
                switch (outputSettings.outputLocationSettings)
                {
                    case OutputLocationSetting.InCompressedFolder:
                        outputDirectory = FileToCompress.Directory!.FullName;
                        if (Directory.Exists(SelectedRootPath))
                        {
                            outputDirectory = Path.Combine(SelectedRootPath, CodeConsts.compressedDirName, RelativeImageDirPath);
                        }
                        else
                        {
                            outputDirectory = Path.Combine(FileToCompress.Directory!.FullName, CodeConsts.compressedDirName);
                        }
                        break;

                    case OutputLocationSetting.SameFolderWithFileNameSuffix:
                        outputDirectory = FileToCompress.Directory!.FullName;
                        break;

                    case OutputLocationSetting.UserSpecificFolder:
                        if (!string.IsNullOrWhiteSpace(outputSettings.outputFolder))
                        {
                            if (multipleFolderSelected && Directory.Exists(SelectedRootPath))
                            {
                                outputDirectory = Path.Combine(outputSettings.outputFolder, Path.GetFileName(SelectedRootPath), RelativeImageDirPath);
                            }
                            else
                                outputDirectory = Path.Combine(outputSettings.outputFolder, RelativeImageDirPath);
                        }
                        else
                        {
                            throw new InvalidOperationException("User specified output folder is not set.");
                        }
                        break;

                    default:
                        throw new NotSupportedException("Unsupported OutputLocationSetting");
                }
            }

            var outputPath = Path.Combine(outputDirectory, modifiedFileName);

            // If the output path is the same as the original file, store in temp directory. This can happen when user selects the same folder for output but not ReplaceOriginal option.
            if (FileToCompress.FullName.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
            {
                outputDirectory = TempDataManager.GetTempStorageDirPath(RelativeImageDirPath);
                IsReplaceOperation = true;
            }

            return Path.Combine(outputDirectory, modifiedFileName);
        }

        /// <summary>
        /// Finalizes the output operation by verifying file size before replacing or saving.
        /// Ensures that the original file is not replaced or duplicated with a larger one.
        /// If compression fails, it copies the original file when appropriate.
        /// </summary>
        /// <param name="outputPath">The path where the compressed file was saved.</param>
        /// <param name="outputSettings">The output settings to get height and width of output image.</param>
        /// <returns>
        /// A tuple with:
        /// 1. wasOriginalFileUsed (True if we discarded the compressed file and kept the original).
        /// 2. failedToFreezeOutput (True if the compressed file was corrupt or unreadable).
        /// </returns>
        public async Task<(bool wasOriginalFileUsed, bool failedToFreezeOutput)> FreezeOutputAsync(
            string outputPath, OutputSettings outputSettings)
        {
            (uint expectedWidth, uint expectedHeight) = DimensionHelper.GetOutputDimensions(outputSettings, width, height);

            FileInfo outputFile = new FileInfo(outputPath);
            outputFile.Refresh();
            CompressedFileSize = (ulong) outputFile.Length;

            if (!outputFile.Exists)
            {
                // Compression failed, decide whether to copy original
                if (ShouldCopyOriginal(expectedWidth, expectedHeight, outputPath))
                {
                    File.Copy(FileToCompress.FullName, outputPath, overwrite: true);
                    CompressedFileSize = FileSize;
                    return (true, false); // Original file used, output file missing but not corrupt
                }
                return (false, true); // Output file missing/corrupt and can't be replaced
            }

            // Load metadata for the compressed file
            ImageMetadata? outputMeta = await LoadImageMetadataAsync(outputPath, loadFileSize: false);
            if(outputMeta != null)
                outputMeta.FileSize = (uint)outputFile.Length;

            if (outputMeta == null)
            {
                // The output file might be corrupt
                if (ShouldCopyOriginal(expectedWidth, expectedHeight, outputPath))
                {
                    File.Copy(FileToCompress.FullName, outputPath, overwrite: true);
                    CompressedFileSize = FileSize;
                    return (true, false); // Used original file, output file unreadable but not fatal
                }

                if (IsReplaceOperation)
                {
                    outputFile.Delete();
                    return (true, true); // Used original file, and failed to freeze output
                }

                return (false, true); // Output file exists but is unreadable, freezing failed
            }

            if (IsLargerFileWithoutChange(outputMeta, outputPath))
            {
                if (IsReplaceOperation)
                {
                    outputFile.Delete();
                    return (true, false); // Original file used, output file discarded
                }

                File.Copy(FileToCompress.FullName, outputPath, overwrite: true);
                CompressedFileSize = FileSize;
                return (true, false); // Original file used instead of larger compressed file
            }

            // If it's a replace operation, move the compressed file over the original
            if (IsReplaceOperation)
            {
                File.Move(outputFile.FullName, FileToCompress.FullName, overwrite: true);
            }

            return (false, false); // Compressed file used successfully
        }

        /// <summary>
        /// Determines if the original file should be copied instead of the output file.
        /// </summary>
        /// <param name="expectedWidth">Expected width of the output image.</param>
        /// <param name="expectedHeight">Expected height of the output image.</param>
        /// <param name="outputPath">Path of the compressed file.</param>
        /// <returns>True if the original file should be copied instead.</returns>
        private bool ShouldCopyOriginal(uint expectedWidth, uint expectedHeight, string outputPath)
        {
            return HasSameExtension(outputPath) && HasSameDimensions(expectedWidth, expectedHeight);
        }

        /// <summary>
        /// Checks if the given output file is larger while having the same dimensions and extension.
        /// </summary>
        /// <param name="outputMeta">Metadata of the compressed file.</param>
        /// <param name="outputFilePath">Path of the compressed file.</param>
        /// <returns>True if the output file is larger without any useful change.</returns>
        private bool IsLargerFileWithoutChange(ImageMetadata outputMeta, string outputFilePath)
        {
            return outputMeta.FileSize >= FileSize && HasSameDimensions(outputMeta.Width, outputMeta.Height) && HasSameExtension(outputFilePath);
        }

        /// <summary>
        /// Checks if the given file has the same extension as the original file.
        /// </summary>
        /// <param name="filePath">Path of the file to compare.</param>
        /// <returns>True if the file extension matches the original.</returns>
        private bool HasSameExtension(string filePath)
        {
            return string.Equals(Path.GetExtension(FileToCompress.FullName), Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the given dimensions match the original file dimensions.
        /// </summary>
        /// <param name="width">Width of the file to compare.</param>
        /// <param name="height">Height of the file to compare.</param>
        /// <returns>True if the dimensions match the original.</returns>
        private bool HasSameDimensions(uint width, uint height)
        {
            return Width == width && Height == height;
        }



        /// <summary>
        /// Represents metadata extracted from an image file.
        /// </summary>
        private class ImageMetadata
        {
            public uint Width { get; set; }
            public uint Height { get; set; }
            public ulong FileSize { get; set; }
            public string? CameraModel { get; set; }
            public DateTimeOffset? DateTaken { get; set; }
        }

    }
}
