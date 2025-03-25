using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Media.ClosedCaptioning;
using Windows.Storage;

namespace miCompressor.core
{
    internal class PersistentSettings
    {
        public const string PS_WindowWidth_Key = "WindowWidth";
        public const string PS_WindowHeight_Key = "WindowHeight";

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// Saves Window Size for future restoration. It takes 1 second to save the setting. Thread safe. 
        /// </summary>
        /// <param name="window"></param>
        public static void SaveWindowSize(Window window)
        {
            ThrottleTask.Add(1000, "PersistentSettings_SaveWindowSize", (() =>
            {
                try
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                    // Do not save size if the window is maximized
                    if (IsZoomed(hwnd))
                    {
                        Debug.WriteLine(" * Skipping window size save (window is maximized).");
                        return;
                    }

                    var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));

                    // Do not save size if the window is maximized
                    if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen ||
                        appWindow.Presenter.Kind == AppWindowPresenterKind.CompactOverlay)
                    {
                        Debug.WriteLine(" * Skipping window size save (window is maximized).");
                        return;
                    }

                    AppSettingsManager.Set(PS_WindowWidth_Key, window.Bounds.Width);
                    AppSettingsManager.Set(PS_WindowHeight_Key, window.Bounds.Height);
                }
                catch
                {
                    Debug.WriteLine(" * Error saving window pos.");
                    //ignore. It's not a big deal if we cannot save last window pos.
                }
            }), shouldRunInUI: true);
        }

        /// <summary>
        /// StopTimer any residual background tasks remaining for SaveWindowSize.
        /// Should be called when window is closing, not necessory. 
        /// </summary>
        public static void CleanupSizeCapture()
        {
            ThrottleTask.Remove("PersistentSettings_SaveWindowSize");
        }

        /// <summary>
        /// Restores Window size previously saved in Application Data.
        /// Must be called in UI Thread.
        /// </summary>
        /// <param name="window"></param>
        public static void RestoreWindowSize(Window window)
        {
            try
            {
                if (AppSettingsManager.TryGet(PS_WindowWidth_Key, out double width) &&
                    AppSettingsManager.TryGet(PS_WindowHeight_Key, out double height))
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));

                    // Get screen bounds
                    var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
                    int maxWidth = displayArea.WorkArea.Width;
                    int maxHeight = displayArea.WorkArea.Height;

                    // Clamp window size to screen size
                    int finalWidth = Math.Max(Math.Min((int)(double)width, maxWidth), 920); // smaller than screen, bigger than 800.
                    int finalHeight = Math.Max(Math.Min((int)(double)height, maxHeight), 700);

                    if (finalWidth > 0 && finalHeight > 0)
                    {
                        appWindow.Resize(new SizeInt32(finalWidth, finalHeight));
                        Debug.WriteLine($" * Restored window size to: {finalWidth}x{finalHeight}");
                    }
                }
            }
            catch 
            {
                Debug.WriteLine(" * Error restoring window pos.");
            }
        }
    }
}
