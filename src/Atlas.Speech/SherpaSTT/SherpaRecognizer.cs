using Atlas.Speech.Services;

namespace Atlas.Speech.SherpaSTT;

public class SherpaRecognizer
{
    private readonly AudioRecorder _recorder;
    private readonly WhisperService _whisper;

    private readonly string _audioFile =
        Path.Combine("Data", "Audio", "recording.wav");

    public SherpaRecognizer(
        AudioRecorder recorder,
        WhisperService whisper)
    {
        _recorder = recorder;
        _whisper = whisper;

        Directory.CreateDirectory(
            Path.Combine("Data", "Audio"));
    }

    public async Task<string> ListenAsync()
    {

        await _recorder.RecordUntilSilenceAsync(_audioFile);

        Console.WriteLine();
        Console.WriteLine("🧠 Transcribing...");

        string text =
            await _whisper.TranscribeAsync(_audioFile);

        // Console.WriteLine($"You: {text}");

        return text.Trim();
    }
}