using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    string[] MultiFrameSupportedFormats = [".gif", ".webp", ".tif", ".tiff", ".png", ".apng", ".heic", ".avif", ".jp2"];

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

        if (forPreview || files.Count() < Environment.ProcessorCount)
        {
            int parallelismInUnitOfWork = Math.Max(1, (int)Math.Ceiling(Environment.ProcessorCount / (double)files.Count()));
            //MagickNET.SetEnvironmentVariable("OMP_NUM_THREADS", parallelismInUnitOfWork.ToString());
        }
        else
        {
            //MagickNET.SetEnvironmentVariable("OMP_NUM_THREADS", "2");
        }


#if DEBUG
        //MagickNET.SetEnvironmentVariable("OMP_NUM_THREADS", "2");
        //maxParallel = 1;
#endif
        int total = files.Count();
        int errorCount = 0;
        int processedCount = 0;

        // Use a semaphore to limit parallel tasks
        using SemaphoreSlim sem = new(maxParallel);
        var tasks = new List<Task>();

        WarningHelper.Instance.ClearAll();

        foreach (MediaFileInfo file in files)
        {
            await sem.WaitAsync().ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessSingleFileAsync(file, fromMultipleSelectPath
                        , outputSettings, forPreview).ConfigureAwait(false);
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


    OutputFormat[] MultiFrameOutputFormats = [OutputFormat.Webp, OutputFormat.Png, 
        //OutputFormat.heic, 
        OutputFormat.avif, OutputFormat.Gif];


    private void RaiseCancellationEvent(MediaFileInfo mediaInfo)
    {
        ImageCompressedEventArgs evtArgs = new(mediaInfo, "", false, CompressionErrorType.Cancelled);
        ImageCompressed?.Invoke(this, evtArgs);
    }

    /// <summary>
    /// Process a single image file: compress, convert format, handle metadata and animation.
    /// multipleSelectPaths helps determine whether to create parent folder in specific output folder or not. 
    /// </summary>
    /// <returns>Return output path where compressed image is created, this can be temporary path and image may NOT exist if it is is a replace operation.</returns>
    public async Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview)
    {
        if (stop)
        {
            RaiseCancellationEvent(mediaInfo);
            return null;
        }

        string sourcePath = mediaInfo.FilePath;
        string outputPath = mediaInfo.GetOutputPath(settings, multipleSelectPaths, forPreview);

        if (IsGIFToPNG(mediaInfo, outputPath))
        {
            ConvertFromGIFToPNG(mediaInfo, settings, outputPath);
        }
        else if (IsGIFToGIF(mediaInfo, outputPath))
        {
            ConvertFromGIFToGIF(mediaInfo, settings, outputPath);
        }
        else // use ImageMagick
        {
            bool multiFrameInput = false;
            bool outputSupportsMulti = false;


            // Determine if input might have multiple frames (animation or multi-page)
            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (MultiFrameSupportedFormats.Contains(ext))
            {
                outputSupportsMulti = MultiFrameOutputFormats.Contains(OutputFormatHelper.GetOutputFormatFor(outputPath));
                multiFrameInput = true;
            }

            // Prepare Magick.NET objects
            MagickImage? image = null;
            MagickImageCollection? collection = null;

            try
            {
                if (multiFrameInput)
                {
                    if (Path.GetExtension(sourcePath).ToLower().EndsWith("png") && MetadataHelper.IsAnimatedPng(sourcePath))
                        collection = new MagickImageCollection(sourcePath, MagickFormat.APng);
                    else
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
                    if (new[] { ".dng", ".nef", ".cr2", ".cr3" }.Contains(Path.GetExtension(sourcePath).ToLower()))
                    {
                        try
                        {
                            image = await WindowsImageDecoder.LoadImageWithDefaultDecoderAsync(mediaInfo);
                            if (image == null)
                            {
                                image = new MagickImage(sourcePath);
                            }
                            else
                                MicLog.Info($"Used Windows Decoder for {Path.GetFileName(sourcePath)}");
                        }
                        catch (Exception ex)
                        {
                            MicLog.Info($"Windows Decoder crashed, so using fallback. {mediaInfo.ShortName}. No big deal, this happens. Error: {ex.Message}");
                            image = new MagickImage(sourcePath);
                        }
                    }
                    else
                        image = new MagickImage(sourcePath);
                }

                if (stop)
                {
                    RaiseCancellationEvent(mediaInfo);
                    return null;
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

                // If using collection (multiple frames) and preserving animation:
                if (collection != null)
                {
                    ImageOrientationHelper.CorrectOrientation(collection);
                    // Resize all frames according to settings
                    ResizeHelper.ResizeFrames(collection, settings);
                    WatermarkHelper.WatermarkAllFrames(collection, settings);

                    foreach (var frame in collection)
                    {
                        frame.Strip();
                    }
                    // If output format is WebP or other lossy, set quality for each frame
                    if (MagickHelper.CanSetQuality(settings.Format, outputPath))
                    {
                        foreach (var frame in collection)
                        {
                            frame.Quality = Convert.ToUInt32(settings.quality);
                        }
                    }


                    if (stop)
                    {
                        RaiseCancellationEvent(mediaInfo);
                        return null;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    // Write all frames to output file
                    var writeDefine = MagickHelper.GetWriteDefinesFor(settings.Format, outputPath, true, settings.Quality, mediaInfo);

                    if (OutputFormatHelper.GetOutputFormatFor(outputPath) == OutputFormat.Gif)
                    {
                        collection.Optimize();
                        collection.OptimizeTransparency();
                    }

                    if (writeDefine != null)
                        collection.Write(outputPath, writeDefine);
                    else
                        collection.Write(outputPath, MagickHelper.GetMagickFormat(settings.Format, outputPath, isMultiframed: true));

                    OptimizeIfAnimatedPNG(outputPath);
                }
                else if (image != null)
                {
                    ImageOrientationHelper.CorrectOrientation(image);

                    // Single frame image processing
                    // Apply resizing
                    ResizeHelper.ResizeImage(image, settings);
                    WatermarkHelper.ApplyWatermark(image, settings);

                    // Set output quality for lossy formats
                    if (MagickHelper.CanSetQuality(settings.Format, outputPath))
                    {
                        image.Quality = settings.quality;
                    }

                    if (stop)
                    {
                        RaiseCancellationEvent(mediaInfo);
                        return null;
                    }

                    // Write image to output file (format is determined by outputPath extension)
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    var writeDefine = MagickHelper.GetWriteDefinesFor(settings.Format, outputPath, false, settings.Quality, mediaInfo);

                    image.Strip();

                    if (writeDefine != null)
                        image.Write(outputPath, writeDefine);
                    else
                        image.Write(outputPath, MagickHelper.GetMagickFormat(settings.Format, outputPath, isMultiframed: false));

                    QuantizeIfPNGAndAllowedToReduceColors(outputPath, settings);
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
        }
        try
        {
            // At this point, outputPath file should exist (unless compression failed silently but FreezeOutputAsync will check the file creation with required width and height)
            // Finalize the output (ensure original is not replaced with larger file, etc.)

            if (stop)
            {
                RaiseCancellationEvent(mediaInfo);
                return null;
            }

            OptimizeIfPNG(outputPath);
            OptimizeIfGIF(mediaInfo.FilePath, outputPath);

            if (!forPreview)
                (new MetadataCopyHelper()).Copy(mediaInfo.FilePath, outputPath, settings);

            if (stop)
            {
                RaiseCancellationEvent(mediaInfo);
                return null;
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
                    WarningHelper.Instance.AddPostWarning(PostCompressionWarningType.UsedOriginalFile, mediaInfo);
                }
            }

            // Emit event for this file's completion
            ImageCompressedEventArgs evtArgs = new(mediaInfo, outputPath, success);
            ImageCompressed?.Invoke(this, evtArgs);
            return outputPath;
        }
        catch (Exception ex)
        {
            // If any error during image loading or processing, log and record error
            MicLog.Exception(ex, $"Failed to process file: {mediaInfo.FilePath}");
            WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, mediaInfo);
            throw;  // rethrow to be handled in outer loop
        }
    }

    private bool IsGIF(string filePath)
    {
        return OutputFormatHelper.GetOutputFormatFor(filePath) == OutputFormat.Gif;
    }

    private bool IsPNG(string filePath)
    {
        return OutputFormatHelper.GetOutputFormatFor(filePath) == OutputFormat.Png;
    }

    private bool IsGIFToPNG(MediaFileInfo mediaInfo, string outputPath)
    {
        return IsGIF(mediaInfo.FilePath) && IsPNG(outputPath);
    }

    private bool IsGIFToGIF(MediaFileInfo mediaInfo, string outputPath)
    {
        return IsGIF(mediaInfo.FilePath) && IsGIF(outputPath);
    }

    private void ConvertFromGIFToPNG(MediaFileInfo mediaInfo, OutputSettings settings, string outputPath)
    {
        try
        {
            var inputGifFile = mediaInfo.FilePath;
            if (ResizeHelper.NeedsResize(mediaInfo.Height, mediaInfo.Width, settings))
            {
                (uint targetH, uint targetW) = DimensionHelper.GetOutputDimensions(settings, mediaInfo.Height, mediaInfo.Width);
                var resizedGifFilePath = TempDataManager.GetTempFile(Path.GetExtension(mediaInfo.FilePath));
                (new GifOptimizeResize()).Resize(inputGifFile, resizedGifFilePath, targetW, targetH);
                inputGifFile = resizedGifFilePath;
            }
            (new GifToApngConverter()).Convert(inputGifFile, outputPath);
        }
        catch (Exception ex)
        {
            //ignore, failure will be managed by freeze method.
            MicLog.Error($" *** Failed to ConvertFrom GIF to PNG as {ex.Message}, {ex.StackTrace}");
        }
    }

    private void ConvertFromGIFToGIF(MediaFileInfo mediaInfo, OutputSettings settings, string outputPath)
    {
        try
        {
            var inputGifFile = mediaInfo.FilePath;
            if (ResizeHelper.NeedsResize(mediaInfo.Height, mediaInfo.Width, settings))
            {
                (uint targetH, uint targetW) = DimensionHelper.GetOutputDimensions(settings, mediaInfo.Height, mediaInfo.Width);

                (new GifOptimizeResize()).Resize(inputGifFile, outputPath, targetW, targetH);
            }
            else
            {
                File.Copy(inputGifFile, outputPath, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            //ignore, failure will be managed by freeze method.
            MicLog.Error($" *** Failed to ConvertFrom GIF to PNG as {ex.Message}, {ex.StackTrace}");
        }
    }

    private void OptimizeIfPNG(string outputFilePath)
    {
        try
        {
            if (IsPNG(outputFilePath))
                (new PNGOptimizer()).Optimize(outputFilePath);
        }
        catch (Exception ex)
        {
            MicLog.Error($" *** Failed to Optimize PNG as {ex.Message}, {ex.StackTrace}");
        }
    }

    private void QuantizeIfPNGAndAllowedToReduceColors(string outputFilePath, OutputSettings settings)
    {
        try
        {
            if (IsPNG(outputFilePath) && settings.allowLossyPNG)
                (new PNGQuantizer()).Optimize(outputFilePath, settings);
        }
        catch (Exception ex)
        {
            MicLog.Error($" *** Failed to Quantize PNG as {ex.Message}, {ex.StackTrace}");
        }
    }

    private void OptimizeIfAnimatedPNG(string outputFilePath)
    {
        try
        {
            if (IsPNG(outputFilePath))
                (new APNGOptimizer()).Optimize(outputFilePath);
        }
        catch (Exception ex)
        {
            MicLog.Error($" *** Failed to Optimize PNG as {ex.Message}, {ex.StackTrace}");
        }
    }

    private void OptimizeIfGIF(string inputPath, string outputPath)
    {
        try
        {
            if (!IsGIF(outputPath))
                return;

            if (IsGIF(inputPath))
                (new GifOptimizeResize()).Optimize(outputPath);
            else
                (new GifOptimizeResize()).OptimizeAndReduceSize(outputPath);
        }
        catch (Exception ex)
        {
            MicLog.Error($" *** Failed to Optimize GIF as {ex.Message}, {ex.StackTrace}");
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
