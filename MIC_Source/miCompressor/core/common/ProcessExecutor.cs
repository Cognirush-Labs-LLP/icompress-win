using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

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
    /// <returns>Exit code if waited; otherwise, 0.</returns>
    public int Execute(string arguments, bool waitForExit = true)
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
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode;
            }
            else
            {
                // Fire-and-forget: return immediately.
                return 0;
            }
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