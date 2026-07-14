using System.Diagnostics;
using System.Text;

namespace Atlas.Speech.Services;

public class WhisperService
{
    private const string WhisperExe =
        @"C:\Users\HP\AtLasOS\runtimes\whisper.cpp\build\bin\Release\whisper-cli.exe";

    private const string WhisperModel =
        @"C:\Users\HP\AtLasOS\runtimes\whisper.cpp\models\ggml-base.en.bin";

    public async Task<string> TranscribeAsync(string audioFile)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = WhisperExe,

            Arguments =
                $"-m \"{WhisperModel}\" -f \"{audioFile}\" --no-prints",

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new();

        process.StartInfo = startInfo;

        process.Start();

        string output =
            await process.StandardOutput.ReadToEndAsync();

        string error =
            await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(error);
        }

        return CleanOutput(output);
    }

    private static string CleanOutput(string text)
    {
        StringBuilder builder = new();

        foreach (string line in text.Split(Environment.NewLine))
        {
            string value = line.Trim();

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (value.StartsWith("["))
            {
                int index = value.IndexOf(']');

                if (index >= 0)
                    value = value[(index + 1)..].Trim();
            }

            builder.Append(value);
            builder.Append(' ');
        }

        return builder.ToString().Trim();
    }
}