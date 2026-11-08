using ImageMagick;
using miCompressor.core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace miCompressor.ui;

/// <param name="Source">The image to show.</param>
/// <param name="UprightWidth">Full width of the decoded image with its orientation corrected, before any edit.</param>
/// <param name="UprightHeight">Full height of the decoded image with its orientation corrected, before any edit.</param>
public sealed record LoadedImage(ImageSource Source, uint UprightWidth, uint UprightHeight);

/// <summary>
/// Loads images for the Edit screen and Preview through the decoder and edit code that compression uses, so what is shown matches what gets written.
/// </summary>
public static class EditedImageLoader
{
    /// <summary>
    /// Call from the UI thread. The decoding runs in the background.
    /// </summary>
    /// <param name="edit">Edit to apply, null to load the image as it is.</param>
    /// <param name="maxLongEdge">Scales down larger images to save memory, null to keep the full size.</param>
    /// <returns>Null if the image can't be decoded.</returns>
    public static async Task<LoadedImage?> LoadAsync(MediaFileInfo media, ImageEdit? edit, uint? maxLongEdge = null)
    {
        var decoded = await Task.Run(() => DecodeAsync(media, edit, maxLongEdge));
        if (decoded == null)
            return null;

        using SoftwareBitmap bitmap = decoded.Value.bitmap;
        var source = new SoftwareBitmapSource();
        await source.SetBitmapAsync(bitmap);
        return new LoadedImage(source, decoded.Value.uprightWidth, decoded.Value.uprightHeight);
    }

    private static async Task<(SoftwareBitmap bitmap, uint uprightWidth, uint uprightHeight)?> DecodeAsync(MediaFileInfo media, ImageEdit? edit, uint? maxLongEdge)
    {
        try
        {
            using MagickImage image = await ImageCompressor.LoadStillImageAsync(media);
            ImageOrientationHelper.CorrectOrientation(image);
            (uint uprightWidth, uint uprightHeight) = (image.Width, image.Height);

            ImageEditor.Apply(image, edit, App.OutputSettingsInstance.format.GetOutputExtension(media.FilePath));

            if (maxLongEdge is uint limit && Math.Max(image.Width, image.Height) > limit)
                image.Thumbnail(new MagickGeometry(limit, limit));

            // The pixels below are read as they are, and they must be RGB for that (not CMYK, for example).
            if (image.ColorSpace != ColorSpace.sRGB)
                image.ColorSpace = ColorSpace.sRGB;

            using IPixelCollection<byte> pixels = image.GetPixels();
            byte[] bytes = pixels.ToByteArray(PixelMapping.BGRA)!;
            using SoftwareBitmap straight = SoftwareBitmap.CreateCopyFromBuffer(bytes.AsBuffer(), BitmapPixelFormat.Bgra8, (int)image.Width, (int)image.Height, BitmapAlphaMode.Straight);
            return (SoftwareBitmap.Convert(straight, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied), uprightWidth, uprightHeight);
        }
        catch (Exception ex)
        {
            MicLog.Exception(ex, $"Cannot load image to edit or preview: {media.FilePath}");
            return null;
        }
    }
}
