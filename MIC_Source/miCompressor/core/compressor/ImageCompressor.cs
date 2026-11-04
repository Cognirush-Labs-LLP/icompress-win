using ImageMagick;
using System;
using System.Collections.Concurrent;
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
    private sealed class OutputLocationException(string directory, Exception innerException)
        : IOException($"Cannot write to output directory '{directory}'.", innerException);

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

    string[] MultiFrameSupportedFormats = [".gif", ".webp", ".tif", ".tiff", ".png", ".apng", ".heic", ".avif", ".jp2", ".jxl"];

    /// <summary>
    /// Cancel any on-going compression.
    /// </summary>
    public void CancelCompression()
    {
        stop = true;
    }

    private const string SKIPPED = "SKIPPED";

    /// <summary>
    /// Upper limit of images compressed at the same time.
    /// </summary>
    protected virtual int ProcessorCount => Environment.ProcessorCount;

    /// <summary>
    /// Free physical memory in bytes, null if unknown.
    /// </summary>
    protected virtual ulong? GetAvailableMemoryBytes() => SystemMemory.GetAvailableBytes();

    /// <summary>
    /// Estimated peak memory needed to compress the file.
    /// </summary>
    protected virtual ulong EstimateMemoryBytes(MediaFileInfo file, OutputSettings settings)
        => BatchMemoryPlanner.EstimateBytes(file, settings);

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
        List<MediaFileInfo> fileList = files.ToList();

        // Callers start this from the UI thread; planning and retries are long running, so none of it may run there.
        await Task.Run(() => CompressImagesInBackgroundAsync(fileList, fromMultipleSelectPath, outputSettings, forPreview)).ConfigureAwait(false);
    }

    private async Task CompressImagesInBackgroundAsync(List<MediaFileInfo> fileList, bool fromMultipleSelectPath, OutputSettings outputSettings, bool forPreview)
    {
#if DEBUG
        //MagickNET.SetEnvironmentVariable("OMP_NUM_THREADS", "2");
#endif
        int total = fileList.Count;
        int errorCount = 0;
        int skipped = 0;

        // Starts images as CPU cores and free memory allow; see MemoryGate for the rule.
        MemoryGate gate = new(ProcessorCount, GetAvailableMemoryBytes);
        if (gate.BudgetBytes != ulong.MaxValue)
            MicLog.Info($"Compressing {total} images, up to {ProcessorCount} at a time within a memory budget of {gate.BudgetBytes / (1024 * 1024)} MB.");
        var compressionTasks = new List<Task>();
        // Files that ran out of memory; they are tried again one at a time once the rest of the batch is done.
        var retryQueue = new ConcurrentQueue<MediaFileInfo>();

        WarningHelper.Instance.ClearAll();

        // Returns false if the file ran out of memory and should be tried again later. Any other outcome is reported.
        async Task<bool> TryCompressAsync(MediaFileInfo file, bool isRetry)
        {
            try
            {
                var outputpath = await ProcessSingleFileAsync(file, fromMultipleSelectPath
                    , outputSettings, forPreview, isRetry).ConfigureAwait(false);

                if (outputpath == SKIPPED)
                {
                    ImageCompressedEventArgs args = new(file, null, success: false, skipped: true);
                    ImageCompressed?.Invoke(this, args);
                    Interlocked.Increment(ref skipped);
                }
                return true;
            }
            catch (Exception ex) when (!isRetry && !stop && BatchMemoryPlanner.IsOutOfMemory(ex))
            {
                MicLog.Warning($"Ran out of memory compressing {file.FilePath}, will try again after other images. {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Log unexpected error and mark as failure
                MicLog.Exception(ex, $"Exception compressing file: {file.FilePath}");
                // Record a compression error for user notification
                CompressionErrorType errorType = ex is OutputLocationException
                    ? CompressionErrorType.AccessDenied
                    : CompressionErrorType.FailedToCompress;
                WarningHelper.Instance.AddCompressionError(errorType, file);
                Interlocked.Increment(ref errorCount);
                // Fire event for this file failure
                ImageCompressedEventArgs args = new(file, null, success: false, skipped: false, errorType);
                ImageCompressed?.Invoke(this, args);
                return true;
            }
        }

        foreach (MediaFileInfo file in fileList)
        {
            ulong memoryEstimate = EstimateMemoryBytes(file, outputSettings);
            if (await gate.WaitToStartAsync(memoryEstimate).ConfigureAwait(false))
                MicLog.Info($"Low memory: compressing {file.FilePath} alone.");

            // Keep only tasks still running (or failed, so WhenAll reports them): batches of 100K+ images must not hold every finished task.
            compressionTasks.RemoveAll(t => t.IsCompletedSuccessfully);
            compressionTasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (!await TryCompressAsync(file, isRetry: false).ConfigureAwait(false))
                        retryQueue.Enqueue(file);
                }
                finally
                {
                    gate.Finished(memoryEstimate);
                }
            }));
        }

        // Wait for all files to finish processing
        await Task.WhenAll(compressionTasks).ConfigureAwait(false);

        if (!retryQueue.IsEmpty)
        {
            MicLog.Info($"Retrying {retryQueue.Count} image(s) one at a time after running out of memory.");
            // Free Magick images that were not disposed but are no longer used, before images run alone.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            foreach (MediaFileInfo file in retryQueue)
                await TryCompressAsync(file, isRetry: true).ConfigureAwait(false);
        }

        // Fire completion event with summary
        int successCount = total - Volatile.Read(ref errorCount) - Volatile.Read(ref skipped);
        CompressionCompletedEventArgs completedArgs = new(total, successCount, Volatile.Read(ref errorCount));
        CompressionCompleted?.Invoke(this, completedArgs);
    }


    OutputFormat[] MultiFrameOutputFormats = [OutputFormat.Webp, OutputFormat.Png, 
        //OutputFormat.heic, 
        OutputFormat.avif, OutputFormat.Gif, OutputFormat.Jxl];


    private void RaiseCancellationEvent(MediaFileInfo mediaInfo)
    {
        ImageCompressedEventArgs evtArgs = new(mediaInfo, "", false, false, CompressionErrorType.Cancelled);
        ImageCompressed?.Invoke(this, evtArgs);
    }

    /// <summary>
    /// Process a single image file: compress, convert format, handle metadata and animation.
    /// multipleSelectPaths helps determine whether to create parent folder in specific output folder or not. 
    /// </summary>
    /// <returns>Return output path where compressed image is created, this can be temporary path and image may NOT exist if it is is a replace operation.</returns>
    public Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview)
        => ProcessSingleFileAsync(mediaInfo, multipleSelectPaths, settings, forPreview, isRetry: false);

    /// <inheritdoc cref="ProcessSingleFileAsync(MediaFileInfo, bool, OutputSettings, bool)"/>
    /// <remarks><paramref name="isRetry"/> is true if this file ran out of memory earlier in the same batch. It already passed the 'skip if file exists' check then, and
    /// the output it may have partially written must be overwritten instead of being skipped.</remarks>
    protected virtual async Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview, bool isRetry)
    {
        if (stop)
        {
            RaiseCancellationEvent(mediaInfo);
            return null;
        }

        string sourcePath = mediaInfo.FilePath;
        string outputPath = mediaInfo.GetOutputPath(settings, multipleSelectPaths, forPreview);


        if (!isRetry && !forPreview && settings.SkipIfFileExists && settings.OutputLocationSettings != OutputLocationSetting.ReplaceOriginal && !mediaInfo.IsReplaceOperation && File.Exists(outputPath))
        {
            return SKIPPED;
        }

        // create output directory if doesn't exist.
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new OutputLocationException(dir, ex);
            }
        }

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
                        ColorProfileHelper.ApplyProfileToAllFrames(collection, sourcePath);
                        // Input has multiple frames
                        if (!outputSupportsMulti)
                        {
                            // Output format does not support animation: use first frame and warn user
                            image = new MagickImage(collection[0]);  // take first frame, a copy, so disposing the collection doesn't dispose the image
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
                        // image is a copy: free the decoded frame now, else it holds memory until the garbage collector finalizes it.
                        collection.Dispose();
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
                    bool origSupported = origExt.TrimStart('.') == "jpeg" || CodeConsts.SupportedOutputExtensions.Contains(origExt.TrimStart('.'));
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
                    // Magick's JXL writer draws each frame over the previous one and stores frames as given.
                    // Partial frames that only draw over the previous frame are kept as-is (coalescing would bloat the file);
                    // APNG is excluded as its frames may replace their area instead, which Magick doesn't expose.
                    bool isJxlOutput = OutputFormatHelper.GetOutputFormatFor(outputPath) == OutputFormat.Jxl;
                    bool keepPartialFrames = isJxlOutput
                        && settings.dimensionStrategy == DimensionReductionStrategy.KeepSame
                        && !settings.ApplyWatermark
                        && collection[0].Format != MagickFormat.APng
                        && collection.All(f => f.GifDisposeMethod is GifDisposeMethod.None or GifDisposeMethod.Undefined);
                    bool coalesceForJxl = isJxlOutput && !keepPartialFrames;
                    if (coalesceForJxl)
                    {
                        // Full frames must replace the previous frame, else semi-transparent pixels get blended twice.
                        // Done before resize/watermark, so their coalescing leaves these frames unchanged.
                        collection.Coalesce();
                        SetFramesToReplacePrevious(collection);
                    }
                    // Resize all frames according to settings
                    ResizeHelper.ResizeFrames(collection, settings, keepPartialFrames);
                    WatermarkHelper.WatermarkAllFrames(collection, settings);
                    if (coalesceForJxl)
                        SetFramesToReplacePrevious(collection); // coalescing in resize/watermark resets it

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

                    if (!LosslessVsLossyWriter.TryWrite(collection, settings, mediaInfo, outputPath))
                    {
                        foreach (var frame in collection)
                        {
                            ColorProfileHelper.StripMetadata(frame, outputPath);
                        }

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
                    }

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

                    if (!LosslessVsLossyWriter.TryWrite(image, settings, mediaInfo, outputPath))
                    {
                        var writeDefine = MagickHelper.GetWriteDefinesFor(settings.Format, outputPath, false, settings.Quality, mediaInfo);

                        ColorProfileHelper.StripMetadata(image, outputPath);

                        if (writeDefine != null)
                            image.Write(outputPath, writeDefine);
                        else
                            image.Write(outputPath, MagickHelper.GetMagickFormat(settings.Format, outputPath, isMultiframed: false));
                    }

                    QuantizeIfPNGAndAllowedToReduceColors(outputPath, settings);
                }
            }
            catch (Exception ex)
            {
                // If any error during image loading or processing, log and record error
                MicLog.Exception(ex, $"Failed to process file: {mediaInfo.FilePath}");
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

            OptimizeIfPNG(outputPath, forPreview);
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
            ImageCompressedEventArgs evtArgs = new(mediaInfo, outputPath, success, skipped: false);
            ImageCompressed?.Invoke(this, evtArgs);
            return outputPath;
        }
        catch (Exception ex)
        {
            // If any error during image loading or processing, log and record error
            MicLog.Exception(ex, $"Failed to process file: {mediaInfo.FilePath}");
            //WarningHelper.Instance.AddCompressionError(CompressionErrorType.FailedToCompress, mediaInfo);
            throw;  // rethrow to be handled in outer loop
        }
    }

    private static void SetFramesToReplacePrevious(MagickImageCollection collection)
    {
        foreach (var frame in collection)
            frame.GifDisposeMethod = GifDisposeMethod.Background;
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

    private void OptimizeIfPNG(string outputFilePath, bool isPreview)
    {
        try
        {
            if (IsPNG(outputFilePath))
            {
                (new PNGOptimizer()).Optimize(outputFilePath, isPreview);
            }
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
    public bool Skipped { get; }
    public CompressionErrorType Error { get; }
    public ImageCompressedEventArgs(MediaFileInfo file, string? outputPath, bool success, bool skipped, CompressionErrorType error = CompressionErrorType.None)
    {
        FileInfo = file;
        OutputPath = outputPath;
        Success = success;
        Skipped = skipped;
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
