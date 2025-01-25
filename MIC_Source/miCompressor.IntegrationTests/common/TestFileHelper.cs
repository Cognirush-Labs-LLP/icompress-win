using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.IntegrationTests
{

    internal static class TestFileHelper
    {
        /// <summary>
        /// Waits until a condition is met or times out.
        /// </summary>
        public static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (!condition())
            {
                if (DateTime.UtcNow - startTime > timeout)
                    throw new TimeoutException("Condition not met within timeout.");

                await Task.Delay(100); // Check every 100ms
            }
        }

        /// <summary>
        /// Creates a test directory structure in the temp folder.
        /// It picks corresponding test images from "test_imgs" and places them in the correct paths.
        /// </summary>
        /// <param name="testName">The name of the test, used to create a unique test directory.</param>
        /// <param name="relativePaths">A list of file paths to create inside the test directory.</param>
        /// <returns>The full path of the test directory created.</returns>
        public static string CreateDirStructure(string testName, ICollection<string> relativePaths)
        {
            // Get test images directory next to the integration test project
            string testImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_imgs");

            if (!Directory.Exists(testImagesDir))
                throw new DirectoryNotFoundException($"Test images directory not found: {testImagesDir}");

            // Create test directory under temp
            string testRootDir = Path.Combine(Path.GetTempPath(), "tests", testName);
            Directory.CreateDirectory(testRootDir);

            foreach (string relativePath in relativePaths)
            {
                string targetPath = Path.Combine(testRootDir, relativePath);

                if (File.Exists(targetPath)) //Do not create if the test file already exists. We don't delete the test files to speed up the test execution, helps in regression
                    continue;

                string targetDir = Path.GetDirectoryName(targetPath) ?? testRootDir;

                // Ensure the directory exists
                Directory.CreateDirectory(targetDir);

                // Determine the required file extension
                string extension = Path.GetExtension(targetPath);
                if (string.IsNullOrEmpty(extension))
                    throw new ArgumentException($"Invalid file path (missing extension): {relativePath}");

                // Find the first available test image with the same extension
                string matchingImage = Directory.GetFiles(testImagesDir, $"*{extension}")
                                                .FirstOrDefault() ?? throw new FileNotFoundException($"No test image with extension {extension} found in {testImagesDir}");

                // Copy and rename the image
                File.Copy(matchingImage, targetPath, overwrite: true);
            }

            return testRootDir;
        }
    }
}
