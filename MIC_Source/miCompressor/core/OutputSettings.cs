﻿using miCompressor.core.common;
using System;
using System.Text.Json;
using Windows.Storage;

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


        #region Output file creation setting
        /// <summary>
        /// Output Location strategy
        /// </summary>
        [AutoNotify]
        public OutputLocationSetting outputLocationSettings = OutputLocationSetting.InCompressedFolder;

        /// <summary>
        /// Path of user specified output folder.
        /// </summary>
        [AutoNotify]
        public string outputFolder = "";

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
                copyMetadata, skipSensitiveMetadata, copyIPTC, copyXMP               
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
            CopyIPTC = other.copyIPTC;
            CopyXMP = other.copyXMP;
            RetainDateTime = other.retainDateTime;
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
