using miCompressor.core;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace miCompressor.IntegrationTests;

/// <summary>
/// Holds physical memory filled with random data (so Windows can't compress it away) until disposed.
/// </summary>
internal sealed class RamBallast : IDisposable
{
    private const int ChunkBytes = 64 * 1024 * 1024;
    private readonly List<ulong[]> _chunks = [];
    public ulong Bytes { get; }

    public RamBallast(ulong bytes)
    {
        ulong state = 0x9E3779B97F4A7C15;
        for (ulong held = 0; held + ChunkBytes <= bytes; held += ChunkBytes)
        {
            var chunk = new ulong[ChunkBytes / sizeof(ulong)];
            for (int i = 0; i < chunk.Length; i++)
            {
                state ^= state << 13; state ^= state >> 7; state ^= state << 17;
                chunk[i] = state;
            }
            _chunks.Add(chunk);
            Bytes = held + ChunkBytes;
        }
    }

    public void Dispose()
    {
        _chunks.Clear();
        GC.Collect();
    }
}

/// <summary>
/// The real compressor; only records attempts, errors, memory readings and how many images run at once.
/// </summary>
internal sealed class RecordingCompressor : ImageCompressor
{
    public int? Cores { get; init; }
    /// <summary>Reports free memory as unknown, so only the image limit applies: used to measure real memory use.</summary>
    public bool DisableMemoryPlanning { get; init; }
    public ConcurrentQueue<(MediaFileInfo file, bool isRetry)> Attempts { get; } = new();
    public ConcurrentQueue<string> Errors { get; } = new();
    public ConcurrentQueue<(MediaFileInfo file, bool isRetry, long start, long end)> Runs { get; } = new();
    private int _running;
    private int _maxRunning;
    public int MaxRunning => _maxRunning;
    private readonly object _sharedLock = new();
    private readonly List<ulong> _runningEstimates = [];
    /// <summary>Most memory, by the estimates, used by images running at the same time (only counted while more than one runs).</summary>
    public ulong MaxSharedEstimateBytes { get; private set; }
    /// <summary>Free memory readings: the first plans the batch, then one before each image starts.</summary>
    public ConcurrentQueue<ulong?> MemoryReadings { get; } = new();

    protected override int MaxImagesAtOnce => Cores ?? base.MaxImagesAtOnce;

    protected override ulong? GetAvailableMemoryBytes()
    {
        ulong? available = base.GetAvailableMemoryBytes();
        MemoryReadings.Enqueue(available);
        return DisableMemoryPlanning ? null : available;
    }

    /// <summary>
    /// Clears what was recorded, so the same compressor can be reused for another batch, as the app does.
    /// </summary>
    public void ResetRecording()
    {
        Attempts.Clear();
        Errors.Clear();
        Runs.Clear();
        MemoryReadings.Clear();
        _maxRunning = 0;
        MaxSharedEstimateBytes = 0;
    }

    protected override async Task<string> ProcessSingleFileAsync(MediaFileInfo mediaInfo, bool multipleSelectPaths, OutputSettings settings, bool forPreview, bool isRetry)
    {
        Attempts.Enqueue((mediaInfo, isRetry));
        long start = Stopwatch.GetTimestamp();
        int now = Interlocked.Increment(ref _running);
        int current;
        while (now > (current = Volatile.Read(ref _maxRunning)) && Interlocked.CompareExchange(ref _maxRunning, now, current) != current) { }
        ulong estimate = EstimateMemoryBytes(mediaInfo, settings);
        lock (_sharedLock)
        {
            _runningEstimates.Add(estimate);
            if (_runningEstimates.Count > 1)
                MaxSharedEstimateBytes = Math.Max(MaxSharedEstimateBytes, _runningEstimates.Aggregate(0UL, (a, b) => a + b));
        }
        try
        {
            return await base.ProcessSingleFileAsync(mediaInfo, multipleSelectPaths, settings, forPreview, isRetry);
        }
        catch (Exception ex)
        {
            Errors.Enqueue($"{mediaInfo.ShortName} retry={isRetry} running={Volatile.Read(ref _running)}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
        finally
        {
            Runs.Enqueue((mediaInfo, isRetry, start, Stopwatch.GetTimestamp()));
            lock (_sharedLock)
                _runningEstimates.Remove(estimate);
            Interlocked.Decrement(ref _running);
        }
    }
}
