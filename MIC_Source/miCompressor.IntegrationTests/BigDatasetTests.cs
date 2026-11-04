using ImageMagick;
using ImageMagick.Drawing;
using miCompressor.core;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;

namespace miCompressor.IntegrationTests;

/// <summary>
/// Compresses a big, mixed data set several times in a row with the same compressor, as the app does, and checks every run:
/// all valid images succeed, outputs decode with the right size, corrupt files get exactly one result and don't hold up the batch,
/// nothing extra is left on disk, and memory/handles don't keep growing.
/// <para>
/// Opt-in as it takes minutes and about 3 GB of disk: set MIC_BIG_DATASET=1. Optional:
/// MIC_BIG_DATASET_RUNS (default 6), MIC_BIG_DATASET_FORMATS (default "Jpg,Webp,Jxl", used in turn),
/// MIC_BIG_DATASET_FREE_MB (hold RAM until only this much is free), MIC_BIG_DATASET_MAGICK_MB (limit Magick memory, no disk fallback, to force retries).
/// The data set is generated once into %TEMP%\miCompressor-big-dataset and reused.
/// </para>
/// </summary>
[Collection(RealMemoryCollection.Name)]
public class BigDatasetTests(ITestOutputHelper output)
{
    private const ulong MB = 1024 * 1024;
    private static readonly string DatasetDir = Path.Combine(Path.GetTempPath(), "miCompressor-big-dataset", "v2");
    private static readonly TimeSpan RunTimeout = TimeSpan.FromMinutes(90); // PNG output of the whole set takes about an hour: oxipng is slow on large photos
    private const string CorruptPrefix = "corrupt-";

    private sealed record Spec(string Name, int Count, uint Width, uint Height, MagickFormat Format, string Extension, bool Alpha = false, int Frames = 1);

    private static readonly Spec[] DatasetSpec =
    [
        new("photo-3mp", 200, 2048, 1536, MagickFormat.Jpeg, ".jpg"),
        new("photo-12mp", 80, 4000, 3000, MagickFormat.Jpeg, ".jpg"),
        new("photo-24mp", 24, 6000, 4000, MagickFormat.Jpeg, ".jpg"),
        new("photo-80mp", 4, 10000, 8000, MagickFormat.Jpeg, ".jpg"),
        new("alpha", 16, 1600, 1200, MagickFormat.Png, ".png", Alpha: true),
        new("webp", 16, 2048, 1536, MagickFormat.WebP, ".webp"),
        new("anim", 10, 480, 360, MagickFormat.Gif, ".gif", Frames: 24),
    ];

    private static bool IsCorrupt(MediaFileInfo file) => file.ShortName.StartsWith(CorruptPrefix, StringComparison.Ordinal);

    [BigDatasetFact]
    public async Task BigDataset_RepeatedBatches_AllSucceed_WithoutLeaks()
    {
        int runs = int.TryParse(Environment.GetEnvironmentVariable("MIC_BIG_DATASET_RUNS"), out int r) ? r : 6;
        OutputFormat[] formats = (Environment.GetEnvironmentVariable("MIC_BIG_DATASET_FORMATS") ?? "Jpg,Webp,Jxl")
            .Split(',').Select(f => Enum.Parse<OutputFormat>(f.Trim(), ignoreCase: true)).ToArray();
        ulong? leaveFreeMb = ulong.TryParse(Environment.GetEnvironmentVariable("MIC_BIG_DATASET_FREE_MB"), out ulong free) ? free : null;
        ulong? magickMb = ulong.TryParse(Environment.GetEnvironmentVariable("MIC_BIG_DATASET_MAGICK_MB"), out ulong magick) ? magick : null;
        bool noPlan = Environment.GetEnvironmentVariable("MIC_BIG_DATASET_NO_PLAN") == "1";

        var stopwatch = Stopwatch.StartNew();
        EnsureDataset();
        List<MediaFileInfo> files = await LoadDatasetAsync();
        List<MediaFileInfo> corrupt = files.Where(IsCorrupt).ToList();
        output.WriteLine($"Data set: {files.Count} files ({corrupt.Count} corrupt), {files.Sum(f => (long)f.FileSize) / (long)MB} MB, " +
            $"{files.Where(f => !IsCorrupt(f)).Sum(f => (double)f.Width * f.Height) / 1e6:F0} MP, ready in {stopwatch.Elapsed.TotalSeconds:F0}s");
        output.WriteLine("Corrupt files as the app sees them: " + string.Join(", ", corrupt.Select(f => $"{f.ShortName} {f.Width}x{f.Height} {f.FileSize / 1024} KB")));
        output.WriteLine($"Cores {Environment.ProcessorCount}, runs {runs}, formats {string.Join(",", formats)}, " +
            $"free RAM target {(leaveFreeMb?.ToString() ?? "-")} MB, Magick limit {(magickMb?.ToString() ?? "80% (as the app)")} MB" +
            (noPlan ? ", MEMORY PLANNING OFF (measuring)" : ""));

        ulong originalMemoryLimit = ResourceLimits.Memory, originalDiskLimit = ResourceLimits.Disk;
        var problems = new List<string>();
        var privateMbAfterRun = new List<long>();
        var handlesAfterRun = new List<int>();
        var compressor = new RecordingCompressor { DisableMemoryPlanning = noPlan }; // one instance for all runs, as the app does
        try
        {
            if (magickMb is ulong limit)
            {
                ResourceLimits.Memory = limit * MB;
                ResourceLimits.Disk = 0;
            }
            else
            {
                ResourceLimits.LimitMemory(new Percentage(80)); // what App.xaml.cs sets
            }

            ulong availableNow = SystemMemory.GetAvailableBytes() ?? 0;
            using var ballast = leaveFreeMb is ulong leave && availableNow > leave * MB ? new RamBallast(availableNow - leave * MB) : null;
            if (ballast != null)
                output.WriteLine($"Holding {ballast.Bytes / MB} MB of RAM, free now {SystemMemory.GetAvailableBytes() / MB} MB");

            for (int run = 1; run <= runs; run++)
            {
                OutputFormat format = formats[(run - 1) % formats.Length];
                string outDir = Path.Combine(Path.GetTempPath(), "miCompressor-big-dataset-out", Guid.NewGuid().ToString("N"));
                var settings = new OutputSettings
                {
                    format = format,
                    quality = 80,
                    copyMetadata = false,
                    retainDateTime = false,
                    dimensionStrategy = DimensionReductionStrategy.KeepSame,
                    OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
                    OutputFolder = outDir
                };

                compressor.ResetRecording();
                var events = new ConcurrentBag<ImageCompressedEventArgs>();
                CompressionCompletedEventArgs? completed = null;
                void OnImage(object? _, ImageCompressedEventArgs e) => events.Add(e);
                void OnCompleted(object? _, CompressionCompletedEventArgs e) => completed = e;
                compressor.ImageCompressed += OnImage;
                compressor.CompressionCompleted += OnCompleted;
                int magickTempBefore = CountMagickTempFiles();

                var runWatch = Stopwatch.StartNew();
                using var sampling = new CancellationTokenSource();
                Task<long> peakPrivate = SamplePeakPrivateBytesAsync(sampling.Token);
                Task batch = compressor.CompressImagesAsync(files, false, settings, forPreview: false);
                bool finished = await Task.WhenAny(batch, Task.Delay(RunTimeout)) == batch;
                runWatch.Stop();
                sampling.Cancel();
                long peakPrivateMb = await peakPrivate / (long)MB - (long)((ballast?.Bytes ?? 0) / MB);
                compressor.ImageCompressed -= OnImage;
                compressor.CompressionCompleted -= OnCompleted;

                string tag = $"run {run} ({format})";
                if (!finished)
                {
                    problems.Add($"{tag}: did not finish within {RunTimeout.TotalMinutes} min (hang?)");
                    break;
                }
                if (batch.IsFaulted)
                    problems.Add($"{tag}: batch task faulted: {batch.Exception}");

                // Only corrupt files may fail, each reported once as 'failed to compress'.
                var failed = events.Where(e => !e.Success).Select(e => e.FileInfo).ToList();
                if (completed == null)
                    problems.Add($"{tag}: CompressionCompleted was not raised");
                else if (completed.TotalFiles != files.Count || completed.FailedCount != failed.Count || completed.SucceededCount != files.Count - failed.Count)
                    problems.Add($"{tag}: {completed.SucceededCount} succeeded, {completed.FailedCount} failed of {completed.TotalFiles}; {failed.Count} failure events, {files.Count} files");
                foreach (string error in compressor.Errors.Where(e => e.Contains("retry=True") && !e.StartsWith(CorruptPrefix, StringComparison.Ordinal)))
                    problems.Add($"{tag}: failed again on retry: {error.Split('\n')[0]}");
                var listedErrors = WarningHelper.Instance.CompressionErrors;
                if (listedErrors.Keys.Any(k => k != CompressionErrorType.FailedToCompress)
                    || listedErrors.Values.SelectMany(v => v).Any(f => !IsCorrupt(f))
                    || listedErrors.Values.Sum(v => v.Count) != failed.Count)
                    problems.Add($"{tag}: errors listed for user: {string.Join(", ", listedErrors.Select(e => $"{e.Key}={string.Join("/", e.Value.Select(f => f.ShortName))}"))}");

                VerifyOutputs(tag, files, events, outDir, problems);

                // How each corrupt file ended, how long it took, and how many images ran beside it.
                var allRuns = compressor.Runs.ToList();
                output.WriteLine($"{tag} corrupt files: " + string.Join(", ", corrupt.Select(f =>
                {
                    var outcome = events.FirstOrDefault(e => e.FileInfo == f);
                    var fileRuns = allRuns.Where(r => r.file == f).ToList();
                    int beside = fileRuns.Sum(fr => allRuns.Count(r => r.file != f && r.start < fr.end && r.end > fr.start));
                    double seconds = fileRuns.Sum(fr => Stopwatch.GetElapsedTime(fr.start, fr.end).TotalSeconds);
                    return $"{f.ShortName} {(outcome == null ? "NO EVENT" : outcome.Success ? "ok" : outcome.Error.ToString())} " +
                        $"{fileRuns.Count}x {seconds:F1}s beside {beside}";
                })));

                int magickTempLeft = CountMagickTempFiles() - magickTempBefore;
                if (magickTempLeft > 0)
                    problems.Add($"{tag}: {magickTempLeft} Magick temp file(s) left behind");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                using var process = Process.GetCurrentProcess();
                long privateMb = process.PrivateMemorySize64 / (long)MB - (long)((ballast?.Bytes ?? 0) / MB);
                privateMbAfterRun.Add(privateMb);
                handlesAfterRun.Add(process.HandleCount);

                ulong?[] readings = [.. compressor.MemoryReadings];
                // The first reading sets the batch budget.
                string budgetMb = !compressor.DisableMemoryPlanning && readings.FirstOrDefault() is ulong startFree ? $"{BatchMemoryPlanner.Budget(startFree) / MB}" : "none";
                output.WriteLine($"{tag}: {runWatch.Elapsed.TotalSeconds:F1}s, max parallel {compressor.MaxRunning}, " +
                    $"retries {compressor.Attempts.Count(a => a.isRetry)}, errors on first attempt (corrupt files included) {compressor.Errors.Count(e => e.Contains("retry=False"))}, " +
                    $"lowest free RAM {readings.Where(v => v.HasValue).DefaultIfEmpty(0UL).Min() / MB} MB, budget {budgetMb} MB, " +
                    $"peak private {peakPrivateMb} MB vs most estimated running together {compressor.MaxSharedEstimateBytes / MB} MB, " +
                    $"output {DirectorySizeMb(outDir)} MB; after GC: private {privateMb} MB (excl. held RAM), handles {process.HandleCount}, threads {process.Threads.Count}");

                try { Directory.Delete(outDir, recursive: true); } catch (Exception ex) { problems.Add($"{tag}: can't delete output: {ex.Message}"); }
            }
        }
        finally
        {
            ResourceLimits.Memory = originalMemoryLimit;
            ResourceLimits.Disk = originalDiskLimit;
        }

        // Leaks: after the first round of formats everything is warmed up; later rounds must not keep growing.
        if (privateMbAfterRun.Count >= 2 * formats.Length)
        {
            int warm = formats.Length - 1;
            long memoryGrowth = privateMbAfterRun[^1] - privateMbAfterRun[warm];
            int handleGrowth = handlesAfterRun[^1] - handlesAfterRun[warm];
            output.WriteLine($"Growth after warm-up: private memory {memoryGrowth} MB, handles {handleGrowth}");
            if (memoryGrowth > 256)
                problems.Add($"Private memory grew {memoryGrowth} MB after warm-up: {string.Join(", ", privateMbAfterRun)} MB");
            if (handleGrowth > 300)
                problems.Add($"Handles grew by {handleGrowth} after warm-up: {string.Join(", ", handlesAfterRun)}");
        }

        foreach (string problem in problems)
            output.WriteLine("PROBLEM: " + problem);
        Assert.Empty(problems);
    }

    private static void VerifyOutputs(string tag, List<MediaFileInfo> files, ConcurrentBag<ImageCompressedEventArgs> events, string outDir, List<string> problems)
    {
        var byFile = events.GroupBy(e => e.FileInfo).ToDictionary(g => g.Key, g => g.ToList());
        foreach (MediaFileInfo file in files)
        {
            if (!byFile.TryGetValue(file, out var fileEvents))
            {
                problems.Add($"{tag}: no event for {file.ShortName}");
                continue;
            }
            if (fileEvents.Count != 1)
                problems.Add($"{tag}: {fileEvents.Count} events for {file.ShortName}");

            ImageCompressedEventArgs e = fileEvents[0];
            if (!e.Success)
            {
                // A corrupt file may fail, but only as 'failed to compress'. Some decoders recover what they can, so it may also succeed.
                if (!IsCorrupt(file) || e.Error != CompressionErrorType.FailedToCompress || e.Skipped)
                    problems.Add($"{tag}: {file.ShortName} not successful (error {e.Error}, skipped {e.Skipped})");
                continue;
            }
            try
            {
                var info = new MagickImageInfo(e.OutputPath!);
                if (!IsCorrupt(file) && (info.Width != file.Width || info.Height != file.Height))
                    problems.Add($"{tag}: {file.ShortName} output is {info.Width}x{info.Height}, expected {file.Width}x{file.Height}");
            }
            catch (Exception ex)
            {
                problems.Add($"{tag}: {file.ShortName} output unreadable: {ex.Message}");
            }
        }

        // Exactly one output per successful image: failed images leave nothing behind.
        int succeeded = events.Count(e => e.Success);
        string[] outputs = Directory.Exists(outDir) ? Directory.GetFiles(outDir, "*", SearchOption.AllDirectories) : [];
        if (outputs.Length != succeeded)
            problems.Add($"{tag}: {outputs.Length} files in output folder, expected {succeeded}: " +
                string.Join(", ", outputs.Select(Path.GetFileName).Where(n => !events.Any(e => e.Success && Path.GetFileNameWithoutExtension(e.FileInfo.ShortName) == Path.GetFileNameWithoutExtension(n))).Take(10)));
    }

    private static async Task<long> SamplePeakPrivateBytesAsync(CancellationToken token)
    {
        using var process = Process.GetCurrentProcess();
        long peak = 0;
        while (!token.IsCancellationRequested)
        {
            process.Refresh();
            peak = Math.Max(peak, process.PrivateMemorySize64);
            try { await Task.Delay(50, token); } catch (OperationCanceledException) { }
        }
        return peak;
    }

    private static int CountMagickTempFiles()
    {
        try { return Directory.GetFiles(Path.GetTempPath(), "magick-*").Length; } catch { return 0; }
    }

    private static long DirectorySizeMb(string dir)
    {
        return Directory.Exists(dir) ? Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) / (long)MB : 0;
    }

    private static async Task<List<MediaFileInfo>> LoadDatasetAsync()
    {
        var files = Directory.GetFiles(DatasetDir)
            .Where(f => !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .Select(f => new MediaFileInfo(DatasetDir, new FileInfo(f)))
            .ToList();
        await TestFileHelper.WaitUntil(() => files.Where(f => !IsCorrupt(f)).All(f => f.Width > 0 && f.Height > 0), TimeSpan.FromMinutes(3));
        return files;
    }

    /// <summary>
    /// Broken files as users have them: random bytes, empty files, files cut short by an interrupted copy,
    /// and PNGs whose header claims 30000x20000 pixels (600 MP) but which hold only a 64x64 image.
    /// </summary>
    private static void CreateCorruptFiles()
    {
        var random = new Random(7);
        for (int i = 1; i <= 3; i++)
        {
            byte[] garbage = new byte[200 * 1024];
            random.NextBytes(garbage);
            File.WriteAllBytes(Path.Combine(DatasetDir, $"{CorruptPrefix}garbage-{i}.jpg"), garbage);
        }

        File.WriteAllBytes(Path.Combine(DatasetDir, $"{CorruptPrefix}empty-1.png"), []);
        File.WriteAllBytes(Path.Combine(DatasetDir, $"{CorruptPrefix}empty-2.jpg"), []);

        foreach (var (source, name) in new[] { ("photo-12mp-001.jpg", "truncated-1.jpg"), ("alpha-001.png", "truncated-2.png"), ("anim-001.gif", "truncated-3.gif") })
        {
            byte[] whole = File.ReadAllBytes(Path.Combine(DatasetDir, source));
            File.WriteAllBytes(Path.Combine(DatasetDir, CorruptPrefix + name), whole[..(whole.Length * 2 / 5)]);
        }

        using var small = new MagickImage(MagickColors.SteelBlue, 64, 64);
        byte[] png = small.ToByteArray(MagickFormat.Png);
        // IHDR is the first chunk: width at byte 16, height at byte 20 (big-endian), its CRC (over type and data) at byte 29.
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 30000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 20000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(29), Crc32(png.AsSpan(12, 17)));
        for (int i = 1; i <= 2; i++)
            File.WriteAllBytes(Path.Combine(DatasetDir, $"{CorruptPrefix}hugeheader-{i}.png"), png);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }

    /// <summary>
    /// Photo-like content (gradient, shapes, sensor-like noise) so encoders do realistic work. Generated once, reused afterwards.
    /// </summary>
    private static void EnsureDataset()
    {
        string marker = Path.Combine(DatasetDir, "complete.txt");
        if (File.Exists(marker))
            return;
        if (Directory.Exists(DatasetDir))
            Directory.Delete(DatasetDir, recursive: true);
        Directory.CreateDirectory(DatasetDir);

        using var source = new MagickImage("gradient:#203a6b-#e8b04a", new MagickReadSettings { Width = 10000, Height = 8000 });
        var random = new Random(42);
        var shapes = new Drawables();
        for (int i = 0; i < 400; i++)
        {
            var color = new MagickColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            int x = random.Next(10000), y = random.Next(8000), size = random.Next(40, 900);
            shapes.FillColor(color);
            if (i % 2 == 0)
                shapes.Circle(x, y, x + size, y);
            else
                shapes.Rectangle(x, y, x + size, y + size / 2);
        }
        shapes.Draw(source);
        source.AddNoise(NoiseType.Gaussian, 0.3);

        var jobs = DatasetSpec.SelectMany(spec => Enumerable.Range(1, spec.Count).Select(i => (spec, i))).ToList();
        object sourceLock = new();
        Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = 4 }, job =>
        {
            var (spec, index) = job;
            var rnd = new Random(HashCode.Combine(spec.Name, index));
            string path = Path.Combine(DatasetDir, $"{spec.Name}-{index:000}{spec.Extension}");

            MagickImage Crop(uint width, uint height)
            {
                int x = rnd.Next((int)(source.Width - width + 1)), y = rnd.Next((int)(source.Height - height + 1));
                lock (sourceLock)
                {
                    var area = (MagickImage)source.CloneArea(x, y, width, height);
                    area.ResetPage();
                    return area;
                }
            }

            if (spec.Frames > 1)
            {
                using var frames = new MagickImageCollection();
                int x = rnd.Next(2000), y = rnd.Next(2000);
                for (int f = 0; f < spec.Frames; f++)
                {
                    MagickImage frame;
                    lock (sourceLock)
                        frame = (MagickImage)source.CloneArea(x + f * 40, y + f * 25, spec.Width, spec.Height);
                    frame.ResetPage();
                    frame.AnimationDelay = 8;
                    frames.Add(frame);
                }
                frames.Write(path, spec.Format);
                return;
            }

            using var image = Crop(spec.Width, spec.Height);
            image.Modulate(new Percentage(rnd.Next(85, 115)), new Percentage(rnd.Next(70, 130)), new Percentage(rnd.Next(60, 140)));
            if (spec.Alpha)
            {
                using var mask = new MagickImage("gradient:white-black", new MagickReadSettings { Width = spec.Width, Height = spec.Height });
                image.Composite(mask, CompositeOperator.CopyAlpha);
            }
            image.Quality = 88;
            image.Write(path, spec.Format);
        });
        CreateCorruptFiles();

        File.WriteAllText(marker, $"Generated {DateTime.Now:O}");
    }

    private sealed class BigDatasetFactAttribute : FactAttribute
    {
        public BigDatasetFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("MIC_BIG_DATASET") != "1")
                Skip = "Takes minutes and about 3 GB of disk; set MIC_BIG_DATASET=1 to run.";
        }
    }
}
