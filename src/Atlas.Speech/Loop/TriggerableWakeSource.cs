namespace Atlas.Speech.Loop;

/// <summary>
/// Wakes the voice loop either when the wake word is heard or when
/// <see cref="Trigger"/> is called (e.g. the user clicks the orb).
/// </summary>
public sealed class TriggerableWakeSource : IWakeSource
{
    private readonly IWakeSource? _inner;
    private readonly object _lock = new();
    private TaskCompletionSource _manual = NewSignal();

    public TriggerableWakeSource(IWakeSource? inner = null)
    {
        _inner = inner;
    }

    /// <summary>Wakes the loop now. If it isn't waiting yet, it wakes as soon as it does.</summary>
    public void Trigger()
    {
        lock (_lock)
            _manual.TrySetResult();
    }

    public async Task WaitForWakeAsync(CancellationToken cancellationToken)
    {
        Task manual;
        lock (_lock)
            manual = _manual.Task;

        using CancellationTokenSource stopInner = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task innerWake = _inner?.WaitForWakeAsync(stopInner.Token) ?? Task.Delay(Timeout.Infinite, stopInner.Token);

        try
        {
            Task first = await Task.WhenAny(manual, innerWake);
            cancellationToken.ThrowIfCancellationRequested();

            if (first == innerWake)
                await innerWake; // surface errors from the wake word source
        }
        finally
        {
            stopInner.Cancel();

            lock (_lock)
            {
                if (_manual.Task.IsCompleted)
                    _manual = NewSignal();
            }

            try { await innerWake; } catch (OperationCanceledException) { }
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
