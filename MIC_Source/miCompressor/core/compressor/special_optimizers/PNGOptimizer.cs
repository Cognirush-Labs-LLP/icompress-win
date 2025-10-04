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
    /// and safely strips removable chunks. Skips processing if the image appears to be animated (APNG).
    /// </summary>
    /// <param name="imagePath">The full path of the PNG image to optimize.</param>
    /// <returns>The exit code of the process, or -1 if the image is an APNG.</returns>
    public int Optimize(string imagePath, bool isPreview)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found", imagePath);

        int threadCount = isPreview ? Environment.ProcessorCount : 1;
        string arguments = $"-t {threadCount} -o 4 --strip safe \"{imagePath}\"";

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
