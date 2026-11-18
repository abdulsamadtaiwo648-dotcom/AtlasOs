namespace Atlas.Speech;

/// <summary>
/// Resolves model and runtime locations without hardcoding a user profile.
/// Root lookup order:
///   1. ATLAS_HOME environment variable
///   2. The nearest parent of the app folder (or working directory) that contains a "Models" folder
///   3. %USERPROFILE%\AtlasOS
/// Individual locations can also be overridden with ATLAS_WHISPER_EXE and ATLAS_WHISPER_MODEL.
/// </summary>
public static class SpeechPaths
{
    public static string Root { get; } = ResolveRoot();

    public static string ModelsDir => Path.Combine(Root, "Models", "Speech");

    public static string SenseVoiceDir => Path.Combine(ModelsDir, "STT", "sherpa-onnx-sense-voice-en");
    public static string SenseVoiceModel => Path.Combine(SenseVoiceDir, "model.onnx");
    public static string SenseVoiceTokens => Path.Combine(SenseVoiceDir, "tokens.txt");

    public static string WakeWordDir => Path.Combine(ModelsDir, "WakeWord",
        "sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01");

    public static string TtsDir => Path.Combine(ModelsDir, "TTS", "vits-piper-en_US-amy-low");

    public static string WhisperExe =>
        Environment.GetEnvironmentVariable("ATLAS_WHISPER_EXE")
        ?? Path.Combine(Root, "runtimes", "whisper.cpp", "build", "bin", "Release", "whisper-cli.exe");

    public static string WhisperModel =>
        Environment.GetEnvironmentVariable("ATLAS_WHISPER_MODEL")
        ?? Path.Combine(Root, "runtimes", "whisper.cpp", "models", "ggml-base.en.bin");

    private static string ResolveRoot()
    {
        string? env = Environment.GetEnvironmentVariable("ATLAS_HOME");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            DirectoryInfo? dir = new(start);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Models", "Speech")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AtlasOS");
    }
}
