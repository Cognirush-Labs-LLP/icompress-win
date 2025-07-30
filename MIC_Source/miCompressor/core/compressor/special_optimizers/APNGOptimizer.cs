using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Optimizes APNG using apngopt command
/// </summary>
public class APNGOptimizer
{
    private readonly ProcessExecutor _executor;

    /// <summary>
    /// Init
    /// </summary>
    public APNGOptimizer()
    {
        _executor = new ProcessExecutor("apngopt.exe");
    }

    /// <summary>
    /// Optimize APNG (animated PNG, extension may still be .png) and replace original file atomically.
    /// </summary>
    /// <param name="apngImagePath">Full path to APNG file</param>
    /// <returns>Exit code from apngopt process</returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="IOException"></exception>
    public int Optimize(string apngImagePath)
    {
        if (string.IsNullOrWhiteSpace(apngImagePath) || !File.Exists(apngImagePath))
            throw new FileNotFoundException("Image file not found", apngImagePath);

        // Generate a temp file path in application temp directory, same extension as original
        string tempFile = TempDataManager.getTempPreviewFilePath(Path.GetFileName(apngImagePath));
        tempFile = Path.Combine(Path.GetDirectoryName(tempFile)!, Guid.NewGuid() + Path.GetExtension(apngImagePath));

        // Build arguments for apngopt: "input.png" "tempfile.png"
        string arguments = $"\"{apngImagePath}\" \"{tempFile}\"";
        int exitCode = _executor.Execute(arguments, waitForExit: true);

        if (exitCode == 0 && File.Exists(tempFile))
        {
            File.Copy(tempFile, apngImagePath, overwrite: true);
            File.Delete(tempFile);
        }
        else
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            throw new IOException($"APNG optimization failed for {apngImagePath}");
        }

        return exitCode;
    }

}
