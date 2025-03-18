using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.UI.Dispatching; // For DispatcherQueuePriority, adjust if necessary

namespace miCompressor.core
{
    /// <summary>
    /// Provides a way to debounce tasks to run only after a specified idle period.
    /// </summary>
    public static class DebounceTask
    {
        /// <summary>
        /// Each debounce "bucket" is represented by a DebounceInvoker.
        /// </summary>
        private class DebounceInvoker : IDisposable
        {
            private Timer _timer;
            private readonly object _lock = new object();
            private Action _action;
            private bool _runOnUI;
            private bool _disposed;

            /// <summary>
            /// Schedules or resets the debounce timer.
            /// </summary>
            /// <param name="debounceTime">The debounce interval in milliseconds.</param>
            /// <param name="action">The action to execute.</param>
            /// <param name="runOnUI">
            /// If true, the action is posted to the UI thread using <see cref="UIThreadHelper.RunOnUIThread"/>.
            /// </param>
            public void Invoke(int debounceTime, Action action, bool runOnUI)
            {
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    _action = action; //run the latest action only.
                    _runOnUI = runOnUI;

                    if (_timer == null)
                    {
                        _timer = new Timer(TimerCallback, null, debounceTime, Timeout.Infinite);
                    }
                    else
                    {
                        _timer.Change(debounceTime, Timeout.Infinite);
                    }
                }
            }

            private void TimerCallback(object state)
            {
                Action actionToRun;
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    // Capture the action and clear it.
                    actionToRun = _action;
                    _action = null;
                }
                try
                {
                    if (actionToRun != null)
                    {
                        if (_runOnUI)
                        {
                            // Execute on the UI thread (example using a UI helper).
                            UIThreadHelper.RunOnUIThread(DispatcherQueuePriority.Normal, actionToRun);
                        }
                        else
                        {
                            actionToRun();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log or handle exceptions.
                }
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (_disposed)
                        return;
                    _disposed = true;
                }
                _timer?.Dispose();
            }
        }

        // Dictionary to hold debounce entries by key.
        private static readonly ConcurrentDictionary<string, DebounceInvoker> _invokers =
            new ConcurrentDictionary<string, DebounceInvoker>();

        /// <summary>
        /// Schedules the given action to be executed only if no new actions are added
        /// for the given debounce interval for the specified key.
        /// </summary>
        /// <param name="debounceTimeInMs">The debounce interval in milliseconds.</param>
        /// <param name="key">
        /// A unique key to identify this debouncing group. For example, using the calling object's hash
        /// or any unique identifier works well.
        /// </param>
        /// <param name="action">The action to execute.</param>
        /// <param name="shouldRunInUI">
        /// If true, the action is posted to the UI thread using <see cref="UIThreadHelper.RunOnUIThread"/>.
        /// </param>
        public static void Add(int debounceTimeInMs, string key, Action action, bool shouldRunInUI = false)
        {
            // Get or create a debounce invoker for this key.
            var invoker = _invokers.GetOrAdd(key, _ => new DebounceInvoker());
            invoker.Invoke(debounceTimeInMs, action, shouldRunInUI);
        }

        /// <summary>
        /// Removes the debounce invoker associated with the specified key.
        /// </summary>
        /// <param name="key">The unique key of the debounce group to remove.</param>
        /// <returns>True if successfully removed; otherwise, false.</returns>
        public static bool Remove(string key)
        {
            try
            {
                if (_invokers.TryRemove(key, out DebounceInvoker invoker))
                {
                    invoker.Dispose();
                    return true;
                }
            }
            catch
            {
                // Ignoring exceptions; removal failure is non-critical.
            }
            return false;
        }

        /// <summary>
        /// Clears all debounce invokers.
        /// </summary>
        public static void Clear()
        {
            foreach (var key in _invokers.Keys)
            {
                try
                {
                    if (_invokers.TryRemove(key, out DebounceInvoker invoker))
                    {
                        invoker.Dispose();
                    }
                }
                catch
                {
                    // Ignore exceptions during clear.
                }
            }
        }
    }
}
