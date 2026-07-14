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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


// ======================================================
// Host Builder
// ======================================================
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// ======================================================
// Http Client
// ======================================================
builder.Services.AddHttpClient();
// Finance
builder.Services.AddSingleton<IFinanceProvider, CoinGeckoProvider>();
builder.Services.AddSingleton<IMarketEngine, MarketEngine>();
builder.Services.AddSingleton<IEconomyEngine, EconomyEngine>();
builder.Services.AddSingleton<IFinanceEngine, FinanceEngine>();
builder.Services.AddSingleton<FinanceContextBuilder>();

builder.Services.AddSingleton<ContextBuilder>();

// Analysis
builder.Services.AddSingleton<RiskAnalyzer>();
builder.Services.AddSingleton<PortfolioAnalyzer>();
builder.Services.AddSingleton<TechnicalAnalyzer>();
builder.Services.AddSingleton<InvestmentAnalyzer>();

// ======================================================
// AI
// ======================================================
builder.Services.AddSingleton<IAIProvider, OllamaAIProvider>();
builder.Services.AddSingleton<IFinanceEngine, FinanceEngine>();

// ======================================================
// Storage
// ======================================================
builder.Services.AddSingleton<IConversationStore, JsonConversationStore>();

// ======================================================
// Planner
// ======================================================
builder.Services.AddSingleton<IPlannerEngine, PlannerEngine>();

// ======================================================
// Conversation
// ======================================================
builder.Services.AddSingleton<IConversationService, ConversationService>();

// ======================================================
// Commands
// ======================================================
builder.Services.AddSingleton<ICommand, HelpCommand>();
builder.Services.AddSingleton<ICommand, HistoryCommand>();
builder.Services.AddSingleton<ICommand, NewCommand>();
builder.Services.AddSingleton<ICommand, DeleteCommand>();

builder.Services.AddSingleton<CommandDispatcher>();

// ======================================================
// Tools
// ======================================================
builder.Services.AddSingleton<ITool, CalculatorTool>();
builder.Services.AddSingleton<ToolManager>();

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
// ======================================================
// Kernel
// ======================================================
builder.Services.AddSingleton<IAtlasKernel, AtlasKernel>();
builder.Services.AddSingleton<IntentEngine>();
builder.Services.AddSingleton<ThinkingEngine>();

// ======================================================
// Build
// ======================================================
IHost host = builder.Build();

IAtlasKernel kernel =
    host.Services.GetRequiredService<IAtlasKernel>();

ISpeechService speech =
    host.Services.GetRequiredService<ISpeechService>();

// ======================================================
// Startup
// ======================================================
Console.Clear();

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
    Console.WriteLine("Press ENTER to speak or type a message.");
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