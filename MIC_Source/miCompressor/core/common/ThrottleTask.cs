using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace miCompressor.core
{
    /// <summary>
    /// Provides a way to throttle tasks to run at most once per interval.
    /// </summary>
    public static class ThrottleTask
    {
        /// <summary>
        /// Each throttle "bucket" is represented by a ThrottleInvoker.
        /// </summary>
        private class ThrottleInvoker
        {
            private Timer _timer;
            private readonly object _lock = new object();
            private bool _scheduled = false;
            private Action _action;
            private bool _runOnUI;
            private bool _disposed;

            public void Invoke(int throttleTime, Action action, bool runOnUI)
            {
                lock (_lock)
                {
                    if (_disposed)
                        return; //gracefully exit. 

                    if (_scheduled)
                    {
                        // Already scheduled – ignore additional calls
                        return;
                    }

                    _scheduled = true;
                    _action = action;
                    _runOnUI = runOnUI;

                    if (_timer == null)
                    {
                        // Create a new timer that fires once after throttleTime.
                        _timer = new Timer(TimerCallback, null, throttleTime, Timeout.Infinite);
                    }
                    else
                    {
                        // Reuse the timer instance.
                        _timer.Change(throttleTime, Timeout.Infinite);
                    }
                }
            }

            private void TimerCallback(object state)
            {
                try
                {
                    if (_disposed)
                        return; //gracefully exist. 

                    // Execute the action. If _runOnUI is true, use your UI helper.
                    if (_runOnUI)
                    {
                        // For example, if using WPF:
                        // Application.Current.Dispatcher.Invoke(_action);
                        UIThreadHelper.RunOnUIThread(_action);
                    }
                    else
                    {
                        _action();
                    }
                }
                finally
                {
                    lock (_lock)
                    {
                        // Allow a new schedule after the current execution.
                        _scheduled = false;
                    }
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

        // A dictionary to hold throttle entries by key.
        private static readonly ConcurrentDictionary<string, ThrottleInvoker> _invokers =
            new ConcurrentDictionary<string, ThrottleInvoker>();

        /// <summary>
        /// Schedules the given action to be executed at most once per <paramref name="throttleTimeInMs"/> for the given key.
        /// </summary>
        /// <param name="throttleTimeInMs">The throttling interval in milliseconds.</param>
        /// <param name="key">A unique key to identify this throttling group. Calling object's hash is a good choice for the key if not using custom hash generation.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="shouldRunInUI">
        /// If true, the action is posted to the UI thread using <c>UIThreadHelper.RunOnUIThread</c>.
        /// </param>
        public static void Add(int throttleTimeInMs, string key, Action action, bool shouldRunInUI = false)
        {
            // Get or create a throttle invoker for this key.
            var invoker = _invokers.GetOrAdd(key, _ => new ThrottleInvoker());
            invoker.Invoke(throttleTimeInMs, action, shouldRunInUI);
        }

        /// <summary>
        /// Removes the throttle invoker associated with the specified key.
        /// </summary>
        public static bool Remove(string key)
        {
            try
            {
                if (_invokers.TryRemove(key, out ThrottleInvoker invoker))
                {
                    // If necessary, dispose the timer within the invoker.
                    invoker.Dispose();
                    return true;
                }
            }
            catch { } //ignore exceptin, we will return false. That too doesn't matter!
            return false;
        }

        /// <summary>
        /// Clears all throttle invokers.
        /// </summary>
        public static void Clear()
        {
            foreach (var key in _invokers.Keys)
            {
                try
                {
                    if (_invokers.TryRemove(key, out ThrottleInvoker invoker))
                    {
                        invoker.Dispose();
                    }
                }
                catch { } // Clear is called on application exit. May the exit be peaceful. 
            }
        }
    }

}
