using miCompressor.core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace miCompressor.ui.viewmodel
{
    public partial class CompressionViewModel
    {
        private FileStore store;
        private OutputSettings settings;
        private ImageCompressor compressor = new();

        private readonly Stopwatch _stopwatch;
        private readonly Timer _timer;
        
        [AutoNotify] private string timeToShow = "";

        [AutoNotify] private int totalFilesToCompress = 0;
        [AutoNotify] private int totalFilesCompressed = 0;
        [AutoNotify] private int totalFilesFailedToCompress = 0;
        [AutoNotify] private int totalFilesCancelled = 0;
        [AutoNotify] private bool compressionInProgress = false;

        [AutoNotify] private ulong totalOriginalSize = 0;
        [AutoNotify] private ulong totalCompressedSize = 0;

        public string OriginalSize => HumanReadable.FileSize(totalOriginalSize);
        public string CompressedSize => HumanReadable.FileSize(totalCompressedSize);

        public string PercentageReduction
        {
            get
            {
                if (totalOriginalSize == 0)
                    return " -- ";
                var reducedPercentage = 100.0 - (100.0 * (double)totalCompressedSize / (double)totalOriginalSize);

                string upOrdown;
                if(reducedPercentage == 0)
                    upOrdown = "↓ 😐";
                else if (reducedPercentage > 70)
                    upOrdown = "↓ 🤗";
                else if (reducedPercentage > 30)
                    upOrdown = "↓ 😏";
                else if (reducedPercentage > 0)
                   upOrdown = "↓ 🙂"; 
                else
                    upOrdown = "↑ 😢";
                return reducedPercentage.ToString("0.##") + $"% {upOrdown}";
            }
        }

        private object compressionInProgressLock = new();

        public CompressionViewModel(FileStore fileStoreInstance, OutputSettings settings)
        {
            this.store = fileStoreInstance;
            this.settings = settings;
            _stopwatch = new Stopwatch();
            _timer = new Timer(300); // Updates every second

            compressor.ImageCompressed += Compressor_ImageCompressed;
            compressor.CompressionCompleted += Compressor_CompressionCompleted;
            _timer.Elapsed += (s, e) =>  { TimeToShow = GetElapsedTimeFormatted(); };

        }

        private void Reset()
        {
            TotalFilesToCompress = 0;
            TotalFilesCompressed = 0;
            TotalFilesFailedToCompress = 0;
            TotalFilesCancelled = 0;
            TotalOriginalSize = 0;
            TotalCompressedSize = 0;
            TimeToShow = "";
            OnPropertyChanged(nameof(PercentageReduction));
            OnPropertyChanged(nameof(OriginalSize));
            OnPropertyChanged(nameof(CompressedSize));
        }

        public (bool good, string error) CheckSettingsCondition()
        {
            if (settings.outputLocationSettings == OutputLocationSetting.UserSpecificFolder && string.IsNullOrWhiteSpace(settings.outputFolder))
                return (false, "Output Folder Not Specified.");

            if (settings.outputLocationSettings == OutputLocationSetting.SameFolderWithFileNameSuffix && string.IsNullOrWhiteSpace(settings.prefix) && string.IsNullOrWhiteSpace(settings.suffix))
                return (false, "You must provide Prefix or Suffix for current output setting: " + OutputLocationSetting.SameFolderWithFileNameSuffix.GetDescription());

            return (true, "");
        }

        /// <summary>
        /// Starts compression of all selected files.
        /// </summary>
        /// <returns>flags if compression couldn't start.</returns>
        public (bool alreadyInProgress, bool nothingToCompress) StartCompression()
        {
            Reset();

            var imagesToCompress = store.GetAllFiles.Where(file => !file.ExcludeAndHide && !file.ExcludeAndShow).ToList();

            if (!imagesToCompress.Any())
                return (false, true);

            lock (compressionInProgressLock)
            {
                if (compressionInProgress)
                    return (true, false);
                CompressionInProgress = true;
                StartTimer();
            }

            TotalFilesToCompress = imagesToCompress.Count();

            _ = compressor.CompressImagesAsync(imagesToCompress, store.SelectedPaths.Count > 1, settings, false);

            return (false, false);
        }

        /// <summary>
        /// Initiates cancellation of the compression. 
        /// </summary>
        public void CancelCompression()
        {
            compressor.CancelCompression();
        }

        private void Compressor_CompressionCompleted(object? sender, CompressionCompletedEventArgs e)
        {
            CompressionInProgress = false;
            StopTimer();
        }

        private void Compressor_ImageCompressed(object? sender, ImageCompressedEventArgs e)
        {
            if (e.Success)
            {
                TotalFilesCompressed++;
                if (e.FileInfo.CompressedFileSize > 0)
                {
                    totalOriginalSize += e.FileInfo.FileSize;
                    totalCompressedSize += e.FileInfo.CompressedFileSize;
                    OnPropertyChanged(nameof(OriginalSize));
                    OnPropertyChanged(nameof(CompressedSize));
                    OnPropertyChanged(nameof(PercentageReduction));
                }
            }
            else if (e.Error == CompressionErrorType.Cancelled)
                TotalFilesCancelled++;
            else
                TotalFilesFailedToCompress++;
        }

        #region Timer
        private void StartTimer()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
                _timer.Start();
            }
        }

        private void StopTimer()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
                _timer.Stop();
                _stopwatch.Reset();
                _timer.Stop();
            }
        }

        private string GetElapsedTimeFormatted()
        {
            TimeSpan ts = _stopwatch.Elapsed;
            return ts.TotalMinutes >= 1 ? $"{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Seconds} sec";
        }
        #endregion
    }
}
