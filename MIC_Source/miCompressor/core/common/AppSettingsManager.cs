using System;
using Windows.Storage;

namespace miCompressor.core
{
    /// <summary>
    /// A helper class for storing and retrieving data from ApplicationData.Current.LocalSettings.
    /// Supports generic types for type-safe operations.
    /// </summary>
    public static class AppSettingsManager
    {
        /// <summary>
        /// Stores a value in ApplicationData.Current.LocalSettings.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="key">The key used to store the value.</param>
        /// <param name="value">The value to store.</param>
        public static void Set<T>(string key, T value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Key cannot be null or empty.", nameof(key));

                if (value == null)
                {
                    ApplicationData.Current.LocalSettings.Values.Remove(key);
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values[key] = value is string || value is int || value is bool || value is double
                        ? (object)value
                        : System.Text.Json.JsonSerializer.Serialize(value);
                }
            }
            catch
            {
                //ignore, happens when appliation is run without installing. Let's not crash when people are have fun without installing. 
            }
        }

        /// <summary>
        /// Retrieves a value from ApplicationData.Current.LocalSettings.
        /// If the key does not exist, the default value of T is returned.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The key used to retrieve the value.</param>
        /// <returns>The retrieved value or default(T) if not found.</returns>
        public static T? Get<T>(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Key cannot be null or empty.", nameof(key));

                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? storedValue))
                {
                    if (storedValue is T typedValue)
                    {
                        return typedValue;
                    }

                    if (storedValue is string jsonString && typeof(T) != typeof(string))
                    {
                        try
                        {
                            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
                        }
                        catch (Exception)
                        {
                            return default;
                        }
                    }
                }
                return default;
            }
            catch { }
            return default;
        }

        /// <summary>
        /// Checks if a key exists in the local settings.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public static bool Exists(string key)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(key) && ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a key from local settings.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public static void Remove(string key)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove(key);
                }
            }
            catch
            {
                //Most probably because LocalSettings is not accessible as application is not installed.
            }
        }
    }
}
