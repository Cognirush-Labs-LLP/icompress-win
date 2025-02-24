using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using miCompressor.core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// App-wide instance of Window, required for file/folder pickers.
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// To store files that are passed in argument (possibly by 'open with')
        /// </summary>
        public static List<string> OpenedFiles { get; private set; } = new List<string>();


        public static int TitleBarHeight { get; private set; } 
        /// <summary>
        /// App-wide instance of FileStore.
        /// </summary>
        public static FileStore FileStoreInstance { get; } = new();

        /// <summary>
        /// App-wide instance of OutputSettings.
        /// </summary>
        public static OutputSettings OutputSettingsInstance { get; } = new();


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            OutputSettingsInstance.restoreFromLastSaved();
            HandleCommandLineArgs();

#if DEBUG
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                FileStoreInstance.AddAsync(@"F:\OpenSource\MassImageCompressor_4\MIC_Source\miCompressor.IntegrationTests\test_imgs\");
                //FileStoreInstance.AddAsync(@"C:\Users\yogee\Pictures\Camera Roll");
            });
#endif
        }

        private void HandleCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            // Skip the first argument (executable path)
            if (args.Length > 1)
            {
                OpenedFiles = args.Skip(1).ToList();
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Create UI helper
            core.UIThreadHelper.Initialize();

            MainWindow = new MainWindow();           
            MainWindow.Activate();

            OpenedFiles.ForEach(file =>  FileStoreInstance.AddAsync(file));
        }
    }
}
