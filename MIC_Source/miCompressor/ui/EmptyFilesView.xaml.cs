using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using miCompressor.core;
using Windows.ApplicationModel;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EmptyFilesView : UserControl
    {
        public string AppVersion => GetAppVersion();
        /// <summary>
        /// Supported extensions to show in UI.
        /// </summary>
        public string SupportedExtensionsInCaps => string.Join(", ", CodeConsts.SupportedInputExtensions).ToUpperInvariant();

        /// <summary>
        /// Checks if latest version is available. Returns false for Store users as that update path is via Microsoft Store. 
        /// </summary>
        /// <returns></returns>
        [AutoNotify] public bool updateAvailable = false;


        public EmptyFilesView()
        {
            this.InitializeComponent();

            _ = UIThreadHelper.RunOnUIThreadAsync(async () =>
            {
                UpdateAvailable = await IsUpdateAvailable();
            });
        }

        public string GetAppVersion()
        {
            try
            {
                if (Package.Current != null)
                {
                    PackageVersion version = Package.Current.Id.Version;
                    return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                }
            }
            catch
            {

            }
            return "4.0.2"; // Default for unpackaged apps
        }

        private async Task<bool> IsUpdateAvailable()
        {
            if (App.CurrentState.IsOnMicrosoftStore)
                return false;

            return await VersionChecker.CheckIfNewVersionAvailableAsync(GetAppVersion());
        }
    }
}
