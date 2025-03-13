using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Helps convert GIF image to Animated PNG image in optimum way. Doesn't support resizing see <![CDATA[GifOptimizeResize]]> to resize GIF before using this.
/// </summary>
class GifToApngConverter
{
    private readonly ProcessExecutor _executor;

    /// <summary>
    /// Init
    /// </summary>
    public GifToApngConverter()
    {
        _executor = new ProcessExecutor("gif2apng.exe");
    }

    /// <summary>
    /// Converts GIF to Animated PNG. 
    /// </summary>
    /// <param name="gifImagePath"></param>
    /// <param name="pngImagePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public int Convert(string gifImagePath, string pngImagePath)
    {
        if (string.IsNullOrWhiteSpace(gifImagePath) || !File.Exists(gifImagePath))
            throw new FileNotFoundException("Image file not found", gifImagePath);

        string arguments = $"\"{gifImagePath}\" \"{pngImagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }
}
