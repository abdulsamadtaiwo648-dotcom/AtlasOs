using Atlas.AI.Providers;
using Atlas.Core.Clock;
using Atlas.Core.Commands;
using Atlas.Core.Interfaces;
using Atlas.Core.Intent;
using Atlas.Core.Planning;
using Atlas.Core.Services;
using Atlas.Core.Settings;
using Atlas.Finance.Analysis;
using Atlas.Finance.Context;
using Atlas.Finance.Engines;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Providers;
using Atlas.Memory;
using Atlas.SmartHome.Interfaces;
using Atlas.SmartHome.Providers.SimulationProvider;
using Atlas.SmartHome.Services;
using Atlas.Tools.Interfaces;
using Atlas.Tools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Hosting;

public static class AtlasServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the Atlas "brain" needs: the AI provider, memory, settings,
    /// conversations, commands, tools, the finance/smart-home/system/vision engines and the kernel.
    /// Speech and screens are added by the apps that use them.
    /// </summary>
    public static IServiceCollection AddAtlasCore(this IServiceCollection services)
    {
        services.AddHttpClient();

        // Finance
        services.AddSingleton<IFinanceProvider, YahooFinanceProvider>();
        services.AddSingleton<IMarketEngine, MarketEngine>();
        services.AddSingleton<IEconomyEngine, EconomyEngine>();
        services.AddSingleton<IFinanceEngine, FinanceEngine>();
        services.AddSingleton<FinanceContextBuilder>();
        services.AddSingleton<ContextBuilder>();
        services.AddSingleton<RiskAnalyzer>();
        services.AddSingleton<PortfolioAnalyzer>();
        services.AddSingleton<TechnicalAnalyzer>();
        services.AddSingleton<InvestmentAnalyzer>();
        services.AddSingleton<ChartEngine>();

        // Smart home, apps and vision
        services.AddSingleton<ISmartHomeProvider, SimulationProvider>();
        services.AddSingleton<SmartHomeEngine>();
        services.AddSingleton<Atlas.System.Interfaces.IAppLauncher, Atlas.System.Providers.WindowsAppLauncher>();
        services.AddSingleton<Atlas.System.Services.AppLauncherEngine>();
        services.AddSingleton<Atlas.Vision.Interfaces.IVisionProvider, Atlas.Vision.Providers.SimulationVisionProvider>();
        services.AddSingleton<Atlas.Vision.Services.VisionEngine>();

        // AI, storage, memory and settings
        services.AddSingleton<IAIProvider, OllamaAIProvider>();
        services.AddSingleton<IConversationStore, JsonConversationStore>();
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService());
        services.AddSingleton<IMemoryService>(_ => new SqliteMemoryService());

        // Conversation, planning, commands and tools
        services.AddSingleton<IPlannerEngine, PlannerEngine>();
        services.AddSingleton<IConversationService, ConversationService>();

        services.AddSingleton<ICommand, HelpCommand>();
        services.AddSingleton<ICommand, HistoryCommand>();
        services.AddSingleton<ICommand, NewCommand>();
        services.AddSingleton<ICommand, DeleteCommand>();
        services.AddSingleton<ICommand, MemoryCommand>();
        services.AddSingleton<ICommand, ForgetCommand>();
        services.AddSingleton<CommandDispatcher>();

        services.AddSingleton<ITool, CalculatorTool>();
        services.AddSingleton<ToolManager>();

        // Kernel
        services.AddSingleton<IAtlasKernel, AtlasKernel>();
        services.AddSingleton<IntentEngine>();
        services.AddSingleton<ThinkingEngine>();

        // Clock
        services.AddSingleton<IClockEngine, ClockEngine>();
        services.AddSingleton<CalendarEngine>();
        services.AddSingleton<TimeZoneEngine>();
        services.AddSingleton<WorldClockEngine>();

        return services;
    }

    /// <summary>One-time start-up work (e.g. seeding the simulated smart-home devices).</summary>
    public static async Task InitializeAtlasAsync(this IServiceProvider services)
    {
        ISmartHomeProvider smartHome = services.GetRequiredService<ISmartHomeProvider>();
        await smartHome.InitializeAsync();
    }
}
