using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Uses ProcessExecutor to optimize a single PNG image using Oxipng.
/// </summary>
public class PNGOptimizer
{
    private readonly ProcessExecutor _executor;

    /// <summary>
    /// Initializes a new instance of PNGOptimizer.
    /// </summary>
    /// <param name="oxipngExePath">
    /// The full path to the Oxipng executable.
    /// Expected to be in the '3rdParty' directory of the project.
    /// </param>
    public PNGOptimizer()
    {
        _executor = new ProcessExecutor("oxipng.exe");
    }

    /// <summary>
    /// Optimizes a single PNG image using one thread, level 4 optimization,
    /// and strips chunks that don't affect how the image renders.
    /// </summary>
    /// <param name="imagePath">The full path of the PNG image to optimize.</param>
    /// <param name="keepColorType">Keep the PNG's colour type, instead of changing it to a smaller one such as palette.</param>
    /// <returns>The exit code of the process, or -1 if the image is an APNG.</returns>
    public int Optimize(string imagePath, bool isPreview, bool keepColorType = false)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found", imagePath);

        int threadCount = isPreview ? Environment.ProcessorCount : 1;
        // 'safe' stripping keeps iCCP, sRGB and cICP but drops gAMA and cHRM, which matter when nothing overrides them.
        var colorChunks = PngColorChunks.TypesIn(imagePath);
        bool gammaDecidesColors = colorChunks.Contains("gAMA") && !colorChunks.Overlaps(["iCCP", "sRGB", "cICP"]);
        string strip = gammaDecidesColors ? "--keep display,gAMA,cHRM" : "--strip safe";
        string colorType = keepColorType ? "--nc " : "";
        string arguments = $"-t {threadCount} -o 4 {colorType}{strip} \"{imagePath}\"";

        return _executor.Execute(arguments, waitForExit: true);
    }

    /// <summary>
    /// Checks whether the PNG image is animated (APNG) by scanning for the "acTL" chunk.
    /// </summary>
    /// <param name="filePath">Path to the PNG file.</param>
    /// <returns>True if the file is an animated PNG; otherwise, false.</returns>
    private bool IsAnimatedPng(string filePath)
    {
        // Read the file into memory.
        byte[] fileBytes = File.ReadAllBytes(filePath);

        // "acTL" in ASCII is represented by these byte values: 0x61, 0x63, 0x54, 0x4C.
        for (int i = 0; i < fileBytes.Length - 3; i++)
        {
            if (fileBytes[i] == 0x61 && fileBytes[i + 1] == 0x63 &&
                fileBytes[i + 2] == 0x54 && fileBytes[i + 3] == 0x4C)
            {
                return true;
            }
        }
        return false;
    }
}
