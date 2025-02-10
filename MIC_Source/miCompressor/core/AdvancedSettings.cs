using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    /// <summary>
    /// Provides advanced settings for the application which user can change.
    /// </summary>
    public partial class AdvancedSettings
    {
        public static AdvancedSettings Instance { get; } = new AdvancedSettings();

        private AdvancedSettings()
        {

        }

        /// <summary>
        /// If the input image has unsupported output format, this format will be used when user wants to keep the original format.
        /// </summary>
        [AutoNotify] public OutputFormat defaultImageExtension = OutputFormat.Jpg;
    }
}
