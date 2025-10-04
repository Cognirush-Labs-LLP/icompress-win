using miCompressor.core.common;
using System;

namespace miCompressor.core
{
    /// <summary>
    /// Stores output settings including compression information and others. 
    /// </summary>
    public partial class OutputSettings
    {
        /// <summary>
        /// Output Quality between 0 to 100.
        /// </summary>
        [AutoNotify]
        public uint quality = 80;

        /// <summary>
        /// If enabled, PNG color table is quantized and dithering is enabled. 
        /// </summary>
        [AutoNotify]
        public bool allowLossyPNG = true;

        /// <summary>
        /// Output Image Format. Defaults to JPEG if the output image format is not supported.
        /// </summary>
        [AutoNotify]
        public OutputFormat format = OutputFormat.KeepSame;

        /// <summary>
        /// If true, existing files in the output directory will not be overwritten. 
        /// Useful for incremental compression where previously compressed images 
        /// should be preserved.
        /// </summary>
        [AutoNotify]
        public bool skipIfFileExists = false;

        #region Dimension
        /// <summary>
        /// Dimension reduction strategy for reducing image size.
        /// </summary>
        [AutoNotify]
        public DimensionReductionStrategy dimensionStrategy = DimensionReductionStrategy.KeepSame;

        /// <summary>
        /// Percentage of original image width (or heigh, doesn't matter). If this is set as 50, then image height and width both are reduced by 50%, hence making image four time smaller than original when total number of pixels are considered.
        /// </summary>
        [AutoNotify]
        public double percentageOfLongEdge = 100;

        /// <summary>
        /// When reducing image by specifying long edge, height or width, this server as its value.
        /// </summary>
        [AutoNotify] public uint primaryEdgeLength = 1920;

        /// <summary>
        /// Print dimension for printing. This is used when dimension strategy is set to FitInFrame or FixedInFrame.
        /// This value is only considered for display purpose and converted and setting primaryEdgeLength.
        /// </summary>
        public PrintDimension PrintDimension { get; set; } = new(8, 6, 0.15); // Defaulted to Desk Photo 8x6 with 0.15 inch margin
        #endregion

        /// <summary>
        /// Copy EXIF data to output image or not.
        /// </summary>
        [AutoNotify]
        public bool copyMetadata = true;

        [AutoNotify]
        public bool skipSensitiveMetadata = false;
        [AutoNotify]
        public bool copyIPTC = true;
        [AutoNotify]
        public bool copyXMP = true;
        [AutoNotify]
        public bool retainDateTime = true;

        #region Watermark Settings
        [AutoNotify]
        public bool applyWatermark = false;

        /// <summary>
        /// Just increment when watermark settings change (helps to update image output in live compression preview
        /// </summary>
        [AutoNotify]
        public int watermarkSettingsHash = 0;

        #endregion

        #region Output file creation setting
        /// <summary>
        /// Output Location strategy
        /// </summary>
        private OutputLocationSetting outputLocationSettings = OutputLocationSetting.InCompressedFolder;

        /// <summary>
        /// Path of user specified output folder.
        /// </summary>
        private string outputFolder = "";

        public OutputLocationSetting OutputLocationSettings
        {
            get => outputLocationSettings;
            set
            {
                if (outputLocationSettings != value)
                {
                    if (!IsAutoSettingOutputPath)
                        IsOutputPathAutoSet = false; //reset IsOutputPathAutoSet to false when user sets the output location settings

                    outputLocationSettings = value;
                    OnPropertyChanged(nameof(OutputLocationSettings));
                }
            }
        }

        public string OutputFolder
        {
            get => outputFolder;
            set
            {
                if (outputFolder != value)
                {
                    if (!IsAutoSettingOutputPath)
                        IsOutputPathAutoSet = false; //reset IsOutputPathAutoSet to false when user sets output path

                    outputFolder = value;
                    OnPropertyChanged(nameof(OutputFolder));
                }
            }
        }

        /// <summary>
        /// File name suffix of output (i.e. compressed) file.
        /// </summary>
        [AutoNotify]
        public string suffix = "";

        /// <summary>
        /// File name prefix of output (i.e. compressed) file.
        /// </summary>
        [AutoNotify]
        public string prefix = "";

        /// <summary>
        /// This specified text from original file name will be replaced to what's specified in <![CDATA[ReplaceTo]]> in output file name.
        /// </summary>
        [AutoNotify]
        public string replaceFrom = "";

        /// <summary>
        /// Text specified in <![CDATA[ReplaceFrom]]>, if found in name of original file, will be replaced with this text in output file name.
        /// </summary>
        [AutoNotify]
        public string replaceTo = "";
        #endregion


        public OutputSettings()
        {
            this.PrintDimension.PropertyChanged += PrintDimension_PropertyChanged;
        }

        private void PrintDimension_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(PrintDimension));
        }

        #region System Set /compressed path

        private object autosetlock = new();
        /// <summary>
        /// This is set to true if the output path is set to 'compressed' directory of selected file/folder.
        /// Auto set of the output directory happens for backward compatibility of expected user behaviour if only one folder/file is selected for compression. As users have been accustom to change this path, we use to provide path as,
        /// Input: C:\abc -> Save to: C:\abc\compressed
        /// Input: C:\abc.jpg -> Save to: C:\abc\compressed
        /// This variable helps to reset Auto Set output path to OutputLocationSetting.InCompressedFolder if multiple files/directory are in selected items.
        /// </summary>
        private bool IsOutputPathAutoSet = true;

        /// <summary>
        /// Stores auto set path to identify if path was auto set
        /// </summary>
        private bool IsAutoSettingOutputPath = false;

        /// <summary>
        /// Auto set output directory to given path. This method do not do anything if the output set was set by user previously.
        /// </summary>
        /// <param name="outputPathToSet">A valid output directory path. This is most probably 'compressed' folder, may or may not exist.</param>
        public void DoAutoSetOutDir(string outputPathToSet)
        {
            if (outputPathToSet == null) return;

            lock (autosetlock)
            {
                if (IsOutputPathAutoSet == false 
                    && !(this.OutputLocationSettings == OutputLocationSetting.UserSpecificFolder && string.IsNullOrWhiteSpace(this.OutputFolder)))
                    return; // no change if current output setting is not via Auto Set or directory 

                IsAutoSettingOutputPath = true;
                IsOutputPathAutoSet = true;
                this.OutputLocationSettings = OutputLocationSetting.UserSpecificFolder;
                this.OutputFolder = outputPathToSet;
                IsAutoSettingOutputPath = false;
            }
        }

        /// <summary>
        /// Auto Set output location setting to OutputLocationSetting.InCompressedFolder when multiple files/folders are selected.
        /// </summary>
        public void DoAutoSetCompressedDir()
        {
            lock (autosetlock)
            {
                if (IsOutputPathAutoSet == false)
                    return; // no change if current output setting is not via Auto Set

                IsOutputPathAutoSet = true;
                this.OutputLocationSettings = OutputLocationSetting.InCompressedFolder;
            }
        }
        #endregion


        #region Serialize and Store/Restore For Future sessions
        /// <summary>
        /// Key to store settings in local settings.
        /// </summary>
        private const string SettingsKey = "SavedOutputSettings";


        public string GetHashForImagePreviewRegeneration()
        {
            return HashHelper.ComputeStableHash(
                quality,
                allowLossyPNG,
                format,
                dimensionStrategy, percentageOfLongEdge, primaryEdgeLength,
                (PrintDimension != null ? PrintDimension.LongEdgeInInch : 1),
                (PrintDimension != null ? PrintDimension.ShortEdgeInInch : 1),
                (PrintDimension != null ? PrintDimension.Margin : 1),
                copyMetadata, skipSensitiveMetadata, copyIPTC, copyXMP, applyWatermark , watermarkSettingsHash
                );
        }

        /// <summary>
        /// Save the current instance to local settings.
        /// </summary>
        public void SaveForFuture()
        {
            try
            {
                AppSettingsManager.Set(SettingsKey, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore the last saved instance from local settings.
        /// </summary>
        public void RestoreFromLastSaved()
        {
            try
            {
                OutputSettings restoredSettings;
                if (AppSettingsManager.TryGet(SettingsKey, out restoredSettings))
                {
                    if (restoredSettings != null)
                    {
                        CopyFrom(restoredSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies properties from another instance.
        /// We do not copy outputFolder, suffix, prefix, replaceFrom and replaceTo as that may cause unexpected saving of files in wrong location.
        /// </summary>
        private void CopyFrom(OutputSettings other)
        {
            Quality = other.quality;
            AllowLossyPNG = other.allowLossyPNG;
            Format = other.format;
            
            DimensionStrategy = other.dimensionStrategy;
            PercentageOfLongEdge = other.percentageOfLongEdge;
            PrimaryEdgeLength = other.primaryEdgeLength;
            PrintDimension = other.PrintDimension;

            CopyMetadata = other.copyMetadata;
            SkipSensitiveMetadata = other.skipSensitiveMetadata;
            SkipIfFileExists = other.skipIfFileExists;
            CopyIPTC = other.copyIPTC;
            CopyXMP = other.copyXMP;
            RetainDateTime = other.retainDateTime;
            ApplyWatermark = other.applyWatermark;
            //OutputLocationSetting = other.outputLocationSettings;
            //OutputFolder = other.outputFolder;
            //Suffix = other.suffix;
            //Prefix = other.prefix;
            //ReplaceFrom = other.replaceFrom;
            //ReplaceTo = other.replaceTo;
        }
        #endregion
    }
}
