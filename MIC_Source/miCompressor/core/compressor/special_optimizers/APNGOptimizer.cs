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
    /// Optimize APNG (animated PNG, extension may still be .png)
    /// </summary>
    /// <param name="apngImagePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public int Optimize(string apngImagePath)
    {
        if (string.IsNullOrWhiteSpace(apngImagePath) || !File.Exists(apngImagePath))
            throw new FileNotFoundException("Image file not found", apngImagePath);
        string arguments = $"\"{apngImagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }
}
