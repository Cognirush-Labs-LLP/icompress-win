using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
    public int Copy(string copyFromImagePath, string copyToImagePath, OutputSettings settings)
    {
        string arguments = "";

        if (settings.CopyMetadata)
        {
            if (string.IsNullOrWhiteSpace(copyFromImagePath) || !File.Exists(copyFromImagePath))
                throw new FileNotFoundException("Image file to copy metadata from, not found", copyFromImagePath);
            if (string.IsNullOrWhiteSpace(copyToImagePath) || !File.Exists(copyToImagePath))
                throw new FileNotFoundException("Image file to copy metadata to, not found", copyToImagePath);

            arguments = $"-exif:all= -tagsfromfile @ -exif:all -thumbnailimage -unsafe -overwrite_original \"{copyToImagePath}\""; //required mainly for WebP image, when created from RAW

            _executor.Execute(arguments, waitForExit: true);

            string options = "";

            options += " -EXIF:all ";
            if (settings.CopyIPTC && !settings.SkipSensitiveMetadata)
                options += " -IPTC:all ";
            if (settings.CopyXMP && !settings.SkipSensitiveMetadata)
                options += " -XMP:all ";

            options += " -ThumbnailImage= -Software=\"Mass Image Compressor\" -ICC_Profile -trailer:all= -unsafe ";

            if (settings.SkipSensitiveMetadata)
            {
                options += " -GPS*= -*Serial*= -PersonInImage= -Time:all= -comment= -*Comment= ";
            }

            string dpi = "72";
            string dpiUnit = "inches";
            if (settings.DimensionStrategy == DimensionReductionStrategy.FitInFrame || settings.DimensionStrategy == DimensionReductionStrategy.FixedWidth)
            {
                if (OutputFormatHelper.GetOutputFormatFor(copyToImagePath) == OutputFormat.Jpg)
                    options += " -EXIF:ResolutionUnit=inches -EXIF:XResolution=300 -EXIF:YResolution=300 -JFIF:XResolution=300 -JFIF:YResolution=300 -JFIF:ResolutionUnit=inches ";
                else
                    options += " -dpi=300 ";
            }
            else
            {
                if (OutputFormatHelper.GetOutputFormatFor(copyToImagePath) == OutputFormat.Jpg)
                    options += " -EXIF:ResolutionUnit=inches -EXIF:XResolution=72 -EXIF:YResolution=72 -JFIF:XResolution=72 -JFIF:YResolution=72 -JFIF:ResolutionUnit=inches ";
                else
                    options += " -dpi=72 ";
            }

            if (String.IsNullOrEmpty(options)) //nothing to copy
                return 0;

            arguments = $" -tagsFromFile \"{copyFromImagePath}\" {options} -overwrite_original \"{copyToImagePath}\" ";
        }
        else
        {
            arguments = $" -all:all= -overwrite_original \"{copyToImagePath}\" ";
        }

        if (settings.RetainDateTime)
        {
            // Get the file's Created Date (NTFS timestamp)
            DateTime createdDate = File.GetCreationTime(copyFromImagePath);
            DateTime modifiedDate = File.GetLastWriteTime(copyFromImagePath);

            // Format as "YYYY:mm:dd HH:MM:SS±HH:MM"
            string formattedCreatedDate = createdDate.ToString("yyyy:MM:dd HH:mm:sszzz");
            string formattedModifiedDate = createdDate.ToString("yyyy:MM:dd HH:mm:sszzz");

            arguments += $" -FileModifyDate=\"<{formattedModifiedDate}\" -FileCreateDate=\"<{formattedCreatedDate}\" ";
        }

        return _executor.Execute(arguments, waitForExit: true);
    }
}
