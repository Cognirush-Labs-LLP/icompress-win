using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using ImageMagick;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ImageMagick.Factories;
using miCompressor.core.common;


namespace miCompressor.core;

public partial class MediaFileInfo
{
    private bool debugThisClass = true;

    private const uint THUMB_SIZE = CodeConsts.ThumbSize;

    /// <summary>
    /// Returns THUMB_SIZE value if metadata is not loaded or height and width are greater than THUMB_SIZE.
    /// Otherwise, (if height and wdith are smaller than target THUMB_SIZE) returns larger of height of width.
    /// </summary>
    public uint ThumbnailSize
    {
        get
        {
            if (IsMetadataLoaded == false)
                return THUMB_SIZE;
            if (height > THUMB_SIZE || width > THUMB_SIZE)
                return THUMB_SIZE;
            return Convert.ToUInt32(height > width ? height : width);
        }
    }

    private const int MaxParallelThumbnailLoads = 50;

    private static readonly ConcurrentDictionary<string, Task<BitmapImage?>> s_thumbnailTasks = new ConcurrentDictionary<string, Task<BitmapImage?>>();
    string s_thumbnailTasksKey => $"{FilePath}-{ThumbnailSize}";


    private static readonly SemaphoreSlim s_globalLoadSemaphore =
        new SemaphoreSlim(MaxParallelThumbnailLoads, MaxParallelThumbnailLoads);

    private BitmapImage? _thumbnail;

    /// <summary>
    /// Exposes the thumbnail as a bindable property. Loads it on first access.
    /// </summary>
    public BitmapImage? Thumbnail
    {
        get
        {
            if (_thumbnail == null)
            {
                ThumbGenerationTaskPool.Instance.EnqueueTask(FilePath, LoadThumbnailAsync);
                //_ = LoadThumbnailAsync();  // Fire-and-forget async loading, we will raise PropertyChanged when it's done.
                return _thumbnail;
            }
            return _thumbnail;
        }
        private set
        {
            if (_thumbnail != value)
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }
    }

    private async Task LoadThumbnailAsync()
    {
        if (_thumbnail != null) return;  // Lock-free read for already-loaded thumbnail.

        /*
        await LoadImageMetadataAsync();
        // set original image as thumbnail if it's smaller than the thumbnail size
        if (width <= ThumbnailSize && height <= ThumbnailSize)
        {
            Thumbnail = new BitmapImage(new Uri(FilePath));
            return;
        }*/

        string cacheKey = s_thumbnailTasksKey;

        var loadingTask = s_thumbnailTasks.GetOrAdd(cacheKey, _ => LoadThumbnailCoreAsync());

        try
        {
            // Wait for the task (newly created or existing one)
            var result = await loadingTask;

            // Set the result only if the thumbnail hasn't been set by another thread
            if (_thumbnail == null)
            {
                Thumbnail = result;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading thumbnail for {FilePath}: {ex}");
            // Optional: remove the failed task to allow a retry on the next request.
            s_thumbnailTasks.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// Loads the thumbnail with a global concurrency limit of 8.
    /// </summary>
    private async Task<BitmapImage?> LoadThumbnailCoreAsync()
    {
        await s_globalLoadSemaphore.WaitAsync();
        try
        {
            // Simulate actual thumbnail load (replace with your real method)
            return await LoadThumbnailWithFallbackAsync();
        }
        finally
        {
            s_globalLoadSemaphore.Release();
        }
    }

    /// <summary>
    /// Attempts to load the thumbnail using Windows.Storage API first.
    /// Falls back to ImageMagick if Windows.Storage fails.
    /// </summary>
    private async Task<BitmapImage> LoadThumbnailWithFallbackAsync()
    {
        try
        {
            var thumbnail = await TryGetThumbnailFromStorageApiAsync();
            System.Diagnostics.Debug.WriteLineIf(debugThisClass, "Thumbnail from Storage API");
            if (thumbnail != null) return thumbnail;
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine($"File access error for {FilePath}: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in Windows.Storage API for {FilePath}: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }

        // Fall back to ImageMagick for thumbnail generation
        try
        {
            return await GenerateThumbnailWithImageMagickAsync();
        }
        catch (MagickException magickEx)
        {
            System.Diagnostics.Debug.WriteLine($"ImageMagick error for {FilePath}: {magickEx.Message}");
            if (magickEx.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {magickEx.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in ImageMagick for {FilePath}: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }

        Debug.WriteLineIf(debugThisClass, $"Loaded thumbnail for {Path.GetFileName(FilePath)}");

        /*
        try
        {
            var thumbnail = await TryGetThumbnailFromStorageApiAsync();
            System.Diagnostics.Debug.WriteLine("Thumbnail from Storage API");
            if (thumbnail != null) return thumbnail;
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine($"File access error for {FilePath}: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in Windows.Storage API for {FilePath}: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }*/

        // Return a placeholder thumbnail in case of complete failure
        return GeneratePlaceholderThumbnail();
    }

    private async Task<BitmapImage?> TryGetThumbnailFromStorageApiAsync()
    {
        try
        {
            // Run file access in a background thread
            StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);

            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)ThumbnailSize, ThumbnailOptions.None);


            if (thumbnail != null)
            {
                if (thumbnail.Type == ThumbnailType.Icon)
                    return null;

                BitmapImage image = null;

                // Ensure UI thread execution for SetSource
                await UIThreadHelper.RunOnUIThreadAsync(DispatcherQueuePriority.Normal, async () =>
                {
                    try
                    {
                        image = new BitmapImage();
                        await image.SetSourceAsync(thumbnail);
                    }
                    catch (Exception ex)
                    {
                        //
                    }
                });

                return image;
            }
        }
        catch (COMException ex)
        {
            System.Diagnostics.Debug.WriteLine($"COMException while accessing thumbnail for {FilePath}: {ex.Message}");
        }

        return null;
    }

    private async Task<BitmapImage> GenerateThumbnailWithImageMagickAsync()
    {
        BitmapImage thumbnail = null;

        try
        {
            Debug.WriteLine("Generating Thumbnail from ImageMagick.");
            // Process image in the background thread and create the stream
            var stream = await Task.Run(() =>
            {
                try
                {
                    var tempStream = new InMemoryRandomAccessStream();
                    using (var image = new MagickImage(FilePath))
                    {
                        MagickGeometry geometry = new MagickGeometry(ThumbnailSize);
                        geometry.IgnoreAspectRatio = false;
                        geometry.FillArea = false;
                        image.Sample(geometry);

                        image.AutoOrient();  // Correct orientation

                        if (image.HasAlpha)
                        {
                            image.Format = MagickFormat.Png;
                        }
                        else
                        {
                            image.Format = MagickFormat.Jpeg;
                        }

                        image.Write(tempStream.AsStream());
                        tempStream.Seek(0);
                    }
                    return tempStream;
                }
                catch
                {
                    return null;
                }
            });

            if (stream == null)
            {
                throw new Exception("Error generating thumbnail from ImageMagic");
            }
            // Use the stream on the UI thread, then dispose it
            UIThreadHelper.RunOnUIThread(() =>
            {
                thumbnail = new BitmapImage();
                thumbnail.SetSource(stream);
                stream.Dispose();  // Dispose the stream after SetSource
                this.Thumbnail = thumbnail; //HACK: Due to many changes, we now have to set this here instead of returning the thumbnail as this is async task. 
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating thumbnail: {ex.Message}");
        }

        return thumbnail; //BUG: This will always return null as Thumbnail source is assigned in UI task. 
    }

    /// <summary>
    /// Generates a placeholder thumbnail in case all attempts fail.
    /// </summary>
    private BitmapImage GeneratePlaceholderThumbnail()
    {
        BitmapImage placeholder = new BitmapImage();
        System.Diagnostics.Debug.WriteLine($"Returning placeholder thumbnail for {FilePath}");
        return placeholder;
    }

    ~MediaFileInfo()
    {
        s_thumbnailTasks.TryRemove(s_thumbnailTasksKey, out _);
    }
}