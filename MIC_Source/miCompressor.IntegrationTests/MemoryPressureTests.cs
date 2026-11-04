using ImageMagick;
using miCompressor.core;
using System.Collections.Concurrent;

namespace miCompressor.IntegrationTests;

[Collection("Image compression")]
public class MemoryPressureTests : IDisposable
{
    private const ulong MB = 1024 * 1024;
    private const ulong GB = 1024 * MB;
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(30);
    private readonly string _testRoot;

    public MemoryPressureTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    #region Estimates

    [Fact]
    public void Budget_Is90PercentOfFreeMemory()
    {
        Assert.Equal(900 * MB, BatchMemoryPlanner.Budget(1000 * MB));
    }

    [Fact]
    public void EstimateBytes_UsesDimensionsAndEncoderCost()
    {
        ulong jpg = BatchMemoryPlanner.EstimateBytes(4000, 3000, 5 * MB, ".jpg", OutputFormat.Jpg, 4);
        ulong jxl = BatchMemoryPlanner.EstimateBytes(4000, 3000, 5 * MB, ".jpg", OutputFormat.Jxl, 4);

        Assert.Equal(12_000_000UL * 4 * 2 + BatchMemoryPlanner.PerJobOverheadBytes, jpg);
        Assert.True(jxl > jpg);
    }

    [Fact]
    public void EstimateBytes_UnknownDimensions_FallsBackToFileSize()
    {
        ulong estimate = BatchMemoryPlanner.EstimateBytes(0, 0, 10 * MB, ".jpg", OutputFormat.Jpg, 4);
        Assert.True(estimate > 100 * MB);
    }

    [Fact]
    public void EstimateBytes_PossiblyAnimatedFile_AccountsForFrames()
    {
        ulong gif = BatchMemoryPlanner.EstimateBytes(500, 500, 20 * MB, ".gif", OutputFormat.Webp, 4);
        ulong png = BatchMemoryPlanner.EstimateBytes(500, 500, 20 * MB, ".png", OutputFormat.Webp, 4);
        Assert.True(gif > png * 10);
    }

    [Fact]
    public void EstimateBytes_CorruptHeaderClaimingHugeDimensions_DoesNotWrapAround()
    {
        ulong estimate = BatchMemoryPlanner.EstimateBytes(uint.MaxValue, uint.MaxValue, 1 * MB, ".png", OutputFormat.Jxl, 16);

        Assert.Equal(ulong.MaxValue, estimate);
        Assert.True(BatchMemoryPlanner.IsUnderPressure(estimate, 16 * GB));
    }

    [Fact]
    public void EstimateBytes_WhenEstimateFails_ImageRunsAloneInsteadOfStoppingTheBatch()
    {
        MediaFileInfo file = CreateMediaFiles(1)[0];

        Assert.Equal(ulong.MaxValue, BatchMemoryPlanner.EstimateBytes(file, new OutputSettings { format = (OutputFormat)999 }));
        Assert.Equal(ulong.MaxValue, BatchMemoryPlanner.EstimateBytes(file, null!));
        Assert.NotEqual(ulong.MaxValue, BatchMemoryPlanner.EstimateBytes(file, new OutputSettings()));
    }

    [Fact]
    public void Warnings_SameFileAndType_ListedOnce_UntilCleared()
    {
        var files = CreateMediaFiles(2);
        var warnings = WarningHelper.Instance;
        warnings.ClearAll();

        warnings.AddCompressionError(CompressionErrorType.FailedToCompress, files[0]);
        warnings.AddCompressionError(CompressionErrorType.FailedToCompress, files[0]); // e.g. retried after running out of memory
        warnings.AddCompressionError(CompressionErrorType.FailedToCompress, files[1]);
        warnings.AddCompressionError(CompressionErrorType.AccessDenied, files[0]);
        warnings.AddPostWarning(PostCompressionWarningType.FileFormatChanged, files[0]);
        warnings.AddPreWarning(PreCompressionWarningType.FileFormatChanged, files[0]); // same name, different warning

        Assert.Equal(2, warnings.CompressionErrors[CompressionErrorType.FailedToCompress].Count);
        Assert.Single(warnings.CompressionErrors[CompressionErrorType.AccessDenied]);
        Assert.Single(warnings.PostCompressionWarnings[PostCompressionWarningType.FileFormatChanged]);
        Assert.Single(warnings.PreCompressionWarnings[PreCompressionWarningType.FileFormatChanged]);

        warnings.ClearPreCompressionWarning();
        warnings.AddPreWarning(PreCompressionWarningType.FileFormatChanged, files[0]);
        warnings.AddPostWarning(PostCompressionWarningType.FileFormatChanged, files[0]);
        Assert.Single(warnings.PreCompressionWarnings[PreCompressionWarningType.FileFormatChanged]);
        Assert.Single(warnings.PostCompressionWarnings[PostCompressionWarningType.FileFormatChanged]);

        warnings.ClearAll();
        warnings.AddCompressionError(CompressionErrorType.FailedToCompress, files[0]);
        Assert.Single(warnings.CompressionErrors[CompressionErrorType.FailedToCompress]);
        warnings.ClearAll();
    }

    [Fact]
    public void IsUnderPressure_OnlyWhenFreeMemoryCantHoldTheImage()
    {
        Assert.False(BatchMemoryPlanner.IsUnderPressure(500 * MB, 4 * GB));
        Assert.True(BatchMemoryPlanner.IsUnderPressure(500 * MB, 600 * MB));
        Assert.False(BatchMemoryPlanner.IsUnderPressure(500 * MB, null));
    }

    [Fact]
    public void IsOutOfMemory_RecognisesMemoryErrorsOnly()
    {
        Assert.True(BatchMemoryPlanner.IsOutOfMemory(new OutOfMemoryException()));
        Assert.True(BatchMemoryPlanner.IsOutOfMemory(new MagickResourceLimitErrorException("memory allocation failed")));
        Assert.True(BatchMemoryPlanner.IsOutOfMemory(new MagickCacheErrorException("cache resources exhausted")));
        Assert.True(BatchMemoryPlanner.IsOutOfMemory(new InvalidOperationException("wrapper", new OutOfMemoryException())));
        Assert.True(BatchMemoryPlanner.IsOutOfMemory(new AggregateException(new IOException(), new OutOfMemoryException())));
        Assert.False(BatchMemoryPlanner.IsOutOfMemory(new IOException("disk")));
        Assert.False(BatchMemoryPlanner.IsOutOfMemory(new MagickCorruptImageErrorException("bad file")));
        Assert.False(BatchMemoryPlanner.IsOutOfMemory(null));
    }

    #endregion

    #region MemoryGate

    [Fact]
    public async Task Gate_EnoughMemory_StartsAsManyImagesAsCores()
    {
        var gate = new MemoryGate(3, () => 64 * GB);

        Assert.False(await Started(gate.WaitToStartAsync(100 * MB)));
        Assert.False(await Started(gate.WaitToStartAsync(100 * MB)));
        Assert.False(await Started(gate.WaitToStartAsync(100 * MB)));
        Task<bool> fourth = gate.WaitToStartAsync(100 * MB);
        Assert.False(fourth.IsCompleted);

        gate.Finished(100 * MB);
        Assert.False(await Started(fourth));
    }

    [Fact]
    public async Task Gate_StartsOnlyWhatFitsInBudget()
    {
        var gate = new MemoryGate(8, () => 1000 * MB); // budget 900 MB

        Assert.False(await Started(gate.WaitToStartAsync(400 * MB)));
        Assert.False(await Started(gate.WaitToStartAsync(400 * MB)));
        Task<bool> third = gate.WaitToStartAsync(400 * MB); // 1200 MB would not fit
        Assert.False(third.IsCompleted);

        gate.Finished(400 * MB);
        Assert.False(await Started(third));
    }

    [Fact]
    public async Task Gate_ImageBiggerThanBudget_WaitsForOthers_ThenRunsAlone()
    {
        var gate = new MemoryGate(8, () => 1000 * MB); // budget 900 MB

        Assert.False(await Started(gate.WaitToStartAsync(100 * MB)));
        Task<bool> big = gate.WaitToStartAsync(2 * GB);
        Assert.False(big.IsCompleted);

        gate.Finished(100 * MB);
        Assert.True(await Started(big));

        Task<bool> small = gate.WaitToStartAsync(10 * MB);
        Assert.False(small.IsCompleted); // nothing starts beside an image running alone

        gate.Finished(2 * GB);
        Assert.False(await Started(small));
        Assert.False(await Started(gate.WaitToStartAsync(10 * MB)));
    }

    [Fact]
    public async Task Gate_LowFreeMemoryNow_ImageWaitsForOthers_ThenRunsAlone()
    {
        ulong freeNow = 64 * GB;
        var gate = new MemoryGate(8, () => freeNow);

        Assert.False(await Started(gate.WaitToStartAsync(100 * MB)));
        freeNow = 300 * MB; // another app took memory: 100 MB more would leave less than the 256 MB reserve
        Task<bool> next = gate.WaitToStartAsync(100 * MB);
        Assert.False(next.IsCompleted);

        gate.Finished(100 * MB);
        Assert.True(await Started(next));

        Task<bool> small = gate.WaitToStartAsync(10 * MB); // fits in free memory, but one image runs alone
        Assert.False(small.IsCompleted);

        gate.Finished(100 * MB);
        Assert.False(await Started(small));
    }

    [Fact]
    public async Task Gate_UnknownFreeMemory_OnlyCoresLimit()
    {
        var gate = new MemoryGate(3, () => null);

        Assert.Equal(ulong.MaxValue, gate.BudgetBytes);
        for (int i = 0; i < 3; i++)
            Assert.False(await Started(gate.WaitToStartAsync(1024 * GB)));
        Task<bool> fourth = gate.WaitToStartAsync(1 * MB);
        Assert.False(fourth.IsCompleted);

        gate.Finished(1024 * GB);
        Assert.False(await Started(fourth));
    }

    [Fact]
    public async Task Gate_HugeEstimateFromCorruptFile_DoesNotOverflow()
    {
        var gate = new MemoryGate(8, () => 16 * GB);

        Assert.True(await Started(gate.WaitToStartAsync(ulong.MaxValue)));
        Task<bool> small = gate.WaitToStartAsync(10 * MB);
        Assert.False(small.IsCompleted);

        gate.Finished(ulong.MaxValue);
        Assert.False(await Started(small));
        Assert.False(await Started(gate.WaitToStartAsync(10 * MB)));
    }

    /// <summary>
    /// Many images with random sizes (some bigger than the budget, some too big for free memory) and random durations.
    /// Checks the rule each time an image starts, and that the batch never gets stuck.
    /// </summary>
    [Fact]
    public async Task Gate_RandomImages_NeverBreakTheRule_AndNeverGetStuck()
    {
        const int cores = 4;
        var gate = new MemoryGate(cores, () => 1000 * MB);
        var random = new Random(1);
        object check = new();
        int running = 0;
        ulong runningBytes = 0;
        bool aloneRunning = false;
        var problems = new List<string>();
        var tasks = new List<Task>();

        for (int i = 0; i < 400; i++)
        {
            ulong estimate = (ulong)random.Next(1, 1200) * MB;
            int workMs = random.Next(0, 4);
            bool alone = await Started(gate.WaitToStartAsync(estimate));
            lock (check)
            {
                running++;
                runningBytes += estimate;
                if (running > cores) problems.Add($"image {i}: {running} running on {cores} cores");
                if (running > 1 && runningBytes > gate.BudgetBytes) problems.Add($"image {i}: {runningBytes / MB} MB running, over budget");
                if (alone && running > 1) problems.Add($"image {i}: runs alone beside {running - 1} others");
                if (aloneRunning) problems.Add($"image {i}: started beside an image running alone");
                aloneRunning = alone;
            }
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(workMs);
                lock (check)
                {
                    running--;
                    runningBytes -= estimate;
                    if (alone) aloneRunning = false;
                }
                gate.Finished(estimate);
            }));
        }

        await Task.WhenAll(tasks).WaitAsync(DeadlockTimeout);
        Assert.Empty(problems);
    }

    /// <summary>
    /// Waits for the image to be let in; fails the test instead of hanging if it never is.
    /// </summary>
    private static Task<bool> Started(Task<bool> waitToStart) => waitToStart.WaitAsync(DeadlockTimeout);

    #endregion

    #region Batch behaviour

    [Fact]
    public async Task Batch_OutOfMemoryFile_IsRetriedAloneAfterOthers_AndSucceeds()
    {
        var files = CreateMediaFiles(8);
        MediaFileInfo failing = files[2];
        var compressor = new FakeCompressor
        {
            Behaviour = (file, isRetry) => file == failing && !isRetry
                ? throw new MagickResourceLimitErrorException("cache resources exhausted")
                : Task.FromResult("done")
        };
        var events = new ConcurrentBag<ImageCompressedEventArgs>();
        compressor.ImageCompressed += (_, e) => events.Add(e);
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);

        Assert.NotNull(completed);
        Assert.Equal(8, completed.TotalFiles);
        Assert.Equal(8, completed.SucceededCount);
        Assert.Equal(0, completed.FailedCount);
        Assert.DoesNotContain(events, e => !e.Success && !e.Skipped);
        Assert.False(WarningHelper.Instance.CompressionErrors.ContainsKey(CompressionErrorType.FailedToCompress));

        var attempts = compressor.Attempts.ToList();
        Assert.Equal(9, attempts.Count);
        var (lastFile, lastIsRetry) = attempts[^1];
        Assert.Same(failing, lastFile);
        Assert.True(lastIsRetry);
        Assert.Equal(1, compressor.MaxRunningDuringRetry);
    }

    [Fact]
    public async Task Batch_FileOutOfMemoryEvenAlone_IsReportedAsFailureOnce()
    {
        var files = CreateMediaFiles(4);
        MediaFileInfo failing = files[1];
        var compressor = new FakeCompressor
        {
            Behaviour = (file, _) => file == failing
                ? throw new OutOfMemoryException()
                : Task.FromResult("done")
        };
        var failures = new ConcurrentBag<ImageCompressedEventArgs>();
        compressor.ImageCompressed += (_, e) => { if (!e.Success && !e.Skipped) failures.Add(e); };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);

        Assert.NotNull(completed);
        Assert.Equal(3, completed.SucceededCount);
        Assert.Equal(1, completed.FailedCount);
        var failure = Assert.Single(failures);
        Assert.Same(failing, failure.FileInfo);
        Assert.Equal(CompressionErrorType.FailedToCompress, failure.Error);
        Assert.Single(WarningHelper.Instance.CompressionErrors[CompressionErrorType.FailedToCompress]);
        Assert.Equal(2, compressor.Attempts.Count(a => a.file == failing));
    }

    [Fact]
    public async Task Batch_OtherErrors_AreNotRetried()
    {
        var files = CreateMediaFiles(3);
        var compressor = new FakeCompressor
        {
            Behaviour = (file, _) => file == files[0] ? throw new IOException("corrupt") : Task.FromResult("done")
        };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);

        Assert.Equal(1, completed!.FailedCount);
        Assert.Single(compressor.Attempts, a => a.file == files[0]);
        Assert.DoesNotContain(compressor.Attempts, a => a.isRetry);
    }

    /// <summary>
    /// 100,000 images, among them broken files, files that run out of memory once, and files whose corrupt header claims huge dimensions
    /// (each of those runs alone, emptying the pipeline first). Nothing may get stuck, and every image must be counted exactly once.
    /// </summary>
    [Fact]
    public async Task Batch_100KImages_WithBrokenAndHugeFiles_CompletesAndCountsEachImageOnce()
    {
        const int count = 100_000;
        var distinct = CreateMediaFiles(20);
        MediaFileInfo broken = distinct[1], outOfMemoryOnce = distinct[2], hugeHeader = distinct[3];
        var files = Enumerable.Range(0, count).Select(i => distinct[i % distinct.Count]).ToList();
        var compressor = new FakeCompressor
        {
            Cores = 8,
            WorkDelay = TimeSpan.Zero,
            Estimate = f => f == hugeHeader ? ulong.MaxValue : 50 * MB,
            AvailableMemory = () => 2 * GB,
            Behaviour = (file, isRetry) =>
                file == broken ? throw new MagickCorruptImageErrorException("broken")
                : file == outOfMemoryOnce && !isRetry ? throw new MagickCacheErrorException("cache resources exhausted")
                : Task.FromResult("done")
        };
        int failureEvents = 0;
        compressor.ImageCompressed += (_, e) => { if (!e.Success && !e.Skipped) Interlocked.Increment(ref failureEvents); };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false).WaitAsync(TimeSpan.FromMinutes(2));

        int perFile = count / distinct.Count;
        Assert.NotNull(completed);
        Assert.Equal(count, completed.TotalFiles);
        Assert.Equal(perFile, completed.FailedCount);
        Assert.Equal(count - perFile, completed.SucceededCount);
        Assert.Equal(perFile, failureEvents);
        Assert.Equal(count + perFile, compressor.Attempts.Count); // each out-of-memory image is retried once
        Assert.Equal(perFile, compressor.Attempts.Count(a => a.isRetry));
        Assert.InRange(compressor.MaxRunning, 1, 8);
    }

    [Fact]
    public async Task Batch_LowMemory_RunsOnlyWhatFitsInBudget()
    {
        var files = CreateMediaFiles(12);
        var compressor = new FakeCompressor
        {
            Cores = 8,
            Estimate = _ => 1 * GB,
            AvailableMemory = () => 3500 * MB, // 90% budget holds 3 images of 1 GB
            WorkDelay = TimeSpan.FromMilliseconds(30)
        };

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);

        Assert.Equal(3, compressor.MaxRunning);
    }

    [Fact]
    public async Task Batch_EnoughMemory_KeepsFullParallelism()
    {
        var files = CreateMediaFiles(16);
        var compressor = new FakeCompressor
        {
            Cores = 4,
            AvailableMemory = () => 64 * GB,
            WorkDelay = TimeSpan.FromMilliseconds(50)
        };

        await compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);

        Assert.Equal(4, compressor.MaxRunning);
    }

    [Fact]
    public async Task Batch_MemoryShortWhenImageStarts_ImageRunsAlone()
    {
        var files = CreateMediaFiles(20);
        MediaFileInfo big = files[6];
        int memoryReads = 0;
        var compressor = new FakeCompressor
        {
            Cores = 4,
            Estimate = f => f == big ? 2 * GB : 100 * MB,
            // Plenty of memory when the batch starts, then memory gets tight: only the big image can't fit in what is free.
            AvailableMemory = () => Interlocked.Increment(ref memoryReads) == 1 ? 64 * GB : 1 * GB,
            WorkDelay = TimeSpan.FromMilliseconds(30),
            Watched = big
        };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        Task run = compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);
        Assert.Same(run, await Task.WhenAny(run, Task.Delay(DeadlockTimeout)));

        Assert.Equal(20, completed!.SucceededCount);
        Assert.Equal(4, compressor.MaxRunning);
        Assert.Equal(1, compressor.RunningWhenWatchedStarted);
        Assert.Equal(0, compressor.StartedDuringWatched);
    }

    [Fact]
    public async Task Batch_ImageBiggerThanBudget_RunsAlone_OthersKeepParallelism()
    {
        var files = CreateMediaFiles(30);
        MediaFileInfo big = files[10];
        var compressor = new FakeCompressor
        {
            Cores = 8,
            // Budget is 90% of 4 GB = 3686 MB: the 3700 MB image doesn't fit, the 500 MB images fit seven at a time.
            // Free memory stays at 4 GB, enough for the big image alone, so it is the budget that makes it run alone.
            Estimate = f => f == big ? 3700 * MB : 500 * MB,
            AvailableMemory = () => 4 * GB,
            WorkDelay = TimeSpan.FromMilliseconds(30),
            Watched = big
        };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        Task run = compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);
        Assert.Same(run, await Task.WhenAny(run, Task.Delay(DeadlockTimeout)));

        Assert.False(BatchMemoryPlanner.IsUnderPressure(3700 * MB, 4 * GB));
        Assert.Equal(30, completed!.SucceededCount);
        Assert.Equal(7, compressor.MaxRunning);
        Assert.Equal(1, compressor.RunningWhenWatchedStarted);
        Assert.Equal(0, compressor.StartedDuringWatched);
    }

    [Fact]
    public async Task Batch_BigImageThatFits_SharesBudgetWithSmallerImages()
    {
        var files = CreateMediaFiles(30);
        MediaFileInfo big = files[10];
        var compressor = new FakeCompressor
        {
            Cores = 8,
            // Budget is 90% of 4 GB = 3686 MB: the 2 GB image fits beside three 500 MB images, not beside four.
            Estimate = f => f == big ? 2 * GB : 500 * MB,
            AvailableMemory = () => 4 * GB,
            WorkDelay = TimeSpan.FromMilliseconds(30),
            Watched = big
        };
        CompressionCompletedEventArgs? completed = null;
        compressor.CompressionCompleted += (_, e) => completed = e;

        Task run = compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);
        Assert.Same(run, await Task.WhenAny(run, Task.Delay(DeadlockTimeout)));

        Assert.Equal(30, completed!.SucceededCount);
        Assert.Equal(7, compressor.MaxRunning);
        Assert.True(compressor.RunningWhenWatchedStarted > 1 || compressor.StartedDuringWatched > 0, "The big image ran alone.");
        Assert.InRange(compressor.MaxRunningEstimateBytes, 2 * GB, BatchMemoryPlanner.Budget(4 * GB));
    }

    [Fact]
    public async Task Batch_StartedFromUiThread_ReturnsImmediately_AndPlansOffThatThread()
    {
        var files = CreateMediaFiles(10);
        var estimateThreads = new ConcurrentBag<int>();
        var compressor = new FakeCompressor
        {
            // Slow planning: 10 files x 50 ms. The caller must not wait for it.
            Estimate = _ =>
            {
                estimateThreads.Add(Environment.CurrentManagedThreadId);
                Thread.Sleep(50);
                return 10 * MB;
            }
        };

        // A dedicated thread stands in for the UI thread: work queued with Task.Run can never run on it.
        Task? run = null;
        int callerThread = 0;
        TimeSpan callDuration = default;
        var caller = new Thread(() =>
        {
            callerThread = Environment.CurrentManagedThreadId;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            run = compressor.CompressImagesAsync(files, false, new OutputSettings(), forPreview: false);
            callDuration = stopwatch.Elapsed;
        });
        caller.Start();
        caller.Join();

        Assert.True(callDuration < TimeSpan.FromMilliseconds(250), $"Caller was blocked for {callDuration.TotalMilliseconds} ms.");
        Assert.Same(run, await Task.WhenAny(run!, Task.Delay(DeadlockTimeout)));
        Assert.Equal(10, estimateThreads.Count);
        Assert.DoesNotContain(callerThread, estimateThreads);
    }

    [Fact]
    public async Task Batch_RetryOverwritesPartialOutput_EvenWithSkipIfFileExists()
    {
        string input = CreatePng("retry.png", 64, 48);
        var media = await CreateMediaFileInfoAsync(input, 64, 48);
        var settings = new OutputSettings
        {
            format = OutputFormat.Png,
            copyMetadata = false,
            retainDateTime = false,
            dimensionStrategy = DimensionReductionStrategy.KeepSame,
            OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
            OutputFolder = Path.Combine(_testRoot, "out"),
            SkipIfFileExists = true
        };
        string outputPath = media.GetOutputPath(settings, false, false);
        byte[] partialOutput = [1, 2, 3, 4];

        var compressor = new PartialWriteThenRealCompressor(outputPath, partialOutput);
        ImageCompressedEventArgs? result = null;
        compressor.ImageCompressed += (_, e) => result = e;

        await compressor.CompressImagesAsync([media], false, settings, forPreview: false);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(result.Skipped);
        Assert.NotEqual(partialOutput, File.ReadAllBytes(outputPath));
        var info = new MagickImageInfo(outputPath);
        Assert.Equal(64u, info.Width);
        Assert.Equal(48u, info.Height);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Replaces the real pipeline with a delay and records attempts and concurrency.
    /// </summary>
    private sealed class FakeCompressor : ImageCompressor
    {
        public int Cores { get; init; } = 4;
        public Func<MediaFileInfo, ulong> Estimate { get; init; } = _ => 10 * MB;
        public Func<ulong?> AvailableMemory { get; init; } = () => 64 * GB;
        public Func<MediaFileInfo, bool, Task<string>> Behaviour { get; init; } = (_, _) => Task.FromResult("done");
        public TimeSpan WorkDelay { get; init; } = TimeSpan.FromMilliseconds(5);
        /// <summary>Runs 4 times longer than other images; records what runs beside it.</summary>
        public MediaFileInfo? Watched { get; init; }

        public ConcurrentQueue<(MediaFileInfo file, bool isRetry)> Attempts { get; } = new();
        private int _running;
        private int _maxRunning;
        private long _runningEstimate;
        private long _maxRunningEstimate;
        private int _watchedRunning;
        private int _startedDuringWatched;
        public int MaxRunning => _maxRunning;
        /// <summary>Most memory, by the estimates, used by images running at the same time.</summary>
        public ulong MaxRunningEstimateBytes => (ulong)Interlocked.Read(ref _maxRunningEstimate);
        public int MaxRunningDuringRetry { get; private set; }
        public int RunningWhenWatchedStarted { get; private set; }
        public int StartedDuringWatched => _startedDuringWatched;

        /// <summary>Estimates the batch asked for, so they are counted without calling <see cref="Estimate"/> again.</summary>
        private readonly ConcurrentDictionary<MediaFileInfo, ulong> _estimates = new();

        protected override int ProcessorCount => Cores;
        protected override ulong? GetAvailableMemoryBytes() => AvailableMemory();
        protected override ulong EstimateMemoryBytes(MediaFileInfo file, OutputSettings settings)
            => _estimates[file] = Estimate(file);

        protected override async Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview, bool isRetry)
        {
            Attempts.Enqueue((mediaInfo, isRetry));
            int now = Interlocked.Increment(ref _running);
            InterlockedMax(ref _maxRunning, now);
            long estimate = (long)_estimates.GetValueOrDefault(mediaInfo);
            InterlockedMax(ref _maxRunningEstimate, Interlocked.Add(ref _runningEstimate, estimate));
            if (isRetry)
                MaxRunningDuringRetry = Math.Max(MaxRunningDuringRetry, now);
            bool watched = mediaInfo == Watched;
            if (watched)
            {
                RunningWhenWatchedStarted = now;
                Interlocked.Exchange(ref _watchedRunning, 1);
            }
            else if (Volatile.Read(ref _watchedRunning) == 1)
                Interlocked.Increment(ref _startedDuringWatched);

            try
            {
                await Task.Delay(watched ? WorkDelay * 4 : WorkDelay);
                return await Behaviour(mediaInfo, isRetry);
            }
            finally
            {
                if (watched)
                    Interlocked.Exchange(ref _watchedRunning, 0);
                Interlocked.Add(ref _runningEstimate, -estimate);
                Interlocked.Decrement(ref _running);
            }
        }
    }

    /// <summary>
    /// First attempt leaves a partial output file and runs out of memory; the retry uses the real pipeline.
    /// </summary>
    private sealed class PartialWriteThenRealCompressor(string outputPath, byte[] partialOutput) : ImageCompressor
    {
        protected override Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview, bool isRetry)
        {
            if (isRetry)
                return base.ProcessSingleFileAsync(mediaInfo, multipleSelectPaths, settings, forPreview, isRetry);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, partialOutput);
            throw new MagickResourceLimitErrorException("cache resources exhausted");
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target))
               && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    private static void InterlockedMax(ref long target, long value)
    {
        long current;
        while (value > (current = Interlocked.Read(ref target))
               && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    private List<MediaFileInfo> CreateMediaFiles(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new MediaFileInfo(_testRoot, new FileInfo(CreatePng($"img{i}.png", 8, 8))))
            .ToList();
    }

    private string CreatePng(string fileName, uint width, uint height)
    {
        string path = Path.Combine(_testRoot, fileName);
        using var image = new MagickImage(new MagickColor("#3a7bd5"), width, height);
        image.Draw(new ImageMagick.Drawing.Drawables().FillColor(MagickColors.Orange).Circle(width / 2.0, height / 2.0, width / 2.0, height / 4.0));
        image.Write(path, MagickFormat.Png);
        return path;
    }

    private static async Task<MediaFileInfo> CreateMediaFileInfoAsync(string inputPath, uint width, uint height)
    {
        var media = new MediaFileInfo(Path.GetDirectoryName(inputPath)!, new FileInfo(inputPath));
        await TestFileHelper.WaitUntil(() => media.Width == width && media.Height == height, TimeSpan.FromSeconds(5));
        return media;
    }

    public void Dispose()
    {
        try { Directory.Delete(_testRoot, recursive: true); } catch { }
    }

    #endregion
}
