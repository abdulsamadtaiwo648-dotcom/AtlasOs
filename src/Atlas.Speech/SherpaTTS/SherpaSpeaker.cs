using SherpaOnnx;
using NAudio.Wave;

namespace Atlas.Speech.SherpaTTS;

/// <summary>
/// Sherpa-ONNX VITS/Piper TTS speaker. Uses a local ONNX model — fully offline, no API key needed.
/// Falls back to Windows SAPI if the model file is not found.
/// </summary>
public class SherpaSpeaker
{
    // ── Model paths ────────────────────────────────────────────────────
    private const string ModelDir   = @"C:\Users\HP\AtLasOS\Models\Speech\TTS\vits-piper-en_US-amy-low";
    private const string ModelFile   = ModelDir + @"\en_US-amy-low.onnx";
    private const string DataDir     = ModelDir + @"\espeak-ng-data";
    private const string TokensFile  = ModelDir + @"\tokens.txt";

    private OfflineTts? _tts;
    private readonly object _lock = new();
    private bool _initialized;

    public bool IsAvailable => _initialized;

    // ── Initialise lazily on first use ─────────────────────────────────
    private void EnsureInit()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;

            if (!File.Exists(ModelFile))
            {
                _initialized = false;
                return;
            }

            try
            {
                var config = new OfflineTtsConfig
                {
                    Model = new OfflineTtsModelConfig
                    {
                        Vits = new OfflineTtsVitsModelConfig
                        {
                            Model   = ModelFile,
                            Lexicon = "",
                            Tokens  = File.Exists(TokensFile)  ? TokensFile  : "",
                            DataDir = Directory.Exists(DataDir) ? DataDir     : ""
                        },
                        NumThreads = 4,
                        Debug      = 0,
                        Provider   = "cpu"
                    },
                    MaxNumSentences = 1
                };

                _tts = new OfflineTts(config);
                _initialized = true;
            }
            catch
            {
                _initialized = false;
            }
        }
    }

    public async Task SpeakAsync(string text, float speed = 1.0f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Run on thread-pool so UI stays responsive
        await Task.Run(() =>
        {
            EnsureInit();

            if (_initialized && _tts != null)
            {
                SpeakWithSherpa(text, speed);
            }
            else
            {
                SpeakWithSapi(text);
            }
        });
    }

    private void SpeakWithSherpa(string text, float speed)
    {
        // Clean markdown / special chars that TTS shouldn't say literally
        string clean = CleanForSpeech(text);

        // Split long text into sentences for lower latency
        var sentences = SplitSentences(clean);

        foreach (var sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;

            var audio = _tts!.Generate(sentence, speed, speakerId: 0);
            PlayPcmFloat(audio.Samples, audio.SampleRate);
        }
    }

    private static void PlayPcmFloat(float[] samples, int sampleRate)
    {
        // Convert float32 PCM → 16-bit PCM for NAudio
        var pcm16 = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float clamped = Math.Clamp(samples[i], -1f, 1f);
            pcm16[i] = (short)(clamped * 32767f);
        }

        var bytes = new byte[pcm16.Length * 2];
        Buffer.BlockCopy(pcm16, 0, bytes, 0, bytes.Length);

        using var ms = new System.IO.MemoryStream(bytes);
        using var rawStream = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, 16, 1));
        using var player    = new WaveOutEvent();
        player.Init(rawStream);
        player.Play();
        while (player.PlaybackState == PlaybackState.Playing)
            System.Threading.Thread.Sleep(50);
    }

    private static void SpeakWithSapi(string text)
    {
        try
        {
            // Windows built-in SAPI via dynamic invocation (no hard reference needed)
            var synth = Activator.CreateInstance(
                Type.GetTypeFromProgID("SAPI.SpVoice")!);
            synth!.GetType().InvokeMember(
                "Speak",
                System.Reflection.BindingFlags.InvokeMethod,
                null, synth,
                new object[] { CleanForSpeech(text), 0 });
        }
        catch { /* silent fallback */ }
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private static string CleanForSpeech(string text)
    {
        // Strip markdown symbols, long dashes, unicode icons, etc.
        var sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (c < 128 || char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)
                || c is '.' or ',' or '?' or '!' or ':' or ';' or '-' or '\'' or '"')
                sb.Append(c);
        }
        return sb.ToString()
                 .Replace("---", " ")
                 .Replace("--", " ")
                 .Replace("  ", " ")
                 .Trim();
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        // Split on sentence-ending punctuation, keeping <300 chars per chunk
        var sentences = new List<string>();
        var current   = new System.Text.StringBuilder();

        foreach (char c in text)
        {
            current.Append(c);
            if ((c is '.' or '!' or '?') && current.Length > 30)
            {
                sentences.Add(current.ToString().Trim());
                current.Clear();
            }
            else if (current.Length > 300)
            {
                sentences.Add(current.ToString().Trim());
                current.Clear();
            }
        }
        if (current.Length > 0)
            sentences.Add(current.ToString().Trim());

        return sentences;
    }
}