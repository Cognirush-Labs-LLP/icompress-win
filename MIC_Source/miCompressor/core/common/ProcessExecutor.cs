using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Executes external processes and allows either synchronous waiting or fire-and-forget mode.
/// The executables must be copied to 3rdParty folder.
/// </summary>
public class ProcessExecutor
{
    /// <summary>
    /// Gets the full path of the executable to run.
    /// </summary>
    public string ExePath { get; }

    /// <summary>
    /// Base directory where 3rd-party executables are located.
    /// </summary>
    public static readonly string ThirdPartyBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Get3rdPartyDir());

    /// <summary>
    /// Longest the tool may run before it is stopped, so a tool stuck on a broken file can't hold up a batch forever.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of <see cref="ProcessExecutor"/> with the given executable name.
    /// </summary>
    /// <param name="exeName">The name or relative path of the executable inside the 3rdParty folder.</param>
    /// <exception cref="ArgumentNullException">Thrown when exeName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the constructed executable path does not exist.</exception>
    public ProcessExecutor(string exeName)
    {
        if (string.IsNullOrWhiteSpace(exeName))
            throw new ArgumentNullException(nameof(exeName), "Executable name cannot be null or empty.");

        ExePath = Path.Combine(ThirdPartyBasePath, exeName);

        if (!File.Exists(ExePath))
            throw new FileNotFoundException("Executable not found in 3rdParty folder", ExePath);
    }

    /// <summary>
    /// Executes the process with the specified arguments.
    /// </summary>
    /// <param name="arguments">Command-line arguments for the process.</param>
    /// <param name="waitForExit">
    /// If true, the method waits for the process to complete and returns the exit code;
    /// if false, the process is started and the method returns immediately.
    /// </param>
    /// <returns>Exit code if waited; otherwise, 0. -1 if the process could not run or was stopped after <see cref="Timeout"/>.</returns>
    public int Execute(string arguments, bool waitForExit = true)
    {
        try
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = ExePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                // Only redirect output when waiting for exit, so we can capture messages.
                if (waitForExit)
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                }

                process.Start();

                if (waitForExit)
                {
                    // Optionally, you can capture output for logging or error handling.
                    // Both outputs are read at the same time: a tool that fills one pipe while we wait on the other never exits
                    // (broken files can make tools print a warning per frame).
                    Task<string> outputReader = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorReader = process.StandardError.ReadToEndAsync();

                    // A stuck tool is stopped and reported as failed (-1); callers already handle a failed tool.
                    bool exited = process.WaitForExit(Timeout);
                    if (!exited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit();
                    }
                    // The pipes close once the tool has exited; the wait is bounded in case something it started keeps them open.
                    Task.WaitAll([outputReader, errorReader], TimeSpan.FromSeconds(30));

                    if (!exited)
                    {
                        MicLog.Error($"Stopped {ExePath} after {Timeout.TotalMinutes:0.#} minutes, arguments: {arguments}");
                        return -1;
                    }
                    return process.ExitCode;
                }
                else
                {
                    // Fire-and-forget: return immediately.
                    return 0;
                }
            }
        } catch 
        {
            MicLog.Error($"Error executing {ExePath} with arguments: {arguments}");
            return -1;
        }
    }

    /// <summary>
    /// Determines the appropriate 3rd-party directory based on system architecture.
    /// </summary>
    /// <returns>The selected 3rd-party directory name.</returns>
    private static string Get3rdPartyDir()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return "3rdParty_arm64"; //future

        return "3rdParty_x64";
    }
}