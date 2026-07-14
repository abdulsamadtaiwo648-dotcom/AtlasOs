namespace Atlas.Speech.Interfaces;

public interface ISpeechService
{
    Task<string> ListenAsync();

    Task SpeakAsync(string text);
}