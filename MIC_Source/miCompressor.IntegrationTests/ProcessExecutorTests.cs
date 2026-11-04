using miCompressor.core;
using System.Diagnostics;

namespace miCompressor.IntegrationTests;

public class ProcessExecutorTests
{
    /// <summary>
    /// Tools like gifsicle can print a warning per frame for a broken file. More than the pipe buffer of error output must not hang the
    /// image (and with it the batch) forever.
    /// </summary>
    [Fact]
    public async Task ToolWritingALotOfErrorOutput_DoesNotHang()
    {
        Task<int> run = Task.Run(() => Cmd().Execute("/d /c \"for /L %i in (1,1,5000) do @echo warning: frame %i is damaged 1>&2\""));

        Assert.Same(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))));
        Assert.Equal(0, run.Result);
    }

    [Fact]
    public void ToolThatFinishes_ReturnsItsExitCode()
    {
        Assert.Equal(3, Cmd().Execute("/d /c exit 3"));
    }

    /// <summary>
    /// cmd starts ping as a child that runs for two minutes: both must be stopped at the timeout, else ping keeps the output pipes open.
    /// </summary>
    [Fact]
    public async Task StuckTool_IsStoppedAtTimeout_WithWhatItStarted_AndReportedAsFailed()
    {
        DateTime started = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();

        int result = Cmd(TimeSpan.FromSeconds(2)).Execute("/d /c ping -n 120 127.0.0.1");
        stopwatch.Stop();

        Assert.Equal(-1, result);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
        for (int i = 0; i < 50 && PingStartedSince(started); i++)
            await Task.Delay(100);
        Assert.False(PingStartedSince(started), "The tool's child process was left running.");
    }

    private static ProcessExecutor Cmd(TimeSpan? timeout = null)
        => new(Path.Combine(Environment.SystemDirectory, "cmd.exe")) { Timeout = timeout ?? TimeSpan.FromMinutes(10) };

    private static bool PingStartedSince(DateTime since)
    {
        return Process.GetProcessesByName("ping").Any(p =>
        {
            try { return p.StartTime >= since; }
            catch { return false; }
        });
    }
}
