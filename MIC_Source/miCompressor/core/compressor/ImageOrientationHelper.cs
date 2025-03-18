using System;
using ImageMagick;

namespace miCompressor.core;


public static class ImageOrientationHelper
{
    /// <summary>
    /// Corrects the orientation of a MagickImage by applying actual rotation and flipping, then removing EXIF metadata.
    /// </summary>
    /// <param name="image">The MagickImage to be corrected.</param>
    public static void CorrectOrientation(IMagickImage image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        // Get the original orientation
        OrientationType orientation = image.Orientation;

        image.AutoOrient();
        return;
    }

    /// <summary>
    /// Corrects the orientation for each frame in a MagickImageCollection (e.g., GIFs, TIFFs).
    /// </summary>
    /// <param name="collection">The MagickImageCollection to be corrected.</param>
    public static void CorrectOrientation(MagickImageCollection collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        foreach (var frame in collection)
        {
            CorrectOrientation(frame);
        }
    }
}
