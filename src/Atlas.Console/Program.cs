using Atlas.Hosting;
using Atlas.AI.Providers;
using Atlas.Core.Commands;
using Atlas.Core.Interfaces;
using Atlas.Core.Planning;
using Atlas.Core.Services;
using Atlas.Core.Intent;
using Atlas.Finance.Analysis;
using Atlas.Speech.Interfaces;
using Atlas.Speech.Services;
using Atlas.Speech.SherpaSTT;
using Atlas.Speech.SherpaTTS;
using Atlas.Speech.Loop;
using Atlas.Speech.WakeWord;
using Atlas.Tools.Interfaces;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Engines;
using Atlas.Tools.Services;
using Atlas.Finance.Providers;
using Atlas.Finance.Context;
using Atlas.Core.Clock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


// ======================================================
// Host Builder
// ======================================================
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// ======================================================
// Atlas brain (AI, memory, settings, engines, kernel)
// ======================================================
builder.Services.AddAtlasCore();

// ======================================================
// Speech
// ======================================================
// ======================================================
// Speech
// ======================================================

builder.Services.AddSingleton<AudioRecorder>();
builder.Services.AddSingleton<WhisperService>();

builder.Services.AddSingleton<SherpaRecognizer>();
builder.Services.AddSingleton<SherpaSpeaker>();

builder.Services.AddSingleton<ISpeechService, SpeechService>();
builder.Services.AddSingleton<WakeWordDetector>();
// ======================================================
// Build
// ======================================================
IHost host = builder.Build();

IAtlasKernel kernel =
    host.Services.GetRequiredService<IAtlasKernel>();

ISpeechService speech =
    host.Services.GetRequiredService<ISpeechService>();

Console.Clear();

// Initialize Smart Home devices
await host.Services.InitializeAtlasAsync();

Console.WriteLine("==========================================");
Console.WriteLine("               Atlas OS");
Console.WriteLine("==========================================");
Console.WriteLine();
Console.WriteLine("Atlas Online.");
Console.WriteLine();

// ======================================================
// Main Loop
// ======================================================
while (true)
{
    Console.WriteLine("------------------------------------------");
    Console.WriteLine("Press ENTER to speak once, type \"voice\" for hands-free mode, or type a message.");
    Console.Write("You: ");

    string input = Console.ReadLine() ?? "";
    
    // ======================================
    // Exit
    // ======================================
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("Atlas: Goodbye!");
        break;
    }

    // ======================================
    // Hands-free voice loop ("voice")
    // ======================================
    if (input.Equals("voice", StringComparison.OrdinalIgnoreCase))
    {
        WakeWordDetector wakeWord = host.Services.GetRequiredService<WakeWordDetector>();

        VoiceLoop loop = new(
            new WakeWordSource(wakeWord),
            new RecognizerListener(host.Services.GetRequiredService<SherpaRecognizer>()),
            new SherpaSpeakerAdapter(host.Services.GetRequiredService<SherpaSpeaker>()),
            (text, ct) => kernel.ProcessStreamAsync(text, ct));

        loop.StateChanged += state => Console.WriteLine($"[voice] {state}");
        loop.Heard += heard => Console.WriteLine($"You: {heard}");
        loop.Said += said => Console.WriteLine($"Atlas: {said}");
        loop.Error += ex => Console.WriteLine($"[voice] error: {ex.Message}");

        using CancellationTokenSource cts = new();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cts.Cancel(); };
        Console.CancelKeyPress += onCancel;

        Console.WriteLine("Voice mode on. Say the wake word, then speak. Say \"stop listening\" or press Ctrl+C to leave.");
        await loop.RunAsync(cts.Token);

        Console.CancelKeyPress -= onCancel;
        Console.WriteLine("Voice mode off.");
        continue;
    }

    // ======================================
    // Voice Mode
    // ======================================
    if (string.IsNullOrWhiteSpace(input))
    {
        try
        {
            input = await speech.ListenAsync();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine();
                Console.WriteLine("Atlas: I couldn't understand what you said.");
                Console.WriteLine();
                continue;
            }

            Console.WriteLine();
            Console.WriteLine($"You: {input}");

            string response =
                await kernel.ProcessAsync(input);

            Console.WriteLine();
            Console.WriteLine($"Atlas: {response}");
            Console.WriteLine();

            await speech.SpeakAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Voice Error: {ex.Message}");
            Console.WriteLine();
        }

        continue;
    }

    // ======================================
    // Text Mode
    // ======================================
    try
    {
        string response =
            await kernel.ProcessAsync(input);

        Console.WriteLine();
        Console.WriteLine($"Atlas: {response}");
        Console.WriteLine();

        await speech.SpeakAsync(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Atlas Error: {ex.Message}");
        Console.WriteLine();
    }
}