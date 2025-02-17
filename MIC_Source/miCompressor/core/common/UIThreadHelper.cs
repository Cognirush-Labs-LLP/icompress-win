using System;
using System.Threading.Tasks;
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

        /// <summary>
        /// Runs an action on the UI thread. If already on the UI thread, the action runs immediately.
        /// </summary>
        /// <example>
        /// <code>
        /// dispatcher.TryEnqueue(() =>
        /// {
        ///     // Execute UI-related code here.
        ///     // Example: Updating UI elements from a background thread.
        ///     myTextBox.Text = "Updated from UI thread.";
        /// });
        /// </code>
        /// </example>
        public static void RunOnUIThread(Action action)
        {
            if (UIThreadDispatcherQueue is null)
#if DEBUG
                //throw new InvalidOperationException("UIThreadDispatcherQueue is not initialized. Call Initialize() on the UI thread first.");
                return;
#else
        return; // Fail silently in release builds.
#endif

            if (UIThreadDispatcherQueue.HasThreadAccess)
                action();
            else
                UIThreadDispatcherQueue.TryEnqueue(() => action());
        }

        /// <summary>
        /// Runs the provided async action on the UI thread. If already on the UI thread, it executes immediately.
        /// </summary>
        public static async Task RunOnUIThreadAsync(Func<Task> asyncAction)
        {
            if (UIThreadDispatcherQueue?.HasThreadAccess == true)
            {
                // Already on UI thread, execute immediately
                await asyncAction();
            }
            else
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();

                UIThreadDispatcherQueue!.TryEnqueue(async () =>
                {
                    try
                    {
                        await asyncAction();  // This runs on the UI thread
                        taskCompletionSource.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);  // Propagate exceptions
                    }
                });

                await taskCompletionSource.Task;
            }
        }
    }
}
