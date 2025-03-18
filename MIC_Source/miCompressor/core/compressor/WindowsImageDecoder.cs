using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using ImageMagick;
using System.Runtime.InteropServices.WindowsRuntime;
using System;
using Windows.Storage;
using System.Runtime.InteropServices;
using miCompressor.core;
using System.IO;

public static class WindowsImageDecoder
{
    /// <summary>
    /// Loads an image using Windows' built-in decoder and creates a MagickImage.
    /// Fully optimized for performance and memory safety.
    /// This is mainly for camera formats, shouldn't use for common formats such as jpeg/png/webp/gif etc.
    /// </summary>
    /// <param name="filePath">Full path to the image file.</param>
    /// <returns>MagickImage instance.</returns>
    public static async Task<MagickImage> LoadImageWithDefaultDecoderAsync(MediaFileInfo mediaInfo)
    {
        using (IRandomAccessStream stream = await OpenFileStreamAsync(mediaInfo.FilePath))
        {
            // Decode image using Windows' built-in decoder (WIC)
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            //if height and width is different than original, abort and fall back to ImageMagick
            // note that some variance (let's say 5%) in the size is acceptable,
            // 1. if the JPEG preview stored is almost same as original. 
            // 2. Sometimes the cropping is considered differently 
            if (!IsHeightAndWidthWithinVariance(decoder, mediaInfo.Height, mediaInfo.Width))
            {
                MicLog.Info($"Variance of size between embedded JPEG and Sensor data is high for {mediaInfo.ShortName}. Width in preview is {decoder.OrientedPixelWidth} vs sensor data's {mediaInfo.Width}. Will use sensor data.");
                return null;
            }
            // Get SoftwareBitmap in BGRA format
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            return await ConvertSoftwareBitmapToMagickImageAsync(softwareBitmap);
        }
    }

    public static bool IsHeightAndWidthWithinVariance(BitmapDecoder fromWindows, uint actualHeight, uint actualWidth)
    {
        return  IsWithinVariance(fromWindows.OrientedPixelHeight, actualHeight) && IsWithinVariance(fromWindows.OrientedPixelWidth, actualWidth);
    }
    private static bool IsWithinVariance(uint value, uint target) => Math.Abs((int)value - (int)target) <= target * 0.05;

    /// <summary>
    /// Opens a file stream asynchronously.
    /// </summary>
    private static async Task<IRandomAccessStream> OpenFileStreamAsync(string filePath)
    {
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        return await file.OpenAsync(FileAccessMode.Read);
    }

    /// <summary>
    /// Converts a SoftwareBitmap to a MagickImage.
    /// </summary>
    /// <param name="softwareBitmap">The SoftwareBitmap to convert.</param>
    /// <returns>A Task representing the asynchronous operation, with a MagickImage as result.</returns>
    public static async Task<MagickImage> ConvertSoftwareBitmapToMagickImageAsync(SoftwareBitmap softwareBitmap)
    {
        if (softwareBitmap == null)
            throw new ArgumentNullException(nameof(softwareBitmap));

        // Cast dimensions to uint for buffer size calculation.
        uint pixelWidth = (uint)softwareBitmap.PixelWidth;
        uint pixelHeight = (uint)softwareBitmap.PixelHeight;
        const int bytesPerPixel = 4; // For BGRA8 format
        uint bufferSize = pixelWidth * pixelHeight * bytesPerPixel;

        // Allocate a buffer to hold the pixel data.
        IBuffer buffer = new Windows.Storage.Streams.Buffer(bufferSize);
        softwareBitmap.CopyToBuffer(buffer);

        // Convert IBuffer to byte array.
        byte[] pixelData = buffer.ToArray();

        // Prepare settings for reading the raw pixel data.
        var settings = new MagickReadSettings
        {
            // Magick.NET expects dimensions as int.
            Width = (uint)pixelWidth,
            Height = (uint)pixelHeight,
            Format = MagickFormat.Bgra
        };

        // Create the MagickImage from raw pixel data on a background thread.
        return await Task.Run(() =>
        {
            return new MagickImage(pixelData, settings);
        });
    }

    /*
    /// <summary>
    /// Converts a SoftwareBitmap to a MagickImage using direct pointer access.
    /// </summary>
    /// <param name="softwareBitmap">The SoftwareBitmap to convert.</param>
    /// <returns>A Task with the resulting MagickImage.</returns>
    private static async Task<MagickImage> ConvertSoftwareBitmapToMagickImageAsync2(SoftwareBitmap softwareBitmap)
    {
        if (softwareBitmap == null)
            throw new ArgumentNullException(nameof(softwareBitmap));

        // Prepare the MagickReadSettings based on the SoftwareBitmap dimensions.
        var settings = new MagickReadSettings
        {
            Width = (uint)softwareBitmap.PixelWidth,
            Height = (uint)softwareBitmap.PixelHeight,
            Format = MagickFormat.Bgra
        };

        using (BitmapBuffer bitmapBuffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
        {
            using (var reference = bitmapBuffer.CreateReference())
            {
                // Wrap the pointer access in an unsafe block.
                unsafe
                {
                    byte* dataInBytes;
                    uint capacity;
                    // Cast the reference to our COM interface to access the underlying buffer.
                    var memoryBufferByteAccess = (IMemoryBufferByteAccess)reference;
                    memoryBufferByteAccess.GetBuffer(out dataInBytes, out capacity);

                    // Offload the MagickImage creation to a background thread.

                    // Create an IntPtr from the pointer.
                    IntPtr ptr = new IntPtr(dataInBytes);

                    byte[] arr = new byte[capacity];
                    Marshal.Copy((IntPtr)ptr, arr, 0, (int) capacity);
                    return new MagickImage(arr, 0, capacity, settings);
                }
            }
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0F6F87C8")]
    public unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    } */
}