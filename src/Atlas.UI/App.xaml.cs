using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        // Http Client
        builder.Services.AddHttpClient();
        
        // Finance
        builder.Services.AddSingleton<IFinanceProvider, YahooFinanceProvider>();
        builder.Services.AddSingleton<IMarketEngine, MarketEngine>();
        builder.Services.AddSingleton<IEconomyEngine, EconomyEngine>();
        builder.Services.AddSingleton<IFinanceEngine, FinanceEngine>();
        builder.Services.AddSingleton<FinanceContextBuilder>();

        // Smart Home
        builder.Services.AddSingleton<Atlas.SmartHome.Interfaces.ISmartHomeProvider, Atlas.SmartHome.Providers.SimulationProvider.SimulationProvider>();
        builder.Services.AddSingleton<Atlas.SmartHome.Services.SmartHomeEngine>();

        // System (App Launcher)
        builder.Services.AddSingleton<Atlas.System.Interfaces.IAppLauncher, Atlas.System.Providers.WindowsAppLauncher>();
        builder.Services.AddSingleton<Atlas.System.Services.AppLauncherEngine>();

        // Vision
        builder.Services.AddSingleton<Atlas.Vision.Interfaces.IVisionProvider, Atlas.Vision.Providers.SimulationVisionProvider>();
        builder.Services.AddSingleton<Atlas.Vision.Services.VisionEngine>();

        builder.Services.AddSingleton<ContextBuilder>();

        // Analysis
        builder.Services.AddSingleton<RiskAnalyzer>();
        builder.Services.AddSingleton<PortfolioAnalyzer>();
        builder.Services.AddSingleton<TechnicalAnalyzer>();
        builder.Services.AddSingleton<InvestmentAnalyzer>();
        builder.Services.AddSingleton<ChartEngine>();

        // AI
        builder.Services.AddSingleton<IAIProvider, OllamaAIProvider>();

        // Storage
        builder.Services.AddSingleton<IConversationStore, JsonConversationStore>();
        builder.Services.AddSingleton<Atlas.Core.Interfaces.IMemoryService>(_ => new Atlas.Memory.SqliteMemoryService());

        // Planner
        builder.Services.AddSingleton<IPlannerEngine, PlannerEngine>();

        // Conversation
        builder.Services.AddSingleton<IConversationService, ConversationService>();

        // Commands
        builder.Services.AddSingleton<ICommand, HelpCommand>();
        builder.Services.AddSingleton<ICommand, HistoryCommand>();
        builder.Services.AddSingleton<ICommand, NewCommand>();
        builder.Services.AddSingleton<ICommand, DeleteCommand>();
        builder.Services.AddSingleton<ICommand, Atlas.Core.Commands.MemoryCommand>();
        builder.Services.AddSingleton<ICommand, Atlas.Core.Commands.ForgetCommand>();
        builder.Services.AddSingleton<CommandDispatcher>();

        // Tools
        builder.Services.AddSingleton<ITool, CalculatorTool>();
        builder.Services.AddSingleton<ToolManager>();

        // Speech
        builder.Services.AddSingleton<AudioRecorder>();
        builder.Services.AddSingleton<WhisperService>();
        builder.Services.AddSingleton<SherpaRecognizer>();
        builder.Services.AddSingleton<SherpaSpeaker>();
        builder.Services.AddSingleton<ISpeechService, SpeechService>();

        // Kernel
        builder.Services.AddSingleton<IAtlasKernel, AtlasKernel>();
        builder.Services.AddSingleton<IntentEngine>();
        builder.Services.AddSingleton<ThinkingEngine>();

        // Clock
        builder.Services.AddSingleton<IClockEngine, ClockEngine>();
        builder.Services.AddSingleton<CalendarEngine>();
        builder.Services.AddSingleton<TimeZoneEngine>();
        builder.Services.AddSingleton<WorldClockEngine>();

        // Settings, memory and speech glue
        builder.Services.AddSingleton<Atlas.Core.Settings.ISettingsService>(_ => new Atlas.Core.Settings.JsonSettingsService());
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
        var smartHomeProvider = _host.Services.GetRequiredService<Atlas.SmartHome.Interfaces.ISmartHomeProvider>();
        await smartHomeProvider.InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
