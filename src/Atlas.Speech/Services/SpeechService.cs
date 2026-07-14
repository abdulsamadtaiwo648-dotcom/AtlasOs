using Atlas.Speech.Interfaces;
using Atlas.Speech.SherpaSTT;
using Atlas.Speech.SherpaTTS;

namespace Atlas.Speech.Services;

public class SpeechService : ISpeechService
{
    private readonly SherpaRecognizer _recognizer;
    private readonly SherpaSpeaker _speaker;

    public SpeechService(
        SherpaRecognizer recognizer,
        SherpaSpeaker speaker)
    {
        _recognizer = recognizer;
        _speaker = speaker;
    }

    public Task<string> ListenAsync()
    {
        return _recognizer.ListenAsync();
    }

    public Task SpeakAsync(string text)
    {
        return _speaker.SpeakAsync(text);
    }
}