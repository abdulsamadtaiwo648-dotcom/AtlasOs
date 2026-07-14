using NAudio.Wave;

namespace Atlas.Speech.Services;

public class AudioRecorder
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;

    public async Task RecordUntilSilenceAsync(
        string outputFile,
        int silenceMilliseconds = 1500,
        float silenceThreshold = 0.03f)
    {
        TaskCompletionSource tcs = new();

        bool speechStarted = false;

        DateTime recordingStarted = DateTime.UtcNow;

        DateTime lastSpeech = DateTime.UtcNow;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1),
            BufferMilliseconds = 100
        };

        _writer = new WaveFileWriter(outputFile, _waveIn.WaveFormat);

        _waveIn.DataAvailable += (s, e) =>
        {
            if (_writer == null)
                return;

            _writer.Write(e.Buffer, 0, e.BytesRecorded);
            _writer.Flush();

            float maxLevel = 0;

            for (int i = 0; i < e.BytesRecorded; i += 2)
{
    short sample = BitConverter.ToInt16(e.Buffer, i);

    float level = Math.Abs(sample / 32768f);

    if (level > maxLevel)
        maxLevel = level;
}



if (maxLevel > silenceThreshold)
{
    if (!speechStarted)
    {
        Console.WriteLine("🗣️ Speech detected...");
    }

    speechStarted = true;
    lastSpeech = DateTime.UtcNow;
}

            TimeSpan recordingTime =
                DateTime.UtcNow - recordingStarted;

            // Safety timeout
            if (recordingTime.TotalSeconds >= 15)
            {
                _waveIn?.StopRecording();
                return;
            }

            // Don't stop until speech has started
            if (!speechStarted)
                return;

            // Always record at least 2 seconds
            if (recordingTime.TotalSeconds < 2)
                return;

            TimeSpan silence =
                DateTime.UtcNow - lastSpeech;

            if (silence.TotalMilliseconds >= silenceMilliseconds)
            {
                Console.WriteLine("🤫 Silence detected.");
                _waveIn?.StopRecording();
            }
        };

        _waveIn.RecordingStopped += (s, e) =>
        {
            _writer?.Dispose();
            _writer = null;

            _waveIn?.Dispose();
            _waveIn = null;

            tcs.TrySetResult();
        };

        Console.WriteLine();
        Console.WriteLine("🎤 Listening...");
        Console.WriteLine("Speak naturally. I'll stop after you pause.");

        _waveIn.StartRecording();

        await tcs.Task;
    }
}