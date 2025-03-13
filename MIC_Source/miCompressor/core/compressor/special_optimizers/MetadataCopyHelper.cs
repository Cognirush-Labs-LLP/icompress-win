using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

public class MetadataCopyHelper
{
    private readonly ProcessExecutor _executor;

    public MetadataCopyHelper()
    {
        _executor = new ProcessExecutor(@"exiftool\exiftool.exe");
    }

    /// <summary>
    /// Removes unsafe metadata (thumbnail images) from current image and copies from supplied image path. 
    /// </summary>
    /// <param name="copyFromImagePath"></param>
    /// <param name="copyToImagePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">Both file should exist.</exception>
    public int Copy(string copyFromImagePath, string copyToImagePath, bool keepDatetime = true)
    {
        if (string.IsNullOrWhiteSpace(copyFromImagePath) || !File.Exists(copyFromImagePath))
            throw new FileNotFoundException("Image file to copy metadata from, not found", copyFromImagePath);
        if (string.IsNullOrWhiteSpace(copyToImagePath) || !File.Exists(copyToImagePath))
            throw new FileNotFoundException("Image file to copy metadata to, not found", copyToImagePath);

        string arguments = $"-exif:all= -tagsfromfile @ -exif:all -thumbnailimage -unsafe -overwrite_original \"{copyToImagePath}\""; //required mainly for WebP image.

        int returnValue = _executor.Execute(arguments, waitForExit: true);

        arguments = $"-tagsFromFile \"{copyFromImagePath}\" -exif:all -x ThumbnailImage -Software=\"Mass Image Compressor\" -unsafe -overwrite_original \"{copyToImagePath}\"";

        if (keepDatetime)
        {
            // Get the file's Created Date (NTFS timestamp)
            DateTime createdDate = File.GetCreationTime(copyFromImagePath);
            DateTime modifiedDate = File.GetLastWriteTime(copyFromImagePath);

            // Format as "YYYY:mm:dd HH:MM:SS±HH:MM"
            string formattedCreatedDate = createdDate.ToString("yyyy:MM:dd HH:mm:sszzz");
            string formattedModifiedDate = createdDate.ToString("yyyy:MM:dd HH:mm:sszzz");

            arguments += $" -FileModifyDate =\"<{formattedModifiedDate}\" -FileCreateDate=\"<{formattedCreatedDate}\" -P";
        }
           

        return _executor.Execute(arguments, waitForExit: true);
    }
}
