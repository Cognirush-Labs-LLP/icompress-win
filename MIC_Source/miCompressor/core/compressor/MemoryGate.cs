using System;
using System.Threading;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Decides when the next image of a batch may start, so a big batch doesn't run out of memory.
/// <para>
/// The rule: an image starts when nothing is running, or when fewer images than the limit are running, its memory estimate fits in what is left of the budget,
/// Windows still has enough free memory for it, and no image is running alone.
/// An image that doesn't fit waits until nothing is running, then runs alone: nothing starts beside it until it is done.
/// </para>
/// <para>
/// The budget is set once, from free memory when the batch starts. An image gives its estimate back to the budget when it is done.
/// </para>
/// <para>
/// Usage: the batch loop calls <see cref="WaitToStartAsync"/> for one image at a time, in list order (never two calls at once).
/// Each started image calls <see cref="Finished"/> with the same estimate when it is done, even if it failed.
/// </para>
/// </summary>
public sealed class MemoryGate
{
    private readonly object _lock = new();

    /// <summary>
    /// Released each time an image finishes, to wake the waiting batch loop. Extra releases only cause an extra check of the rule.
    /// Not disposed: it holds no OS handle, and so an image finishing late can never hit a disposed gate.
    /// </summary>
    private readonly SemaphoreSlim _imageFinished = new(0);

    private readonly Func<int> _getMaxRunning;
    private readonly Func<ulong?> _getFreeMemoryBytes;

    private int _running;
    private ulong _reservedBytes;
    private bool _aloneRunning;

    /// <summary>
    /// Bytes the running images may use together, by their estimates. ulong.MaxValue if free memory is unknown.
    /// </summary>
    public ulong BudgetBytes { get; }

    /// <param name="getMaxRunning">Reads the most images allowed to run at once. Read before each start, so it can change during a batch; images already running are not stopped.</param>
    /// <param name="getFreeMemoryBytes">Reads free physical memory now, null if unknown (then only <paramref name="getMaxRunning"/> limits the batch).</param>
    public MemoryGate(Func<int> getMaxRunning, Func<ulong?> getFreeMemoryBytes)
    {
        _getMaxRunning = getMaxRunning;
        _getFreeMemoryBytes = getFreeMemoryBytes;
        BudgetBytes = getFreeMemoryBytes() is ulong free ? BatchMemoryPlanner.Budget(free) : ulong.MaxValue;
    }

    /// <summary>
    /// Waits until the rule lets the image start, then counts it as running.
    /// </summary>
    /// <returns>True if the image didn't fit and runs alone.</returns>
    public async Task<bool> WaitToStartAsync(ulong estimateBytes)
    {
        while (true)
        {
            bool lowOnMemoryNow = BatchMemoryPlanner.IsUnderPressure(estimateBytes, _getFreeMemoryBytes());
            int maxRunning = _getMaxRunning();
            lock (_lock)
            {
                bool fits = FitsInBudget(estimateBytes) && !lowOnMemoryNow;
                if (_running == 0 || (fits && _running < maxRunning && !_aloneRunning))
                {
                    _running++;
                    _reservedBytes += estimateBytes; // can't overflow: either nothing was reserved, or it fits in the budget
                    _aloneRunning = !fits;
                    return _aloneRunning;
                }
            }
            await _imageFinished.WaitAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Called once by each started image when it is done: frees its slot and gives its estimate back to the budget.
    /// </summary>
    public void Finished(ulong estimateBytes)
    {
        lock (_lock)
        {
            _running--;
            _reservedBytes -= estimateBytes;
            if (_running == 0)
                _aloneRunning = false;
        }
        _imageFinished.Release();
    }

    /// <summary>
    /// Written without adding to the estimate, which can be near ulong.MaxValue for corrupt files.
    /// </summary>
    private bool FitsInBudget(ulong estimateBytes)
    {
        return estimateBytes <= BudgetBytes && _reservedBytes <= BudgetBytes - estimateBytes;
    }
}
