using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace miCompressor.core;

/// <summary>
/// Strips metadata while keeping the colour information needed to render the image as the source does.
/// Without it, a wide-gamut image (e.g. Display P3) is shown as if its pixel values were sRGB, which shifts its colours.
/// </summary>
internal static class ColorProfileHelper
{
    // Same chunks Strip() excludes from PNG, minus the colour chunks (cHRM, gAMA, iCCP, sRGB).
    // zTXt must stay allowed: Magick drops iCCP when tEXt and zTXt are both excluded. PNGOptimizer removes the leftover text.
    private const string PngExcludedChunks = "bKGD,caNv,eXIf,iTXt,pHYs,tEXt,zCCP,date";

    /// <summary>
    /// Removes metadata like <see cref="IMagickImage.Strip"/>, but keeps colour information valid for the output format.
    /// Embeds the ICC profile where the output can carry it; otherwise converts pixels to sRGB so colours stay correct.
    /// Set the image quality before calling, as lossy JPEG XL cannot carry an ICC profile.
    /// </summary>
    public static void StripMetadata(IMagickImage<byte> image, string outputPath)
    {
        IColorProfile? profile = image.GetColorProfile();
        image.Strip();
        image.SetArtifact("png:exclude-chunk", PngExcludedChunks);

        // Viewers assume sRGB when there is no profile, so an sRGB profile only adds bytes.
        if (profile == null || IsSrgb(profile))
            return;

        image.SetProfile(profile);
        if (CanEmbed(profile, image, outputPath))
            return;

        try
        {
            image.TransformColorSpace(ColorProfiles.SRGB);
        }
        catch (MagickException ex)
        {
            MicLog.Warning($"Could not convert colours to sRGB, profile '{profile.Description}' dropped: {ex.Message}");
        }
        image.RemoveProfile(profile.Name);
    }

    /// <summary>
    /// Gives every frame the file's colour profile. Magick's APNG and animated WebP readers drop it, and its JPEG XL reader
    /// keeps it on the first frame only.
    /// </summary>
    public static void ApplyProfileToAllFrames(MagickImageCollection collection, string sourcePath)
    {
        IColorProfile? profile = collection.Select(f => f.GetColorProfile()).FirstOrDefault(p => p != null)
            ?? ReadProfileFromFile(sourcePath);
        if (profile == null)
            return;

        foreach (var frame in collection.Where(f => f.GetColorProfile() == null))
            frame.SetProfile(profile);
    }

    private static IColorProfile? ReadProfileFromFile(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".png":
            case ".apng":
                // Read as plain PNG: that reader keeps iCCP. Ping skips decoding pixels.
                using (var image = new MagickImage())
                {
                    image.Ping(path, new MagickReadSettings { Format = MagickFormat.Png });
                    return image.GetColorProfile();
                }
            case ".webp":
                return ReadWebPProfile(path);
            default:
                return null;
        }
    }

    // WebP keeps the profile in an ICCP chunk of its RIFF container, before any image data.
    private static IColorProfile? ReadWebPProfile(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        reader.BaseStream.Position = 12; // "RIFF", file size, "WEBP"
        while (reader.BaseStream.Length - reader.BaseStream.Position >= 8)
        {
            string type = Encoding.ASCII.GetString(reader.ReadBytes(4));
            uint size = reader.ReadUInt32();
            if (type == "ICCP")
                return size <= reader.BaseStream.Length - reader.BaseStream.Position ? new ColorProfile(reader.ReadBytes((int)size)) : null;
            if (type is "ANIM" or "ANMF" or "VP8 " or "VP8L")
                return null;
            reader.BaseStream.Position += size + (size & 1); // chunks are padded to even size
        }
        return null;
    }

    private static bool IsSrgb(IColorProfile profile) =>
        profile.ColorSpace == ColorSpace.sRGB && (profile.Description?.Contains("sRGB", StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool CanEmbed(IColorProfile profile, IMagickImage<byte> image, string outputPath)
    {
        return OutputFormatHelper.GetOutputFormatFor(outputPath) switch
        {
            OutputFormat.Jpg => true, // also stores CMYK and grayscale profiles
            // Magick writes these as RGB, so only an RGB profile describes the pixels. JPEG XL keeps the profile only when lossless.
            OutputFormat.Png or OutputFormat.Webp or OutputFormat.avif => profile.ColorSpace == ColorSpace.sRGB,
            OutputFormat.Jxl => profile.ColorSpace == ColorSpace.sRGB && image.Quality >= 100,
            _ => false, // GIF: viewers mostly ignore its profiles
        };
    }
}
