using Atlas.Speech.SherpaSTT;
using Atlas.Speech.SherpaTTS;
using Atlas.Speech.WakeWord;

namespace Atlas.Speech.Loop;

/// <summary>Wake word detection backed by the always-on keyword spotter.</summary>
public sealed class WakeWordSource : IWakeSource
{
    private readonly WakeWordDetector _detector;

    public WakeWordSource(WakeWordDetector detector)
    {
        _detector = detector;
    }

    public async Task WaitForWakeAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnWake(string _) => tcs.TrySetResult();

        _detector.WakeWordDetected += OnWake;
        try
        {
            _detector.Start(); // no-op if already running or models are missing
            await tcs.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _detector.WakeWordDetected -= OnWake;
        }
    }
}

public sealed class RecognizerListener : IListener
{
    private readonly SherpaRecognizer _recognizer;

    public RecognizerListener(SherpaRecognizer recognizer)
    {
        _recognizer = recognizer;
    }

    public Task<string> ListenAsync(CancellationToken cancellationToken)
        => _recognizer.ListenAsync().WaitAsync(cancellationToken);
}

public sealed class SherpaSpeakerAdapter : ISpeaker
{
    private readonly SherpaSpeaker _speaker;

    public SherpaSpeakerAdapter(SherpaSpeaker speaker)
    {
        _speaker = speaker;
    }

    public Task SpeakAsync(string text, CancellationToken cancellationToken)
        => _speaker.SpeakAsync(text).WaitAsync(cancellationToken);
}
