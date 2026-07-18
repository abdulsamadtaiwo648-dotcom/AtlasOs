using SherpaOnnx;
using NAudio.Wave;

namespace Atlas.Speech.SherpaSTT;

/// <summary>
/// Sherpa-ONNX offline STT using the SenseVoice English model + NAudio microphone capture.
/// Falls back to whisper-cli subprocess if the model is unavailable.
/// </summary>
public class SherpaRecognizer
{
    // ── Model paths ────────────────────────────────────────────────────────────
    private const string ModelFile  = @"C:\Users\HP\AtLasOS\Models\Speech\STT\sherpa-onnx-sense-voice-en\model.onnx";
    private const string TokensFile = @"C:\Users\HP\AtLasOS\Models\Speech\STT\sherpa-onnx-sense-voice-en\tokens.txt";

    private const int   SampleRate      = 16000;
    private const int   SilenceMs       = 750;
    private const float SilenceThreshold = 0.012f;
    private const int   MaxRecordSecs   = 15;

    // ── VUI state events ───────────────────────────────────────────────────────
    public event Action? ListeningStarted;
    public event Action? SpeechDetected;
    public event Action? TranscribingStarted;
    public event Action? ListeningEnded;

    // ── Public API ─────────────────────────────────────────────────────────────
    public async Task<string> ListenAsync()
    {
        float[] samples = await RecordAsync();
        if (samples.Length == 0)
        {
            ListeningEnded?.Invoke();
            return "";
        }

        TranscribingStarted?.Invoke();
        string text = Transcribe(samples);
        ListeningEnded?.Invoke();
        return text.Trim();
    }

    // ── Microphone capture ─────────────────────────────────────────────────────
    private Task<float[]> RecordAsync()
    {
        var tcs = new TaskCompletionSource<float[]>();
        var buffer = new List<float>(SampleRate * MaxRecordSecs);
        bool speechStarted = false;
        DateTime recordStart = DateTime.UtcNow;
        DateTime lastSpeech  = DateTime.UtcNow;

        var waveIn = new WaveInEvent
        {
            WaveFormat         = new WaveFormat(SampleRate, 16, 1),
            BufferMilliseconds = 80
        };

        ListeningStarted?.Invoke();

        waveIn.DataAvailable += (_, e) =>
        {
            float maxLevel = 0f;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                float f = BitConverter.ToInt16(e.Buffer, i) / 32768f;
                buffer.Add(f);
                float abs = MathF.Abs(f);
                if (abs > maxLevel) maxLevel = abs;
            }

            if (maxLevel > SilenceThreshold)
            {
                if (!speechStarted)
                {
                    speechStarted = true;
                    SpeechDetected?.Invoke();
                }
                lastSpeech = DateTime.UtcNow;
            }

            double elapsed  = (DateTime.UtcNow - recordStart).TotalSeconds;
            double silenceMs = (DateTime.UtcNow - lastSpeech).TotalMilliseconds;

            if (elapsed >= MaxRecordSecs
                || (speechStarted && silenceMs >= SilenceMs && elapsed >= 2.0))
            {
                waveIn.StopRecording();
            }
        };

        waveIn.RecordingStopped += (_, _) =>
        {
            waveIn.Dispose();
            tcs.TrySetResult(speechStarted ? buffer.ToArray() : Array.Empty<float>());
        };

        waveIn.StartRecording();
        return tcs.Task;
    }

    // ── Transcription ──────────────────────────────────────────────────────────
    private static string Transcribe(float[] samples)
    {
        if (!File.Exists(ModelFile) || !File.Exists(TokensFile))
            return TranscribeWhisperFallback(samples);

        try
        {
            var config = new OfflineRecognizerConfig
            {
                FeatConfig = new FeatureConfig { SampleRate = SampleRate, FeatureDim = 80 },
                ModelConfig = new OfflineModelConfig
                {
                    SenseVoice = new OfflineSenseVoiceModelConfig
                    {
                        Model    = ModelFile,
                        Language = "en",
                        UseInverseTextNormalization = 1
                    },
                    Tokens     = TokensFile,
                    NumThreads = 4,
                    Debug      = 0,
                    Provider   = "cpu"
                }
            };

            using var recognizer = new OfflineRecognizer(config);
            using var stream     = recognizer.CreateStream();
            stream.AcceptWaveform(SampleRate, samples);
            recognizer.Decode(stream);
            return stream.Result.Text;
        }
        catch
        {
            return TranscribeWhisperFallback(samples);
        }
    }

    // ── Whisper fallback ───────────────────────────────────────────────────────
    private static string TranscribeWhisperFallback(float[] samples)
    {
        const string exe   = @"C:\Users\HP\AtLasOS\runtimes\whisper.cpp\build\bin\Release\whisper-cli.exe";
        const string model = @"C:\Users\HP\AtLasOS\runtimes\whisper.cpp\models\ggml-base.en.bin";
        if (!File.Exists(exe)) return "";

        string tmpFile = Path.Combine(Path.GetTempPath(), $"atlas_stt_{Guid.NewGuid():N}.wav");
        try
        {
            WriteWav(tmpFile, samples, SampleRate);
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName               = exe,
                Arguments              = $"-m \"{model}\" -f \"{tmpFile}\" --no-prints",
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            })!;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output.Trim();
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }

    private static void WriteWav(string path, float[] samples, int sampleRate)
    {
        using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
        var pcm = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(Math.Clamp(samples[i], -1f, 1f) * 32767f);
            pcm[i * 2]     = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        writer.Write(pcm, 0, pcm.Length);
    }
}