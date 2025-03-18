using miCompressor.core;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        public static FileStore FileStoreInstance => CurrentState.FileStore;

        /// <summary>
        /// App-wide instance of OutputSettings.
        /// </summary>
        public static OutputSettings OutputSettingsInstance => CurrentState.OutputSettings;

        /// <summary>
        /// Holds shared state information between views. 
        /// </summary>
        public static MasterState CurrentState = new();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            OutputSettingsInstance.restoreFromLastSaved();
            HandleCommandLineArgs();
            Task.Run(() => HousekeepingAndDefaults());

#if DEBUG
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                FileStoreInstance.Enqueue(@"F:\OpenSource\MassImageCompressor_4\MIC_Source\miCompressor.IntegrationTests\test_imgs\");
                FileStoreInstance.Enqueue(@"F:\\photo_work", true);
                //FileStoreInstance.Enqueue(@"C:\Users\yogee\Pictures\Camera Roll");
            });
#endif
        }

        /// <summary>
        /// This should run when application start to keep things organized.
        /// It also adds MIC shortcut to "Send To" folder. 
        /// </summary>
        private void HousekeepingAndDefaults()
        {
            try
            {
                TempDataManager.CleanUpTempDir();
                //Environment.SetEnvironmentVariable("OMP_NUM_THREADS", "2", EnvironmentVariableTarget.Process); //We set this in code again. This is just insurance if some version of Magick.NET doesn't respect this setting after initialization. 
                Environment.SetEnvironmentVariable("PATH", ProcessExecutor.ThirdPartyBasePath + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);

                ResourceLimits.LimitMemory(new Percentage(80)); //Limit Magick.NET to use 80% of available memory.
            }
            catch(Exception ex)
            {
                MicLog.Exception(ex);
            }
            try { SendToIntegration.AddToSendTo(); } catch{}
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

            OpenedFiles.ForEach(file => FileStoreInstance.Enqueue(file));
            OpenedFiles.Clear(); //not required, but let's clean it up. 
        }
    }
}
