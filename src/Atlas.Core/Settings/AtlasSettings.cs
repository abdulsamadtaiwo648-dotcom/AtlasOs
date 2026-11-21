namespace Atlas.Core.Settings;

/// <summary>User-editable options. Saved as JSON next to the memory database.</summary>
public class AtlasSettings
{
    /// <summary>Where Ollama is running (local), or another Ollama-compatible server.</summary>
    public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";

    /// <summary>Model name as Ollama knows it, e.g. "gemma3:4b".</summary>
    public string Model { get; set; } = "gemma3:4b";

    /// <summary>Speak typed replies out loud too.</summary>
    public bool SpeakReplies { get; set; } = false;

    /// <summary>1.0 is normal speed.</summary>
    public double SpeechSpeed { get; set; } = 1.0;

    /// <summary>Turn on the always-listening voice loop when the app opens.</summary>
    public bool StartHandsFree { get; set; } = false;

    public AtlasSettings Clone() => (AtlasSettings)MemberwiseClone();
}
