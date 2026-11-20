using System.Threading.Channels;

namespace Atlas.Speech.Loop;

/// <summary>
/// The hands-free conversation loop:
/// idle -> wake word -> listen -> think -> speak (streamed, sentence by sentence) -> idle.
/// </summary>
public sealed class VoiceLoop
{
    private static readonly string[] StopPhrases =
    {
        "stop listening", "goodbye", "good bye", "goodbye atlas", "shut down voice mode", "exit voice mode"
    };

    private readonly IWakeSource _wake;
    private readonly IListener _listener;
    private readonly ISpeaker _speaker;
    private readonly Func<string, CancellationToken, IAsyncEnumerable<string>> _respond;

    public VoiceLoop(
        IWakeSource wake,
        IListener listener,
        ISpeaker speaker,
        Func<string, CancellationToken, IAsyncEnumerable<string>> respond)
    {
        _wake = wake;
        _listener = listener;
        _speaker = speaker;
        _respond = respond;
    }

    public VoiceState State { get; private set; } = VoiceState.Stopped;

    public event Action<VoiceState>? StateChanged;

    /// <summary>Raised with the transcript of what the user said.</summary>
    public event Action<string>? Heard;

    /// <summary>Raised with each sentence just before it is spoken.</summary>
    public event Action<string>? Said;

    /// <summary>Raised when a turn fails (the loop keeps running).</summary>
    public event Action<Exception>? Error;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SetState(VoiceState.Idle);
                await _wake.WaitForWakeAsync(cancellationToken);

                bool keepGoing = await RunTurnAsync(cancellationToken);
                if (!keepGoing)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        finally
        {
            SetState(VoiceState.Stopped);
        }
    }

    /// <summary>Handles a single wake -> reply turn. Returns false when the user asked to stop.</summary>
    private async Task<bool> RunTurnAsync(CancellationToken ct)
    {
        try
        {
            SetState(VoiceState.Listening);
            string text = (await _listener.ListenAsync(ct)).Trim();

            if (text.Length == 0)
                return true;

            Heard?.Invoke(text);

            if (IsStopPhrase(text))
            {
                await SpeakAsync("Goodbye.", ct);
                return false;
            }

            SetState(VoiceState.Thinking);
            await RespondAsync(text, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);

            try
            {
                await SpeakAsync("Sorry, something went wrong.", ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // nothing more we can do for this turn
            }
        }

        return true;
    }

    private async Task RespondAsync(string text, CancellationToken ct)
    {
        // The model's reply is cut into sentences by one task and spoken by
        // another, so the first sentence is heard while the rest is still being written.
        Channel<string> sentences = Channel.CreateUnbounded<string>();

        Task producer = Task.Run(async () =>
        {
            try
            {
                SentenceChunker chunker = new();

                await foreach (string fragment in _respond(text, ct).WithCancellation(ct))
                {
                    foreach (string sentence in chunker.Push(fragment))
                        sentences.Writer.TryWrite(sentence);
                }

                foreach (string rest in chunker.Flush())
                    sentences.Writer.TryWrite(rest);

                sentences.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                sentences.Writer.TryComplete(ex);
            }
        }, ct);

        await foreach (string sentence in sentences.Reader.ReadAllAsync(ct))
            await SpeakAsync(sentence, ct);

        await producer;
    }

    private async Task SpeakAsync(string sentence, CancellationToken ct)
    {
        SetState(VoiceState.Speaking);
        Said?.Invoke(sentence);
        await _speaker.SpeakAsync(sentence, ct);
    }

    public static bool IsStopPhrase(string text)
    {
        string normalized = new string(text.ToLowerInvariant()
            .Where(c => char.IsLetter(c) || c == ' ').ToArray()).Trim();

        return StopPhrases.Contains(normalized);
    }

    private void SetState(VoiceState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke(state);
    }
}
