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

        // Planner
        builder.Services.AddSingleton<IPlannerEngine, PlannerEngine>();

        // Conversation
        builder.Services.AddSingleton<IConversationService, ConversationService>();

        // Commands
        builder.Services.AddSingleton<ICommand, HelpCommand>();
        builder.Services.AddSingleton<ICommand, HistoryCommand>();
        builder.Services.AddSingleton<ICommand, NewCommand>();
        builder.Services.AddSingleton<ICommand, DeleteCommand>();
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

        // UI Window — MainWindow also gets SherpaRecognizer + WakeWordDetector
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<SherpaRecognizer>();
        builder.Services.AddSingleton<WakeWordDetector>();

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
