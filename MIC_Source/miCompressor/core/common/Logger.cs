using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using miCompressor.core;
using Serilog;

namespace miCompressor.core
{
    /// <summary>
    /// Provides static logging methods with file rotation, caller info, and log caching.
    /// </summary>
    public static class MicLog
    {
        // Cache for last 10 info logs when Info logging is disabled.
        private static readonly ConcurrentQueue<string> InfoCache = new ConcurrentQueue<string>();
        private const int MaxCacheSize = 10;


        /// <summary>
        /// Global flag to enable/disable all logging.
        /// </summary>
        public static bool IsLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Flag to enable/disable Info logging. When false, caches last 10 Info logs.
        /// </summary>
        public static bool IsInfoEnabled { get; set; } = true;

        // Static constructor to initialize Serilog.
        static MicLog()
        {
            // Ensure the log folder exists.
            Directory.CreateDirectory("logs");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 1_000_000, // ~1 MB
                    retainedFileCountLimit: 7,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// Logs an Info message. If Info logging is disabled, caches the message.
        /// </summary>
        /// <param name="message">Message text.</param>
        /// <param name="caller">Calling method name (automatically provided).</param>
        public static void Info(string message, [CallerMemberName] string caller = "")
        {
            if (!IsLoggingEnabled) return;

            if (IsInfoEnabled)
            {
                Log.Information("{Caller} - {Message}", caller, message);
            }
            else
            {
                var cachedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {caller} - {message}";

                // Cache message if disabled and maintain max cache size. Not locking InfoCache operations as it is not critical and won't result in exception.
                if (InfoCache.Count >= MaxCacheSize)
                    InfoCache.TryDequeue(out _);

                InfoCache.Enqueue(cachedMessage);
            }
        }

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">Warning text.</param>
        /// <param name="caller">Calling method name (automatically provided).</param>
        public static void Warning(string message, [CallerMemberName] string caller = "")
        {
            if (!IsLoggingEnabled) return;
            Log.Warning("{Caller} - {Message}", caller, message);
        }

        /// <summary>
        /// Logs an Error message. Dumps cached Info logs first if Info logging is disabled.
        /// </summary>
        /// <param name="message">Error text.</param>
        /// <param name="caller">Calling method name (automatically provided).</param>
        public static void Error(string message, [CallerMemberName] string caller = "")
        {
            if (!IsLoggingEnabled) return;

            // If Info logging is disabled, dump the cached Info logs. Not locking InfoCache operations as it is not critical and won't result in exception.
            if (!IsInfoEnabled && !InfoCache.IsEmpty)
            {
                while (InfoCache.TryDequeue(out var cachedInfo))
                {
                    Log.Information(cachedInfo);
                }
            }
            Log.Error("{Caller} - {Message}", caller, message);
        }

        /// <summary>
        /// Logs exception details including call stack and one-level inner exception if present.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="caller">Calling method name (automatically)</param>
        public static void Exception(Exception ex, [CallerMemberName] string caller = "")
        {
            if (!IsLoggingEnabled) return;

            if (ex.InnerException == null)
            {
                Log.Error(ex, "{Caller} - Exception: {Message}", caller, ex.Message);
            }
            else
            {
                Log.Error(ex, "{Caller} - Exception: {Message}{NewLine}Inner Exception: {InnerMessage}",
                    caller, ex.Message, ex.InnerException.Message);
            }
        }

        /// <summary>
        /// Asynchronously zips all log files into a single archive and copies it to the common temp directory.
        /// Returns the full path of the zip file. Will be automatically deleted in next application run. 
        /// </summary>
        /// <returns>Zip file full path.</returns>
        public static async Task<string> CreateLogsZipAsync()
        {
            Log.CloseAndFlush();

            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                return null;
            }

            string tempDir = TempDataManager.GetCommonTempDir();
            string zipFilePath = Path.Combine(tempDir, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            // Run the zip creation in a background thread to avoid blocking.
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(logDir, zipFilePath);
            });

            return zipFilePath;
        }
    }
}
