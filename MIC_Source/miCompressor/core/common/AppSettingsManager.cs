using CommunityToolkit.WinUI.Converters;
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace miCompressor.core;

/// <summary>
/// Interface for settings storage.
/// </summary>
public interface ISettingsStorage
{
    void Set<T>(string key, T value);
    T? Get<T>(string key);
    T? Get<T>(string key, T defaultValue);
    bool TryGet<T>(string key, out T value);
    bool Exists(string key);
    void Remove(string key);
}

/// <summary>
/// Static class that abstracts local settings storage.
/// It automatically selects between ApplicationData.Current.LocalSettings (for store builds)
/// and file-based storage (for direct/standalone distributions).
/// </summary>
public static class AppSettingsManager
{
    private static bool? _localSettingsAccessible;
    public static bool LocalSettingsAccessible
    {
        get
        {
            if (_localSettingsAccessible.HasValue)
                return _localSettingsAccessible.Value;
            try
            {
                // Attempt to read the LocalSettings instance.
                var settings = ApplicationData.Current.LocalSettings;
                // Optionally, try a harmless read operation:
                var _ = settings.Values;
                _localSettingsAccessible = true;
            }
            catch
            {
                _localSettingsAccessible = false;
            }
            return _localSettingsAccessible.Value;
        }
    }

    // Choose the proper storage implementation at runtime.
    private static readonly ISettingsStorage _storage =
        (CodeConsts.IsForStoreDistribution && LocalSettingsAccessible) ? (ISettingsStorage)new StoreSettingsStorage() : new FileSettingsStorage();

    public static void Set<T>(string key, T value)
    {
        _storage.Set(key, value);
    }

    public static T? Get<T>(string key)
    {
        return _storage.Get<T>(key);
    }

    public static T? Get<T>(string key, T defaultValue)
    {
        return _storage.Get(key, defaultValue);
    }

    public static bool TryGet<T>(string key, out T value)
    {
        return _storage.TryGet(key, out value);
    }


    public static bool Exists(string key)
    {
        return _storage.Exists(key);
    }

    public static void Remove(string key)
    {
        _storage.Remove(key);
    }
}

/// <summary>
/// A helper class for storing and retrieving data from ApplicationData.Current.LocalSettings.
/// Supports generic types for type-safe operations.
/// </summary>
public class StoreSettingsStorage : ISettingsStorage
{
    /// <summary>
    /// Stores a value in ApplicationData.Current.LocalSettings.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The key used to store the value.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value)
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
    public T? Get<T>(string key)
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
    /// Retrieves a value from ApplicationData.Current.LocalSettings.
    /// If the key does not exist, the default value passed to this method of T is returned.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The key used to retrieve the value.</param>
    /// <param name="valueToReturnIfSettingsDoesNotExist">This value is return if key is not found in settings</param>
    /// <returns>The retrieved value or <![CDATA[valueToReturnIfSettingsDoesNotExist]]> if not found.</returns>
    public T? Get<T>(string key, T valueToReturnIfSettingsDoesNotExist)
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
                        return valueToReturnIfSettingsDoesNotExist;
                    }
                }
            }
            return valueToReturnIfSettingsDoesNotExist;
        }
        catch { }
        return valueToReturnIfSettingsDoesNotExist;
    }

    /// <summary>
    /// If the value is already store, sets the value to the out parameter and returns true.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet<T>(string key, out T value)
    {
        try
        {
            if (!Exists(key))
            {
                value = default;
                return false;
            }
            value = Get<T>(key);
        }
        catch
        {
            value = default;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if a key exists in the local settings.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public bool Exists(string key)
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
    public void Remove(string key)
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

/// <summary>
/// Implementation for Standalone (non-store) distributions using file-based storage.
/// Each setting is stored as a separate file in the user config directory.
/// </summary>
public class FileSettingsStorage : ISettingsStorage
{
    // Helper: Get a safe file path for a given key.
    private static string GetFilePath(string key)
    {
        string dir = TempDataManager.GetUserConfigDir();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Sanitize key to be a valid file name.
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            key = key.Replace(invalid, '_');
        }
        return Path.Combine(dir, key + ".json");
    }

    public void Set<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        string path = GetFilePath(key);
        try
        {
            if (value == null)
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }

            // For simple types, store the value directly; for complex types, serialize the value.
            object storedValue = value is string || value is int || value is bool || value is double
                ? value
                : JsonSerializer.Serialize(value);

            // Build an object that holds both the type and the stored value.
            var entry = new
            {
                Type = typeof(T).AssemblyQualifiedName,
                Value = storedValue
            };

            // Write the JSON content to file.
            string json = JsonSerializer.Serialize(entry);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore errors.
        }
    }

    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        string path = GetFilePath(key);
        if (!File.Exists(path))
            return default;

        try
        {
            string json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("Type", out JsonElement typeProperty))
            {
                string? storedType = typeProperty.GetString();
                // If the stored type matches the requested type
                if (typeof(T).AssemblyQualifiedName == storedType && root.TryGetProperty("Value", out JsonElement valueProperty))
                {
                    // For simple types, extract directly.
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)valueProperty.GetString()!;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)valueProperty.GetInt32();
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)valueProperty.GetBoolean();
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        return (T)(object)valueProperty.GetDouble();
                    }
                    else
                    {
                        // For complex types, the value was stored as a JSON string.
                        string storedJson = valueProperty.GetString() ?? "";
                        return JsonSerializer.Deserialize<T>(storedJson);
                    }
                }
                else if (root.TryGetProperty("Value", out JsonElement altValue))
                {
                    // As a fallback, attempt to deserialize the stored value directly.
                    try
                    {
                        return JsonSerializer.Deserialize<T>(altValue.GetRawText());
                    }
                    catch
                    {
                        return default;
                    }
                }
            }
        }
        catch
        {
            return default;
        }
        return default;
    }

    public T? Get<T>(string key, T defaultValue)
    {
        if (!Exists(key))
            return defaultValue;
        return Get<T>(key);
    }

    /// <summary>
    /// If the value is already store, sets the value to the out parameter and returns true.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet<T>(string key, out T value)
    {
        try
        {
            if (!Exists(key))
            {
                value = default;
                return false;
            }
            value = Get<T>(key);
        }
        catch
        {
            value = default;
            return false;
        }
        return true;
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        string path = GetFilePath(key);
        return File.Exists(path);
    }

    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        string path = GetFilePath(key);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore errors.
        }
    }
}
