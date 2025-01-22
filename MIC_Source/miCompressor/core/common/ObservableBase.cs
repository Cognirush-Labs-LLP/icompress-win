using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Linq;


namespace miCompressor.core
{

    /// <summary>
    /// This attribute helps generate a public property that auto implements getter and setters for ObservableBase to trigger a change event.
    /// </summary>
    /// <remarks>
    /// Do Not Create variable name with first letter capital or something that cannot be capitalized such as underscore. A good variable name 'width'. Bad variable names '_width', 'Width'.
    /// Additionally, class using `AutoNotifyAttribute` must not be an inner class of any other class.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AutoNotifyAttribute : Attribute { }

    /// <summary>
    /// Base class for observable objects that supports property change notifications
    /// without requiring explicit private fields.
    /// </summary>
    /// <example>
    /// <code>
    /// public int Width
    /// {
    ///     get => GetProperty&lt;int&gt;(); // Retrieve the current value from the backing store.
    ///     set
    ///     {
    ///         // Custom logic before change:
    ///         if (value < 0)
    ///             throw new ArgumentException("Width cannot be negative");
    ///         
    ///         // Optionally get the previous value (if needed)
    ///         int previous = GetProperty&lt;int&gt;();
    ///         
    ///         // Attempt to update the property. SetProperty returns true if the value changed.
    ///         if (SetProperty(value, (oldVal, newVal) =>
    ///             {
    ///                 // This callback is only invoked if the property value actually changes.
    ///                 Console.WriteLine($"Width changed from {oldVal} to {newVal}");
    ///             }))
    ///         {
    ///             // Execute additional custom logic if needed after the property changed
    ///             DoSomethingExtra();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ObservableBase : INotifyPropertyChanged
    {
        // Thread-safe storage for property values
        private readonly Dictionary<string, object?> _propertyValues = new();

        // Optional callbacks for property changes
        private readonly Dictionary<string, Action<object?, object?>?> _changeCallbacks = new();

        // Lock for thread-safe access
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets a property value. If the property is not set, returns the provided default value.
        /// </summary>
        /// <typeparam name="T">Type of the property value.</typeparam>
        /// <param name="defaultValue">Optional default value (default is null or default(T)).</param>
        /// <param name="propertyName">Name of the property (automatically inferred).</param>
        /// <returns>The stored value or the default if not set.</returns>
        protected T GetProperty<T>(T defaultValue = default!, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            _lock.EnterReadLock();
            try
            {
                return _propertyValues.TryGetValue(propertyName, out var value) ? (T)value! : defaultValue;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Sets a property value and triggers PropertyChanged event if the value has changed.
        /// Also optionally invokes a callback when the value changes.
        /// </summary>
        /// <typeparam name="T">Type of the property value.</typeparam>
        /// <param name="value">New value to set.</param>
        /// <param name="onChanged">Optional callback executed when the property value changes.</param>
        /// <param name="propertyName">Name of the property (automatically inferred).</param>
        /// <returns>True if the property value was changed, false otherwise.</returns>
        protected bool SetProperty<T>(T value, Action<T, T>? onChanged = null, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null) return false;

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_propertyValues.TryGetValue(propertyName, out var existingValue) &&
                    EqualityComparer<T>.Default.Equals((T?)existingValue, value))
                {
                    return false; // No change
                }

                _lock.EnterWriteLock();
                try
                {
                    _propertyValues[propertyName] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                // Invoke property change notification
                raisePropertyChanged(propertyName);

                // Invoke change callback if provided
                if (onChanged != null)
                {
                    _changeCallbacks[propertyName] = (oldValue, newValue) => onChanged((T)oldValue!, (T)newValue!);
                    _changeCallbacks[propertyName]?.Invoke(existingValue, value);
                }

                return true;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Raises event in UI thread if the UI Thread is available. Ignores raising the event otherwise.  
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void raisePropertyChanged(string propertyName)
        {
            var dispatcher = UIThreadHelper.UIThreadDispatcherQueue;

            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                // ignore if UI cannot listen.
            }
        }

    }

}
