using ImageMagick;
using miCompressor.core;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;

namespace miCompressor.IntegrationTests;

/// <summary>
/// Tests here change process-wide Magick limits or hold real RAM, so nothing else may run alongside them.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RealMemoryCollection
{
    public const string Name = "Real memory pressure";
}

/// <summary>
/// Runs the real compression pipeline into real memory exhaustion, nothing about the image processing is faked.
/// </summary>
[Collection(RealMemoryCollection.Name)]
public class RealMemoryPressureTests : IDisposable
{
    private const ulong MB = 1024 * 1024;
    private readonly string _testRoot;
    private readonly ITestOutputHelper _output;
    private readonly ulong _originalMemoryLimit = ResourceLimits.Memory;
    private readonly ulong _originalDiskLimit = ResourceLimits.Disk;

    public RealMemoryPressureTests(ITestOutputHelper output)
    {
        _output = output;
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-real-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    /// <summary>
    /// Magick limited to 100 MB without disk fallback: one 3000x2000 image fits, several at once don't and fail with Magick's real
    /// "cache resources exhausted" error. This is the low-RAM, many-core situation; the retry must recover every image.
    /// </summary>
    [Fact]
    public async Task MagickRunsOutOfMemoryInParallel_FailedImagesAreRetriedAlone_AndAllSucceed()
    {
        var files = await CreateImagesAsync(8, 3000, 2000);
        var settings = CreateSettings();
        var compressor = new RecordingCompressor { Cores = 8 };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        LimitMagickMemory(100 * MB);
        await compressor.CompressImagesAsync(files, false, settings, forPreview: false);
        RestoreMagickLimits();

        int retries = compressor.Attempts.Count(a => a.isRetry);
        _output.WriteLine($"Attempts: {compressor.Attempts.Count}, retries: {retries}, max parallel: {compressor.MaxRunning}");
        foreach (string error in compressor.Errors)
            _output.WriteLine(error);
        Assert.NotNull(completed);
        Assert.Equal(0, completed.FailedCount);
        Assert.Equal(8, completed.SucceededCount);
        Assert.True(retries > 0, "Memory limit did not cause any failure, test proves nothing.");
        Assert.False(WarningHelper.Instance.CompressionErrors.ContainsKey(CompressionErrorType.FailedToCompress));
        foreach (var file in files)
        {
            var info = new MagickImageInfo(file.GetOutputPath(settings, false, false));
            Assert.Equal(3000u, info.Width);
            Assert.Equal(2000u, info.Height);
        }
    }

    /// <summary>
    /// If an image doesn't fit even alone, it is reported as a normal failure, once, after one retry.
    /// </summary>
    [Fact]
    public async Task MagickRunsOutOfMemoryEvenAlone_ReportedAsFailureOnce()
    {
        var files = await CreateImagesAsync(3, 3000, 2000);
        var compressor = new RecordingCompressor { Cores = 8 };
        var failures = new ConcurrentBag<ImageCompressedEventArgs>();
        compressor.ImageCompressed += (_, e) => { if (!e.Success && !e.Skipped) failures.Add(e); };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        LimitMagickMemory(30 * MB);
        await compressor.CompressImagesAsync(files, false, CreateSettings(), forPreview: false);
        RestoreMagickLimits();

        Assert.NotNull(completed);
        Assert.Equal(3, completed.FailedCount);
        Assert.Equal(3, failures.Count);
        Assert.All(failures, f => Assert.Equal(CompressionErrorType.FailedToCompress, f.Error));
        Assert.Equal(3, WarningHelper.Instance.CompressionErrors[CompressionErrorType.FailedToCompress].Count);
        Assert.All(files, f => Assert.Equal(2, compressor.Attempts.Count(a => a.file == f)));
    }

    /// <summary>
    /// Takes real RAM away from the machine until little is free (1.5 GB by default, MIC_RAM_STRESS_FREE_MB to change), then compresses
    /// 24 MP images plus one 100 MP image with nothing faked: real free-memory readings, real estimates, real Magick.
    /// Opt-in as it squeezes the whole machine: set MIC_RAM_STRESS=1.
    /// </summary>
    [RamStressFact]
    public async Task RealLowRam_BatchRunsFewerImagesAtOnce_AndAllSucceed()
    {
        ulong leaveFree = (ulong.TryParse(Environment.GetEnvironmentVariable("MIC_RAM_STRESS_FREE_MB"), out ulong mb) ? mb : 1536) * MB;
        var files = await CreateImagesAsync(15, 6000, 4000);
        MediaFileInfo big = (await CreateImagesAsync(1, 10000, 10000, "big"))[0];
        files.Insert(8, big);
        var settings = CreateSettings();
        var compressor = new RecordingCompressor { Cores = Environment.ProcessorCount }; // above the usual limit, so only low RAM can reduce parallel images; real memory readings
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        ulong availableBefore = SystemMemory.GetAvailableBytes() ?? throw new InvalidOperationException("Can't read free memory.");
        using var ballast = new RamBallast(availableBefore > leaveFree ? availableBefore - leaveFree : 0);
        ulong availableDuring = SystemMemory.GetAvailableBytes()!.Value;
        ulong normalEstimate = BatchMemoryPlanner.EstimateBytes(files[0], settings);
        ulong bigEstimate = BatchMemoryPlanner.EstimateBytes(big, settings);
        _output.WriteLine($"Cores {Environment.ProcessorCount}; free before {availableBefore / MB} MB, held {ballast.Bytes / MB} MB, free now {availableDuring / MB} MB");
        _output.WriteLine($"Estimate per image {normalEstimate / MB} MB, big image {bigEstimate / MB} MB");

        var stopwatch = Stopwatch.StartNew();
        await compressor.CompressImagesAsync(files, false, settings, forPreview: false);
        ulong budget = BatchMemoryPlanner.Budget(compressor.MemoryReadings.First()!.Value); // the first reading sets the batch budget
        var bigRun = compressor.Runs.Single(r => r.file == big && !r.isRetry);
        int overlappingBig = compressor.Runs.Count(r => r.file != big && r.start < bigRun.end && r.end > bigRun.start);
        _output.WriteLine($"Done in {stopwatch.Elapsed.TotalSeconds:F1}s; budget {budget / MB} MB, max parallel {compressor.MaxRunning}, " +
            $"most estimated memory running together {compressor.MaxSharedEstimateBytes / MB} MB, retries {compressor.Attempts.Count(a => a.isRetry)}, " +
            $"images running alongside the big one {overlappingBig}, peak working set {Process.GetCurrentProcess().PeakWorkingSet64 / (long)MB} MB, free at end {SystemMemory.GetAvailableBytes() / MB} MB");
        foreach (string error in compressor.Errors)
            _output.WriteLine(error);

        Assert.NotNull(completed);
        Assert.Equal(16, completed.SucceededCount);
        Assert.True(compressor.MaxRunning < Environment.ProcessorCount, "Low RAM did not reduce parallel images.");
        Assert.True(compressor.MaxRunning > 1, "Low RAM made the whole batch run one image at a time.");
        Assert.True(compressor.MaxSharedEstimateBytes <= budget, "Images running together were over the memory budget.");
    }

    #region Helpers

    private sealed class RamStressFactAttribute : FactAttribute
    {
        public RamStressFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("MIC_RAM_STRESS") != "1")
                Skip = "Squeezes machine RAM; set MIC_RAM_STRESS=1 to run.";
        }
    }

    private static void LimitMagickMemory(ulong bytes)
    {
        ResourceLimits.Memory = bytes;
        ResourceLimits.Disk = 0; // no disk fallback, so exceeding the limit fails like real RAM running out
    }

    private void RestoreMagickLimits()
    {
        ResourceLimits.Memory = _originalMemoryLimit;
        ResourceLimits.Disk = _originalDiskLimit;
    }

    private OutputSettings CreateSettings() => new()
    {
        format = OutputFormat.Jpg,
        quality = 80,
        copyMetadata = false,
        retainDateTime = false,
        dimensionStrategy = DimensionReductionStrategy.KeepSame,
        OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
        OutputFolder = Path.Combine(_testRoot, "out")
    };

    private async Task<List<MediaFileInfo>> CreateImagesAsync(int count, uint width, uint height, string namePrefix = "img")
    {
        string source = Path.Combine(_testRoot, namePrefix + "-source.png");
        using (var image = new MagickImage(new MagickColor("#3a7bd5"), width, height))
        {
            image.Draw(new ImageMagick.Drawing.Drawables().FillColor(MagickColors.Orange).Circle(width / 2.0, height / 2.0, width / 2.0, height / 4.0));
            image.Write(source, MagickFormat.Png);
        }

        var files = new List<MediaFileInfo>();
        for (int i = 0; i < count; i++)
        {
            string path = Path.Combine(_testRoot, $"{namePrefix}{i}.png");
            File.Copy(source, path);
            var media = new MediaFileInfo(_testRoot, new FileInfo(path));
            await TestFileHelper.WaitUntil(() => media.Width == width && media.Height == height, TimeSpan.FromSeconds(30));
            files.Add(media);
        }
        File.Delete(source);
        return files;
    }

    public void Dispose()
    {
        RestoreMagickLimits();
        try { Directory.Delete(_testRoot, recursive: true); } catch { }
    }

    #endregion
}
