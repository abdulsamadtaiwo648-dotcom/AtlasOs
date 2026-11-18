namespace Atlas.Speech.Loop;

/// <summary>Blocks until the wake word is heard.</summary>
public interface IWakeSource
{
    Task WaitForWakeAsync(CancellationToken cancellationToken);
}

/// <summary>Records one utterance and returns the transcribed text ("" if nothing was said).</summary>
public interface IListener
{
    Task<string> ListenAsync(CancellationToken cancellationToken);
}

/// <summary>Speaks one piece of text and returns when it has finished playing.</summary>
public interface ISpeaker
{
    Task SpeakAsync(string text, CancellationToken cancellationToken);
}

public enum VoiceState
{
    Idle,
    Listening,
    Thinking,
    Speaking,
    Stopped
}
