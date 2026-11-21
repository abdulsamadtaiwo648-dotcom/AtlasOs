using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
using Atlas.Tools.Interfaces;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Engines;
using Atlas.Tools.Services;
using Atlas.Finance.Providers;
using Atlas.Finance.Context;
using Atlas.Core.Clock;
using Atlas.Speech.WakeWord;

namespace AtlasUI;

public partial class App : Application
{
    private IHost _host;

    public App()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Atlas brain (AI, memory, settings, engines, kernel)
        builder.Services.AddAtlasCore();

        // Speech
        builder.Services.AddSingleton<AudioRecorder>();
        builder.Services.AddSingleton<WhisperService>();
        builder.Services.AddSingleton<SherpaRecognizer>();
        builder.Services.AddSingleton<SherpaSpeaker>();
        builder.Services.AddSingleton<ISpeechService, SpeechService>();

        // Settings, memory and speech glue
        builder.Services.AddSingleton<WakeWordDetector>();
        builder.Services.AddSingleton(sp => new Atlas.Speech.Loop.TriggerableWakeSource(
            new Atlas.Speech.Loop.WakeWordSource(sp.GetRequiredService<WakeWordDetector>())));
        builder.Services.AddSingleton<Atlas.Speech.Loop.IListener>(sp =>
            new Atlas.Speech.Loop.RecognizerListener(sp.GetRequiredService<SherpaRecognizer>()));
        builder.Services.AddSingleton<Atlas.Speech.Loop.ISpeaker>(sp =>
        {
            var settings = sp.GetRequiredService<Atlas.Core.Settings.ISettingsService>();
            return new Atlas.Speech.Loop.SherpaSpeakerAdapter(
                sp.GetRequiredService<SherpaSpeaker>(),
                () => (float)settings.Current.SpeechSpeed);
        });

        // Screens (view models) and the window
        builder.Services.AddSingleton<Atlas.App.Mvvm.IUiDispatcher, WpfDispatcher>();
        builder.Services.AddHttpClient<Atlas.App.Settings.IOllamaClient, Atlas.App.Settings.HttpOllamaClient>();
        builder.Services.AddSingleton<Atlas.App.Chat.ChatViewModel>();
        builder.Services.AddSingleton<Atlas.App.Memory.MemoryViewModel>();
        builder.Services.AddSingleton<Atlas.App.Settings.SettingsViewModel>();
        builder.Services.AddSingleton<Atlas.App.Voice.VoiceViewModel>();
        builder.Services.AddSingleton<Atlas.App.ShellViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        // Initialize Smart Home
        await _host.Services.InitializeAtlasAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
