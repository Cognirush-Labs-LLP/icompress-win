using System;
using Microsoft.UI.Dispatching;

namespace miCompressor.core
{

    /// <summary>
    /// Provides a globally accessible reference to the UI thread's <see cref="DispatcherQueue"/>.
    /// This allows ViewModels and background tasks to dispatch UI updates safely.
    /// </summary>
    public static class UIThreadHelper
    {
        /// <summary>
        /// Stores the UI thread's DispatcherQueue for cross-thread UI updates.
        /// This must be initialized on the UI thread.
        /// </summary>
        public static DispatcherQueue? UIThreadDispatcherQueue { get; private set; }

        /// <summary>
        /// Initializes the UI dispatcher queue. 
        /// This must be called **only from the UI thread**, preferably in App.xaml.cs or MainWindow.xaml.cs.
        /// </summary>
        public static void Initialize()
        {
            // Capture the UI thread's DispatcherQueue for later use.
            UIThreadDispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Ensure it's not called from a background thread (optional safety check).
            if (UIThreadDispatcherQueue == null)
            {
                throw new InvalidOperationException("UIThreadHelper.Initialize() must be called from the UI thread.");
            }
        }
    }
}
