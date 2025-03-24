using System.IO;

namespace miCompressor.core.compressor
{
    /// <summary>
    /// This uses PNGQuant to reduce PNG Size. PNGQuant reduces image size by reducing the quality. If done carefully, pngquant doesn't 
    /// </summary>
    public class PNGQuantizer
    {
        private readonly ProcessExecutor _executor;

        /// <summary>
        /// Initializes a new instance of PNGOptimizer.
        /// </summary>
        /// <param name="oxipngExePath">
        /// The full path to the Oxipng executable.
        /// Expected to be in the '3rdParty' directory of the project.
        /// </param>
        public PNGQuantizer()
        {
            _executor = new ProcessExecutor("pngquant.exe");
        }

        /// <summary>
        /// Optimizes a single PNG image 
        /// </summary>
        /// <param name="imagePath">The full path of the PNG image to optimize.</param>
        public int Optimize(string imagePath, OutputSettings settings)
        {
            string quality = "90-100";
            if (settings.quality > 90)
            {
                quality = $"{settings.quality}-100";
            }
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("Image file not found", imagePath);


            string arguments = $"--quality={quality} -f --ext .png \"{imagePath}\""; //regardless of quality set by user, we keep minimum 90% quality for PNG as pngquant introduces heavy dithering in many cases below 90% SSIM

            return _executor.Execute(arguments, waitForExit: true);
        }
    }
}
