using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using miCompressor.core;
using miCompressor.ui;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            PersistentSettings.RestoreWindowSize(this);

            this.Title = "Mass Image Compressor 4.0";
            this.AppWindow.SetIcon("Assets/mic_win_4.ico");

            this.Content = new MasterView();
         
            this.SizeChanged += (sender, args) => PersistentSettings.SaveWindowSize(this);
            this.Closed += MainWindow_Closed;

#if DEBUG
            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 700));
#endif
        }
                
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            PersistentSettings.CleanupSizeCapture();
            // Clean up throttle timers on window close.
            ThrottleTask.Clear();
#if DEBUG
            App.OutputSettingsInstance.saveForFuture();
#endif
        }
    }
}
