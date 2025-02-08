using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        public int quality = 70;

        /// <summary>
        /// Output Image Format. Defaults to JPEG if the output image format is not supported.
        /// </summary>
        [AutoNotify]
        public OutputFormat format = OutputFormat.Jpg;

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
        public decimal percentageOfLongEdge = 100;

        /// <summary>
        /// When reducing image by specifying long edge, height or width, this server as its value.
        /// </summary>
        [AutoNotify] public int primaryEdgeLength = 1920;

        /// <summary>
        /// Print dimension for printing. This is used when dimension strategy is set to FitInFrame or FixedInFrame.
        /// This value is only considered for display purpose and converted and setting primaryEdgeLength.
        /// </summary>
        public PrintDimension PrintDimension { get; set; } = new(8, 6, 0.15m); // Defaulted to Desk Photo 8x6 with 0.15 inch margin
        #endregion

        /// <summary>
        /// Copy EXIF data to output image or not.
        /// </summary>
        [AutoNotify]
        public bool copyMetadata = true;

        #region Output file creation setting
        /// <summary>
        /// Output Location stragegy 
        /// </summary>
        [AutoNotify]
        public OutputLocationSettings outputLocationSettings = OutputLocationSettings.UserSpecificFolder;

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


        #region Serialize and Store/Restore For Future sessions
        /// <summary>
        /// Key to store settings in local settings.
        /// </summary>
        private const string SettingsKey = "SavedOutputSettings";

        /// <summary>
        /// Save the current instance to local settings.
        /// </summary>
        public void saveForFuture()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                ApplicationData.Current.LocalSettings.Values[SettingsKey] = json;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore the last saved instance from local settings.
        /// </summary>
        public void restoreFromLastSaved()
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(SettingsKey, out object? jsonObj) && jsonObj is string json)
                {
                    OutputSettings? restoredSettings = JsonSerializer.Deserialize<OutputSettings>(json);
                    if (restoredSettings != null)
                    {
                        CopyFrom(restoredSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies properties from another instance.
        /// We do not copy outputFolder, suffix, prefix, replaceFrom and replaceTo as that may cause unexpected saving of files in wrong location.
        /// </summary>
        private void CopyFrom(OutputSettings other)
        {
            Quality = other.quality;
            
            Format = other.format;
            
            DimensionStrategy = other.dimensionStrategy;
            PercentageOfLongEdge = other.percentageOfLongEdge;
            PrimaryEdgeLength = other.primaryEdgeLength;
            PrintDimension = other.PrintDimension;

            CopyMetadata = other.copyMetadata;
            //OutputLocationSettings = other.outputLocationSettings;
            //OutputFolder = other.outputFolder;
            //Suffix = other.suffix;
            //Prefix = other.prefix;
            //ReplaceFrom = other.replaceFrom;
            //ReplaceTo = other.replaceTo;
        }
        #endregion
    }
}
