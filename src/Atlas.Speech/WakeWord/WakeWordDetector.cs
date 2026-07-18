using NAudio.Wave;
using SherpaOnnx;

namespace Atlas.Speech.WakeWord;

/// <summary>
/// Listens to the microphone 24/7 for the wake phrase "Hey Atlas" or "Atlas".
/// Uses sherpa-onnx KeywordSpotter with the Gigaspeech 3.3M zipformer model.
/// When detected, fires the <see cref="WakeWordDetected"/> event.
/// </summary>
public sealed class WakeWordDetector : IDisposable
{
    // ── Model paths ────────────────────────────────────────────────────────
    private const string ModelDir  = @"C:\Users\HP\AtLasOS\Models\Speech\WakeWord\sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01";
    private const string Encoder   = ModelDir + @"\encoder-epoch-12-avg-2-chunk-16-left-64.int8.onnx";
    private const string Decoder   = ModelDir + @"\decoder-epoch-12-avg-2-chunk-16-left-64.int8.onnx";
    private const string Joiner    = ModelDir + @"\joiner-epoch-12-avg-2-chunk-16-left-64.int8.onnx";
    private const string TokensPath   = ModelDir + @"\tokens.txt";
    private const string KeywordsPath = ModelDir + @"\atlas_keywords.txt";

    private const int SampleRate  = 16000;
    private const int ChunkSizeMs = 100; // process 100 ms chunks

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Raised on the thread-pool when the wake word is detected.</summary>
    public event Action<string>? WakeWordDetected;

    // ── State ──────────────────────────────────────────────────────────────
    private KeywordSpotter?  _spotter;
    private OnlineStream?    _stream;
    private WaveInEvent?     _mic;
    private bool             _running;
    private bool             _disposed;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    /// <summary>Initialise and start listening.</summary>
    public void Start()
    {
        if (_running || _disposed) return;

        if (!ModelsAvailable())
        {
            Console.WriteLine("[WakeWord] Model files not found – wake word disabled.");
            return;
        }

        try
        {
            _spotter = CreateSpotter();
            _stream  = _spotter.CreateStream();
            _running = true;
            StartMic();
            Console.WriteLine("[WakeWord] Listening for 'Hey Atlas'…");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WakeWord] Failed to initialise: {ex.Message}");
        }
    }

    /// <summary>Stop listening and release resources.</summary>
    public void Stop()
    {
        _running = false;
        _mic?.StopRecording();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _stream?.Dispose();
        _spotter?.Dispose();
        _mic?.Dispose();
    }

    // ── Microphone capture ─────────────────────────────────────────────────

    private void StartMic()
    {
        _mic = new WaveInEvent
        {
            WaveFormat         = new WaveFormat(SampleRate, 16, 1),
            BufferMilliseconds = ChunkSizeMs
        };

        _mic.DataAvailable    += OnAudioData;
        _mic.RecordingStopped += OnRecordingStopped;
        _mic.StartRecording();
    }

    private void OnAudioData(object? sender, WaveInEventArgs e)
    {
        if (!_running || _spotter == null || _stream == null) return;

        // Convert 16-bit PCM → float samples
        int sampleCount = e.BytesRecorded / 2;
        var samples     = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

        // Feed samples into the streaming recogniser
        _stream.AcceptWaveform(SampleRate, samples);

        // Poll for detection
        while (_spotter.IsReady(_stream))
        {
            _spotter.Decode(_stream);
            var result = _spotter.GetResult(_stream);
            if (!string.IsNullOrWhiteSpace(result.Keyword))
            {
                string kw = result.Keyword.Trim();
                Console.WriteLine($"[WakeWord] Detected: '{kw}'");
                Task.Run(() => WakeWordDetected?.Invoke(kw));
                // Reset the stream so it is ready for the next detection
                _stream.Dispose();
                _stream = _spotter.CreateStream();
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_running && !_disposed)
        {
            // Restart mic if it stopped unexpectedly
            Console.WriteLine("[WakeWord] Mic stopped unexpectedly – restarting…");
            Task.Delay(500).ContinueWith(_ => StartMic());
        }
    }

    // ── KeywordSpotter factory ─────────────────────────────────────────────

    private static KeywordSpotter CreateSpotter()
    {
        var config = new KeywordSpotterConfig
        {
            FeatConfig = new FeatureConfig
            {
                SampleRate = SampleRate,
                FeatureDim = 80
            },
            ModelConfig = new OnlineModelConfig
            {
                Transducer = new OnlineTransducerModelConfig
                {
                    Encoder = Encoder,
                    Decoder = Decoder,
                    Joiner  = Joiner
                },
                Tokens     = TokensPath,
                NumThreads = 2,
                Debug      = 0,
                Provider   = "cpu"
            },
            KeywordsFile  = KeywordsPath,
            NumTrailingBlanks = 1
        };

        return new KeywordSpotter(config);
    }

    private static bool ModelsAvailable() =>
        File.Exists(Encoder) &&
        File.Exists(Decoder) &&
        File.Exists(Joiner)  &&
        File.Exists(TokensPath) &&
        File.Exists(KeywordsPath);
}
