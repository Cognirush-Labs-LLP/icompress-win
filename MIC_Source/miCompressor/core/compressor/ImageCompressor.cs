using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace miCompressor.core;

/// <summary>
/// Compresses images using Magick.NET with parallel processing and format conversion.
/// </summary>
public class ImageCompressor
{
    /// <summary>
    /// Event fired when an image has been processed (success or failure).
    /// </summary>
    public event EventHandler<ImageCompressedEventArgs>? ImageCompressed;

    /// <summary>
    /// Event fired when the entire compression operation is completed.
    /// </summary>
    public event EventHandler<CompressionCompletedEventArgs>? CompressionCompleted;

    /// <summary>
    /// A flag is better than a complex cancellation token here.
    /// </summary>
    private bool stop = false;
    /// <summary>
    /// Cancel any on-going compression.
    /// </summary>
    public void CancelCompression()
    {
        stop = true;
    }

    /// <summary>
    /// Compress and convert a collection of images according to the given settings.
    /// </summary>
    /// <param name="files">List of MediaFileInfo objects to compress.</param>
    /// <param name="outputSettings">Output settings (format, quality, dimensions, metadata handling, etc.).</param>
    /// <param name="forPreview">If true, output file will be created in temp folder.</param>
    /// <param name="fromMultipleSelectPath">If True, and OutputSetting is a specific folder, we create parent folder name of selected folder in output folder. e.g. if selected folder is selected\path and output folder is output\folder, if this argument is true, then input file selected\path\input\image.jpg will be created as output\folder\path\input\image.jpg, otherwise output\folder\input\image.jpg</param>
    public async Task CompressImagesAsync(IEnumerable<MediaFileInfo> files, bool fromMultipleSelectPath, OutputSettings outputSettings, bool forPreview)
    {
        stop = false;

        if (files == null) throw new ArgumentNullException(nameof(files));
        // Determine max degree of parallelism (half the CPU core count)
        int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
#if DEBUG
        //maxParallel = 1;
#endif
        int total = files.Count();
        int errorCount = 0;
        int processedCount = 0;

        // Use a semaphore to limit parallel tasks
        using SemaphoreSlim sem = new(maxParallel);
        var tasks = new List<Task>();

        foreach (MediaFileInfo file in files)
        {
            await sem.WaitAsync().ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessSingleFileAsync(file, fromMultipleSelectPath
                        ,outputSettings, forPreview).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Log unexpected error and mark as failure
                    MicLog.Exception(ex, $"Exception compressing file: {file.FilePath}");
                    // Record a compression error for user notification
                    WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, file);
                    Interlocked.Increment(ref errorCount);
                    // Fire event for this file failure
                    ImageCompressedEventArgs args = new(file, null, success: false);
                    ImageCompressed?.Invoke(this, args);
                }
                finally
                {
                    Interlocked.Increment(ref processedCount);
                    sem.Release();
                }
            }));
        }

        // Wait for all files to finish processing
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Fire completion event with summary
        int successCount = total - Volatile.Read(ref errorCount);
        CompressionCompletedEventArgs completedArgs = new(total, successCount, Volatile.Read(ref errorCount));
        CompressionCompleted?.Invoke(this, completedArgs);
    }

    string[] MultiFrameSupportedFormats = [".gif", ".webp", ".tif", ".tiff", ".png", ".apng", ".heic", ".avif", ".jp2"];
    OutputFormat[] MultiFrameOutputFormats = [OutputFormat.Webp, OutputFormat.Tiff, OutputFormat.Png, OutputFormat.heic, OutputFormat.avif];
    

    private void RaiseCancellationEvent(MediaFileInfo mediaInfo)
    {
        ImageCompressedEventArgs evtArgs = new(mediaInfo, "", false, CompressionErrorType.Cancelled);
        ImageCompressed?.Invoke(this, evtArgs);
    }

    /// <summary>
    /// Process a single image file: compress, convert format, handle metadata and animation.
    /// multipleSelectPaths helps determine whether to create parent folder in specific output folder or not. 
    /// </summary>
    private async Task ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview)
    {
        if (stop)
        {
            RaiseCancellationEvent(mediaInfo);
            return;
        }

        string sourcePath = mediaInfo.FilePath;
        string outputPath = mediaInfo.GetOutputPath(settings, multipleSelectPaths, forPreview);
        bool replaceOriginal = settings.format == OutputFormat.KeepSame
                                && settings.outputLocationSettings == OutputLocationSetting.ReplaceOriginal;
        bool multiFrameInput = false;
        bool outputSupportsMulti = false;

        
        // Determine if input might have multiple frames (animation or multi-page)
        string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (MultiFrameSupportedFormats.Contains(ext))
        {
            outputSupportsMulti = settings.format == OutputFormat.KeepSame || MultiFrameOutputFormats.Contains(settings.format);
            multiFrameInput = true;
        }

        // Prepare Magick.NET objects
        MagickImage? image = null;
        MagickImageCollection? collection = null;
        try
        {
            if (multiFrameInput)
            {
                // Load all frames if input is multi-frame 
                collection = new MagickImageCollection(sourcePath);
                if (collection.Count > 1)
                {
                    // Input has multiple frames
                    if (!outputSupportsMulti)
                    {
                        // Output format does not support animation: use first frame and warn user
                        image = new MagickImage(collection[0]);  // take first frame
                        // Detach first frame from collection so we can dispose collection without disposing the image
                        collection.RemoveAt(0);
                        collection.Dispose();
                        collection = null;
                        // Log and warn that animation will be lost
                        MicLog.Warning($"Animation frames dropped for: {mediaInfo.FilePath}");
                        WarningHelper.Instance.AddPostWarning(PostCompressionWarningType.AnimationLost, mediaInfo);
                    }
                    else
                    {
                        // We will preserve animation frames
                        image = null; // not used; we'll process the collection frames directly
                    }
                }
                else
                {
                    // Only one frame in input
                    image = new MagickImage(collection[0]);
                    //collection.Dispose(); //is this ok?
                    collection = null;
                }
            }
            else
            {
                // Single-frame input, load normally
                image = new MagickImage(sourcePath);
            }

            if (stop)
            {
                RaiseCancellationEvent(mediaInfo);
                return;
            }

            // Determine actual output format by file extension
            string outExt = Path.GetExtension(outputPath).ToLowerInvariant();

            // If output format was "KeepSame" but original format was not supported, 
            // we have effectively changed format (e.g., GIF -> JPEG default). Warn user.
            if (settings.format == OutputFormat.KeepSame)
            {
                string origExt = Path.GetExtension(sourcePath).ToLowerInvariant();
                bool origSupported = CodeConsts.SupportedOutputExtensions.Contains(origExt.TrimStart('.'));
                if (!origSupported)
                {
                    WarningHelper.Instance.AddPostWarning(PostCompressionWarningType.FileFormatChanged, mediaInfo);
                    MicLog.Info($"Output format changed for {mediaInfo.FilePath} (original format not supported).");
                }
            }

            image?.AutoOrient();

            // If using collection (multiple frames) and preserving animation:
            if (collection != null)
            {
                // Resize all frames according to settings
                ResizeHelper.ResizeFrames(collection, settings);

                // If output format is WebP or other lossy, set quality for each frame
                if (outExt == ".webp" || outExt == ".avif" || outExt == ".heic")
                {
                    foreach (var frame in collection)
                    {
                        frame.Quality = Convert.ToUInt32(settings.quality);
                    }
                }

                // Metadata handling: for multi-frame, apply metadata to first frame if needed
                if (settings.copyMetadata)
                {
                    // We assume global metadata is in first frame
                    MetadataHelper.FilterAndCopyMetadata(collection[0], collection[0], MetadataCopyMode.AllExceptSensitive);
                }
                else
                {
                    // Strip metadata from first frame (and hence from output file)
                    collection[0].Strip();
                }

                if (stop)
                {
                    RaiseCancellationEvent(mediaInfo);
                    return;
                }

                // Write all frames to output file
                collection.Write(outputPath);
            }
            else if (image != null)
            {
                // Single frame image processing
                // Apply resizing
                ResizeHelper.ResizeImage(image, settings);

                // Set output quality for lossy formats
                if (outExt == ".jpg" || outExt == ".jpeg" || outExt == ".webp" || outExt == ".avif" || outExt == ".heic")
                {
                    image.Quality = settings.quality;
                }

                // Handle metadata according to user selection
                if (settings.copyMetadata)
                {
                    // Preserve metadata (EXIF, IPTC, XMP) except sensitive fields
                    MetadataHelper.FilterAndCopyMetadata(image, image, MetadataCopyMode.AllExceptSensitive);
                }
                else
                {
                    // Remove all metadata
                    image.Strip();
                }

                if (stop)
                {
                    RaiseCancellationEvent(mediaInfo);
                    return;
                }

                // Write image to output file (format is determined by outputPath extension)
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                image.Write(outputPath);
            }
        }
        catch (Exception ex)
        {
            // If any error during image loading or processing, log and record error
            MicLog.Exception(ex, $"Failed to process file: {mediaInfo.FilePath}");
            WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, mediaInfo);
            throw;  // rethrow to be handled in outer loop
        }
        finally
        {
            // Dispose Magick objects to free memory
            image?.Dispose();
            collection?.Dispose();
        }

        try
        {
            // At this point, outputPath file should exist (unless compression failed silently but FreezeOutputAsync will check the file creation with required width and height)
            // Finalize the output (ensure original is not replaced with larger file, etc.)

            if (stop)
            {
                RaiseCancellationEvent(mediaInfo);
                return;
            }

            var freezeResult = await mediaInfo.FreezeOutputAsync(outputPath, settings).ConfigureAwait(false);
            bool success = true;
            if (freezeResult.failedToFreezeOutput)
            {
                // Compression output was invalid or not usable
                success = false;
                MicLog.Error($"Compressed output invalid for file: {mediaInfo.FilePath}");
                WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, mediaInfo);
            }
            else
            {
                if (freezeResult.wasOriginalFileUsed)
                {
                    // The original file was kept (output was larger or not beneficial)
                    MicLog.Warning($"Original file kept (no size reduction) for: {mediaInfo.FilePath}");
                    WarningHelper.Instance.AddPostWarning(PostCompressionWarningType.FileSizeIncreased, mediaInfo);
                }
            }

            // Emit event for this file's completion
            ImageCompressedEventArgs evtArgs = new(mediaInfo, outputPath, success);
            ImageCompressed?.Invoke(this, evtArgs);
        }
        catch (Exception ex)
        {
            // If any error during image loading or processing, log and record error
            MicLog.Exception(ex, $"Failed to process file: {mediaInfo.FilePath}");
            WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, mediaInfo);
            throw;  // rethrow to be handled in outer loop
        }
    }
}

/// <summary>
/// Event arguments for a compressed image result.
/// </summary>
public class ImageCompressedEventArgs : EventArgs
{
    public MediaFileInfo FileInfo { get; }
    public string? OutputPath { get; }
    public bool Success { get; }
    public CompressionErrorType Error { get; }
    public ImageCompressedEventArgs(MediaFileInfo file, string? outputPath, bool success, CompressionErrorType error = CompressionErrorType.None)
    {
        FileInfo = file;
        OutputPath = outputPath;
        Success = success;
        Error = error;
    }
}

/// <summary>
/// Event arguments for completion of a compression batch.
/// </summary>
public class CompressionCompletedEventArgs : EventArgs
{
    public int TotalFiles { get; }
    public int SucceededCount { get; }
    public int FailedCount { get; }
    public CompressionCompletedEventArgs(int totalFiles, int succeeded, int failed)
    {
        TotalFiles = totalFiles;
        SucceededCount = succeeded;
        FailedCount = failed;
    }
}
