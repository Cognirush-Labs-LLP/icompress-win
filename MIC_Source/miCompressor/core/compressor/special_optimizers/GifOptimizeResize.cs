using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Uses ProcessExecutor to optimize a single PNG image using gifsicle command (3rdParty).
/// </summary>
public class GifOptimizeResize
{
    private readonly ProcessExecutor _executor;

    /// <summary>
    /// Initializes 
    /// </summary>
    public GifOptimizeResize()
    {
        _executor = new ProcessExecutor("gifsicle.exe");
    }

    /// <summary>
    /// Optimizes GIF image, especially if the color space was using more than 256 colors. 
    /// </summary>
    /// <param name="gifImagePath"></param>
    /// <returns>0 if all good. non-zero if the operation failed.</returns>
    public int Optimize(string gifImagePath)
    {
        if (string.IsNullOrWhiteSpace(gifImagePath) || !File.Exists(gifImagePath))
            throw new FileNotFoundException("Image file not found", gifImagePath);
        // - Replace file (-b)
        // - Optimize max (-O3)
        // - Make GIF work on some scenario with somewhat increased size (--careful)
        string arguments = $"-b --careful -O3 \"{gifImagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }

    /// <summary>
    /// Optimizes GIF image, especially if the color space was using more than 256 colors. 
    /// </summary>
    /// <param name="gifInputImagePath">Existing GIF file to optimize</param>
    /// <param name="gifOutputPath">Optimized file to be created.</param>
    /// <returns>0 if all good. non-zero if the operation failed.</returns>
    public int Optimize(string gifInputImagePath, string gifOutputPath)
    {
        if (string.IsNullOrWhiteSpace(gifInputImagePath) || !File.Exists(gifInputImagePath))
            throw new FileNotFoundException("Image file not found", gifInputImagePath);
        // - Replace file (-b)
        // - Optimize max (-O3)
        // - Make GIF work on some scenario with somewhat increased size (--careful)
        string arguments = $"--careful -O3 \"{gifInputImagePath}\" -o \"{gifOutputPath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }

    /// <summary>
    /// Optimizes GIF image if converting from high color images.
    /// </summary>
    /// <param name="gifImagePath"></param>
    /// <returns>0 if all good. non-zero if the operation failed.</returns>
    public int OptimizeAndReduceSize(string gifImagePath)
    {
        if (string.IsNullOrWhiteSpace(gifImagePath) || !File.Exists(gifImagePath))
            throw new FileNotFoundException("Image file not found", gifImagePath);
        // - Replace file (-b)
        // - Optimize max (-O3)
        // - Make GIF work on some scenario with somewhat increased size (--careful)
        string arguments = $"-b --careful --dither --lossy --colors=256 -O3 \"{gifImagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }
    /// <summary>
    /// Optimizes GIF image, especially if the color space was using more than 256 colors. 
    /// </summary>
    /// <returns>0 if all good. non-zero if the operation failed.</returns>
    public int Resize(string gifImagePath, string outputGifImagePath, uint width, uint height)
    {
        if (string.IsNullOrWhiteSpace(gifImagePath) || !File.Exists(gifImagePath))
            throw new FileNotFoundException("Image file not found", gifImagePath);
        // - Replace file (-b)
        // - Optimize max (-O3)
        // - Make GIF work on some scenario with somewhat increased size (--careful)
        string arguments = $"\"{gifImagePath}\" --resize {width}x{height} -o \"{outputGifImagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }
}
