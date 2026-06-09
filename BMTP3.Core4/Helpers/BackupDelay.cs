namespace BMTP3.Core4.Helpers;

internal readonly struct BackupDelay
{
    private readonly int _delayMs;
    private readonly CancellationToken _ct;

    public BackupDelay(int delayMs, CancellationToken ct)
    {
        _delayMs = delayMs;
        _ct = ct;
    }

    public bool IsEnabled => _delayMs > 0;

    public int DelayMs => _delayMs;

    public Task WaitAsync()
    {
        if (!IsEnabled)
            return Task.CompletedTask;
        return Task.Delay(_delayMs, _ct);
    }
}
